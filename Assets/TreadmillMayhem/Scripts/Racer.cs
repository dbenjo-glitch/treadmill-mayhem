using System;
using System.Collections;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Physics body shared by the player and AI racers. A controller (PlayerController / AIController) writes
    /// MoveInput and BoostHeld; this turns them into Rigidbody forces, handles wobble, recovery and knockback,
    /// and reports elimination. Stats come from a <see cref="RacerDefinition"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Racer : MonoBehaviour
    {
        public RacerDefinition definition;
        public bool isPlayer;

        public static event Action<Racer> Eliminated;
        public static event Action<Racer, Racer, float> Bumped;

        // --- Controller inputs ---
        public Vector2 MoveInput { get; set; }
        public bool BoostHeld { get; set; }

        // --- State ---
        public Rigidbody Body { get; private set; }
        public bool IsEliminated { get; private set; }
        public float EliminatedAt { get; private set; }
        public bool Grounded => groundedFrames > 0;
        public bool ControlLost => controlLossTimer > 0f;
        public Vector2 SmoothedInput { get; private set; }
        public float ShieldTimer { get; set; }
        public bool HasShield => ShieldTimer > 0f;
        public string DisplayName => definition ? definition.displayName : name;
        public Color Color => definition ? definition.color : Color.white;
        public RacerShape Shape => definition ? definition.shape : RacerShape.Can;

        public StaminaSystem Stamina { get; private set; }
        public AccelerationSystem Acceleration { get; private set; }
        public SuperSpeedSystem Super { get; private set; }

        /// <summary>Fired for any noticeable hit, with the impact speed; used to reset acceleration.</summary>
        public event Action<float> Impacted;

        static int nextRacerId;
        readonly int racerId = ++nextRacerId;
        const float KnockCooldown = 0.35f;
        const float MaxKnockSpeed = 9f;

        int groundedFrames;
        float knockImmuneUntil;
        float controlLossTimer;
        float instabilitySeed;
        Treadmill treadmill;
        GameObject shieldBubble;
        Transform visual;
        Vector3 visualScale = Vector3.one;
        Coroutine squashRoutine;
        float baseMass;

        GameConfig Config => GameConfig.Current;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.constraints = RigidbodyConstraints.None;
            Stamina = GetComponent<StaminaSystem>();
            Acceleration = GetComponent<AccelerationSystem>();
            Super = GetComponent<SuperSpeedSystem>();
            instabilitySeed = UnityEngine.Random.value * 100f;
            ApplyDefinition();
        }

        /// <summary>Pushes the definition's physics values onto the Rigidbody and collider.</summary>
        public void ApplyDefinition()
        {
            if (definition == null) return;
            Body.mass = definition.mass;
            Body.linearDamping = definition.linearDamping;
            Body.angularDamping = definition.angularDamping;
            baseMass = definition.mass;
            var col = GetComponent<Collider>();
            if (col)
            {
                var pm = col.sharedMaterial != null ? col.sharedMaterial : new PhysicsMaterial(definition.displayName);
                pm.dynamicFriction = pm.staticFriction = definition.friction;
                pm.bounciness = definition.bounciness;
                pm.frictionCombine = PhysicsMaterialCombine.Maximum;
                pm.bounceCombine = PhysicsMaterialCombine.Maximum;
                col.sharedMaterial = pm;
            }
        }

        void Start()
        {
            treadmill = FindAnyObjectByType<Treadmill>();
            // Rolling speed = spin × radius, so skinny rollers (rolling pin, bottle) need a higher spin cap to keep
            // up with a fast belt. Off-axis wobble is still held in check by ApplyStability's damping.
            float radius = RollRadius();
            float rollCap = radius > 0.01f ? (Config.maxSpeed + 4f) / radius : 0f;
            Body.maxAngularVelocity = Mathf.Max(Config.maxAngularVelocity, rollCap);
            if (Shape == RacerShape.Slider)
            {
                // Weeble: a low centre of mass makes the fish roll back onto its belly after a hit.
                Body.centerOfMass = new Vector3(0f, -radius * 0.7f, 0f);
            }
            shieldBubble = Effects.CreateShieldBubble(transform, GetVisualRadius());
            shieldBubble.SetActive(false);
            visual = transform.Find("Model");
            if (visual) visualScale = visual.localScale;
        }

        float RollRadius()
        {
            var col = GetComponent<Collider>();
            if (col is CapsuleCollider cap) return cap.radius * Mathf.Max(transform.lossyScale.y, transform.lossyScale.z);
            if (col is SphereCollider sph) return sph.radius * transform.lossyScale.x;
            return 0.4f;
        }

        float GetVisualRadius()
        {
            var col = GetComponent<Collider>();
            return col ? col.bounds.extents.magnitude : 0.6f;
        }

        void Update()
        {
            if (ShieldTimer > 0f) ShieldTimer -= Time.deltaTime;
            if (controlLossTimer > 0f) controlLossTimer -= Time.deltaTime;
            float massScale = HasShield ? 2.5f : (Super != null && Super.IsActive ? 2f : 1f);
            Body.mass = baseMass * massScale;
            if (shieldBubble)
            {
                shieldBubble.SetActive(HasShield && !IsEliminated);
                if (HasShield) shieldBubble.transform.rotation = Quaternion.identity;
            }

            bool racing = GameManager.Instance != null && GameManager.Instance.IsRacing;
            if (racing && !IsEliminated && treadmill != null && treadmill.IsOffTreadmill(transform.position))
                Eliminate();
        }

        void FixedUpdate()
        {
            if (groundedFrames > 0) groundedFrames--;
            bool racing = GameManager.Instance != null && GameManager.Instance.IsRacing;
            if (IsEliminated || !racing) { SmoothedInput = Vector2.zero; return; }

            // Momentum: the commanded direction eases toward the input, so reversing takes a moment.
            var target = Vector2.ClampMagnitude(MoveInput, 1f) * (ControlLost ? 0.15f : 1f);
            float resp = definition ? definition.responsiveness : 5f;
            SmoothedInput = Vector2.MoveTowards(SmoothedInput, target, resp * Time.fixedDeltaTime);

            ApplyMovement();
            ApplyStability();
            ApplyAntiCamping();
        }

        /// <summary>Continuous seconds spent in a (hidden) no-camp zone: by the console, or in a front corner.</summary>
        public float CampTime { get; private set; }
        public int CampWarnings { get; private set; }
        /// <summary>True from the moment a warning is issued until the racer leaves the zone.</summary>
        public bool CampWarningActive => warnedThisVisit;
        public string EliminationReason { get; private set; }
        public static event Action<Racer, int> CampWarned;
        bool warnedThisVisit;

        /// <summary>
        /// Nobody gets a free safe spot. The zones are invisible: after a few seconds in one you get a warning,
        /// a bit longer and you're disqualified, and a second warning in the same run also disqualifies you.
        /// </summary>
        void ApplyAntiCamping()
        {
            // Obstacle Mode has no rivals to shelter from, and hazards arrive from the front anyway.
            if (treadmill == null || Session.IsObstacle) return;
            var cfg = Config;
            var pos = Body.position;
            float fromFront = treadmill.FrontZ - pos.z;
            float localX = pos.x - treadmill.transform.position.x;
            float fromSide = treadmill.HalfWidth - Mathf.Abs(localX);
            // Side strips only guard the front corners, not the whole rail.
            bool cornerRegion = fromFront < treadmill.beltLength * cfg.campCornerFraction;
            bool inFront = fromFront < cfg.campFrontDepth;
            bool inZone = inFront || (cornerRegion && fromSide < cfg.campSideDepth);

            if (!inZone)
            {
                CampTime = 0f;
                warnedThisVisit = false;
                return;
            }

            CampTime += Time.fixedDeltaTime;
            if (!warnedThisVisit && CampTime >= cfg.campWarnSeconds)
            {
                warnedThisVisit = true;
                CampWarnings++;
                CampWarned?.Invoke(this, CampWarnings);
                if (CampWarnings >= cfg.campMaxWarnings) { Eliminate("CAMPING"); return; }
            }
            if (CampTime >= cfg.campLoseSeconds) Eliminate("CAMPING");
        }

        public float ForceMultiplier
        {
            get
            {
                float m = 1f;
                if (Stamina) m *= Stamina.ForceMultiplier;
                if (Acceleration) m *= Acceleration.ForceMultiplier;
                if (Super) m *= Super.ForceMultiplier;
                return Mathf.Min(m, Config.maxForceMultiplier);
            }
        }

        void ApplyMovement()
        {
            var input = SmoothedInput;
            if (input.sqrMagnitude < 0.0001f) return;

            float accel = definition ? definition.acceleration : 22f;
            float steer = definition ? definition.steering : 18f;
            float maxRel = definition ? definition.maxRelativeSpeed : 7f;
            if (isPlayer)
            {
                // Permanent upgrades: Movement Speed everywhere, Runner Speed on top in Obstacle Mode.
                // These scale the tuna's own push and top speed, never the belt.
                float upgrade = Upgrades.MovementMultiplier * (Session.IsObstacle ? Upgrades.ObstacleRunnerMultiplier : 1f);
                accel *= upgrade;
                steer *= upgrade;
                maxRel *= upgrade;
            }
            float mult = ForceMultiplier;
            float air = Grounded ? 1f : 0.35f;

            var force = new Vector3(input.x * steer, 0f, input.y * accel) * mult * air;

            // Measure speed relative to the belt so the treadmill always wins when a racer stops pushing.
            var beltVel = treadmill ? treadmill.BeltVelocity : Vector3.zero;
            var rel = Body.linearVelocity - beltVel;
            rel.y = 0f;
            var dir = force.normalized;
            if (Vector3.Dot(rel, dir) < maxRel * Mathf.Sqrt(mult))
                Body.AddForce(force, ForceMode.Acceleration);

            if (Shape == RacerShape.Ball)
            {
                // Spin the ball the way it is pushed so it reads as rolling.
                Body.AddTorque(Vector3.Cross(Vector3.up, dir) * force.magnitude * 0.5f, ForceMode.Acceleration);
            }
        }

        /// <summary>
        /// Cans keep their roll axis free but are gently pulled back to lying flat across the belt; spheres just
        /// get clamped spin. Lumpy racers (the apple) get a little random wobble while rolling.
        /// </summary>
        void ApplyStability()
        {
            float stability = definition ? definition.stability : 0.7f;
            if (Shape == RacerShape.Can)
            {
                var axis = transform.right;
                var desired = Vector3.right * Mathf.Sign(Vector3.Dot(axis, Vector3.right) + 0.0001f);
                var correction = Vector3.Cross(axis, desired);
                // Damp spin about everything except the rolling axis, so it can still roll freely.
                var w = Body.angularVelocity;
                var wobbleSpin = w - Vector3.Project(w, axis);
                float k = Mathf.Lerp(8f, 40f, stability);
                float d = Mathf.Lerp(1f, 6f, stability);
                Body.AddTorque(correction * k - wobbleSpin * d, ForceMode.Acceleration);
            }
            else if (Shape == RacerShape.Slider)
            {
                // Stay upright, and swing the nose toward where we're going (input, or drift if idle).
                // Strong righting: a slider on its side is helpless, so this must win against its round collider.
                float tilt = Vector3.Angle(transform.up, Vector3.up);
                float k = Mathf.Lerp(40f, 110f, stability) * (tilt > 35f ? 2f : 1f);
                var upright = Vector3.Cross(transform.up, Vector3.up) * k;
                var heading = SmoothedInput.sqrMagnitude > 0.04f ? new Vector3(SmoothedInput.x, 0f, SmoothedInput.y) : Vector3.forward;
                var flatFwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                float yaw = Vector3.SignedAngle(flatFwd, heading.normalized, Vector3.up) * Mathf.Deg2Rad;
                var turn = Vector3.up * yaw * Mathf.Lerp(4f, 12f, stability);
                Body.AddTorque(upright + turn - Body.angularVelocity * Mathf.Lerp(1.5f, 4f, stability), ForceMode.Acceleration);
            }

            float instability = definition ? definition.rollingInstability : 0f;
            if (instability > 0f && Grounded && Body.linearVelocity.sqrMagnitude > 1f)
            {
                float n = Mathf.PerlinNoise(instabilitySeed, Time.time * 2f) - 0.5f;
                Body.AddForce(Vector3.right * n * instability * 12f, ForceMode.Acceleration);
                Body.AddTorque(Vector3.forward * n * instability * 20f, ForceMode.Acceleration);
            }
            if (Shape == RacerShape.Slider && instability > 0f && Grounded)
            {
                // Fish flop: a quick side-to-side tail wiggle that nudges it around the belt.
                float flop = Mathf.Sin(Time.time * 9f + instabilitySeed) * instability;
                Body.AddTorque(Vector3.up * flop * 18f, ForceMode.Acceleration);
                Body.AddForce(transform.right * flop * 3f, ForceMode.Acceleration);
            }
        }

        void OnCollisionStay(Collision c)
        {
            if (c.collider.GetComponentInParent<Treadmill>() != null) groundedFrames = 3;
        }

        void OnCollisionEnter(Collision c)
        {
            if (c.collider.GetComponentInParent<Treadmill>() != null) { groundedFrames = 3; return; }

            float impact = c.relativeVelocity.magnitude;
            if (impact > 1f) ApplyWobble(c, impact);
            if (impact >= Config.accelerationImpactReset) Impacted?.Invoke(impact);

            var other = c.rigidbody ? c.rigidbody.GetComponent<Racer>() : null;
            if (other == null || IsEliminated || other.IsEliminated) return;
            // Each pair is resolved once, by the racer with the lower id.
            if (racerId > other.racerId) return;

            var flatRel = c.relativeVelocity;
            flatRel.y = 0f;
            float flatImpact = flatRel.magnitude;
            if (flatImpact < 1.2f) return;

            var dir = other.Body.position - Body.position;
            dir.y = 0f;
            dir.Normalize();

            // Exaggerated arcade knockback: the faster racer wins the exchange, bouncy racers rebound harder.
            float mySpeed = Vector3.Dot(Body.linearVelocity, dir);
            float theirSpeed = Vector3.Dot(other.Body.linearVelocity, -dir);
            float share = Mathf.Clamp01(0.5f + (mySpeed - theirSpeed) * 0.12f);
            float myHit = HitStrength(), theirHit = other.HitStrength();
            float push = Mathf.Clamp(flatImpact / 5f, 0.3f, 1.3f);

            other.TakeKnock(dir * myHit * push * share * (1f + other.Bounciness) + Vector3.up * push * 0.4f);
            TakeKnock(-dir * theirHit * push * (1f - share) * (1f + Bounciness) + Vector3.up * push * 0.25f);

            Bumped?.Invoke(this, other, flatImpact);
            Effects.Burst(c.GetContact(0).point, Color.Lerp(Color, other.Color, 0.5f), 14);
            AudioFX.Play(Sfx.Bump, Mathf.Clamp01(flatImpact / 7f), 1.1f);
            Squash(0.25f);
            other.Squash(0.25f);
        }

        float Bounciness => definition ? definition.bounciness : 0.25f;

        float HitStrength()
        {
            float s = definition ? definition.bumpStrength : 4f;
            if (Super != null && Super.IsActive) s *= Config.superHitMultiplier;
            return s;
        }

        /// <summary>Off-centre hits spin the racer: torque from the contact offset and the push direction.</summary>
        void ApplyWobble(Collision c, float impact)
        {
            var contact = c.GetContact(0).point;
            var r = contact - Body.worldCenterOfMass;
            var otherPos = c.rigidbody ? c.rigidbody.worldCenterOfMass : contact - c.GetContact(0).normal;
            var pushDir = (Body.worldCenterOfMass - otherPos).normalized;
            float susceptibility = (definition ? definition.wobbleSusceptibility : 1f) * (isPlayer ? Upgrades.WobbleMultiplier : 1f);
            float resist = Super != null && Super.IsActive ? 0.3f : 1f;
            var torque = Vector3.Cross(r, pushDir) * impact * Config.offCenterTorque * susceptibility * resist;
            Body.AddTorque(torque, ForceMode.VelocityChange);

            if (impact >= Config.severeImpactSpeed && !(Super != null && Super.IsActive))
            {
                float recoveryBonus = GetComponent<AIController>() is AIController ai && ai.Profile ? ai.Profile.recovery : 0.5f;
                controlLossTimer = Config.controlLossSeconds * Mathf.Lerp(1.3f, 0.6f, recoveryBonus);
                if (isPlayer) CameraRig.Shake(0.3f);
            }
        }

        public void TakeKnock(Vector3 velocityChange)
        {
            if (IsEliminated) return;
            // A short immunity window stops one bump from chaining through the whole pack.
            if (Time.time < knockImmuneUntil) return;
            knockImmuneUntil = Time.time + KnockCooldown;
            float taken = definition ? definition.knockbackTaken : 1f;
            if (HasShield) taken *= 0.15f;
            if (Super != null && Super.IsActive) taken *= Config.superKnockbackTaken;
            Body.AddForce(velocityChange * taken, ForceMode.VelocityChange);

            var flat = Body.linearVelocity;
            float vy = flat.y;
            flat.y = 0f;
            if (flat.magnitude > MaxKnockSpeed) Body.linearVelocity = flat.normalized * MaxKnockSpeed + Vector3.up * Mathf.Min(vy, 3f);
            Impacted?.Invoke(velocityChange.magnitude * taken);
        }

        public void Eliminate(string reason = null)
        {
            if (IsEliminated) return;
            IsEliminated = true;
            EliminationReason = reason;
            EliminatedAt = GameManager.Instance ? GameManager.Instance.ElapsedTime : 0f;
            MoveInput = Vector2.zero;
            BoostHeld = false;
            ShieldTimer = 0f;
            Effects.Burst(transform.position, Color, 30);
            AudioFX.Play(isPlayer ? Sfx.Out : Sfx.Fall, 0.8f);
            Eliminated?.Invoke(this);
            // Rivals tumble for a moment, then tidy themselves away; the player stays for the game-over shot.
            if (!isPlayer) StartCoroutine(ShrinkAway(2.5f));
        }

        /// <summary>Extra Life: put the racer back on the belt, briefly shielded.</summary>
        public void Revive(Vector3 position)
        {
            IsEliminated = false;
            EliminationReason = null;
            CampTime = 0f;
            warnedThisVisit = false;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = position;
            transform.position = position;
            Body.rotation = Quaternion.identity;
            transform.rotation = Quaternion.identity;
            ShieldTimer = 2.5f;
            controlLossTimer = 0f;
            Squash(0.4f);
            Effects.Burst(position, new Color(1f, 0.5f, 0.7f), 30);
        }

        IEnumerator ShrinkAway(float delay)
        {
            yield return new WaitForSeconds(delay);
            var start = transform.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime * 3f)
            {
                transform.localScale = start * (1f - t * t);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        /// <summary>Quick cartoon squash-and-stretch on the visual model only, so physics is unaffected.</summary>
        public void Squash(float amount)
        {
            if (visual == null || !isActiveAndEnabled) return;
            if (squashRoutine != null) StopCoroutine(squashRoutine);
            squashRoutine = StartCoroutine(SquashRoutine(amount));
        }

        IEnumerator SquashRoutine(float amount)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime * 5f)
            {
                float s = Mathf.Sin(t * Mathf.PI) * amount * (1f - t);
                visual.localScale = visualScale * (1f + s);
                yield return null;
            }
            visual.localScale = visualScale;
        }
    }
}
