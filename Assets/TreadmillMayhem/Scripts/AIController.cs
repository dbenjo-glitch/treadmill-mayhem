using UnityEngine;

namespace TreadmillMayhem
{
    public enum AIState { Survive, Reposition, AvoidObstacle, CollectBoost, Recover, AttackOpponent, EscapeEdge }

    /// <summary>
    /// State-machine driver for NPC racers. It drives the same Racer inputs as the player (MoveInput, BoostHeld),
    /// so NPCs obey identical physics. Difficulty comes from an <see cref="AIDifficultyProfile"/>: reaction time,
    /// prediction, awareness, mistakes and risk tolerance — not extra speed.
    /// </summary>
    [RequireComponent(typeof(Racer))]
    public class AIController : MonoBehaviour
    {
        [Header("Personality (small per-racer variation)")]
        [Range(-0.15f, 0.15f)] public float aggressionBias;
        [Range(-0.15f, 0.15f)] public float greedBias;
        [Range(-1f, 1f), Tooltip("Preferred lane, -1 = left edge, 1 = right edge.")]
        public float laneBias;

        public AIDifficultyProfile Profile { get; private set; }
        public AIState State { get; private set; }

        Racer racer;
        Treadmill treadmill;
        float nextThink;
        Vector2 targetInput;
        bool boostWanted;
        float wanderSeed;
        static AIDifficultyProfile fallback;

        void Awake()
        {
            racer = GetComponent<Racer>();
            wanderSeed = Random.value * 100f;
        }

        void Start() => treadmill = FindAnyObjectByType<Treadmill>();

        public void SetProfile(AIDifficultyProfile profile) => Profile = profile;

        AIDifficultyProfile P
        {
            get
            {
                if (Profile) return Profile;
                if (fallback == null) fallback = ScriptableObject.CreateInstance<AIDifficultyProfile>();
                return fallback;
            }
        }

        void Update()
        {
            bool racing = GameManager.Instance != null && GameManager.Instance.IsRacing;
            if (racer.IsEliminated || treadmill == null || !racing)
            {
                racer.MoveInput = Vector2.zero;
                racer.BoostHeld = false;
                return;
            }

            if (Time.time >= nextThink)
            {
                nextThink = Time.time + P.reactionTime * Random.Range(0.8f, 1.2f);
                Think();
            }
            float follow = Mathf.Lerp(3f, 12f, P.correctionStrength);
            racer.MoveInput = Vector2.MoveTowards(racer.MoveInput, targetInput, Time.deltaTime * follow);
            racer.BoostHeld = boostWanted;
        }

        // ---------- Decision making ----------

        void Think()
        {
            var p = P;
            var pos = racer.Body.position;
            var predicted = pos + racer.Body.linearVelocity * p.lookahead;
            var est = Vector3.Lerp(pos, predicted, p.predictionAccuracy);

            float half = treadmill.HalfWidth;
            float len = treadmill.beltLength;
            float localX = est.x - treadmill.transform.position.x;
            float fromRear = est.z - treadmill.RearZ;
            float rearDanger = Mathf.InverseLerp(len * Mathf.Max(0.12f, p.rearSafety), 0f, fromRear);
            float stamina = racer.Stamina ? racer.Stamina.Current : 0f;

            // Priority-ordered state selection.
            Obstacle threat = null;
            Component pickup = null;
            Racer victim = null;
            float escapeThreshold = Mathf.Lerp(0.45f, 0.75f, p.riskTolerance);

            // A ball bouncing near the back must still drive for safety rather than "ride out" the bounce
            // (playtests: the Ball NPC was dropping off first, median 18 s).
            bool ballInDanger = racer.Shape == RacerShape.Ball && rearDanger > escapeThreshold * 0.8f && !racer.ControlLost;
            if (!ballInDanger && (racer.ControlLost || IsTumbling())) State = AIState.Recover;
            else if (rearDanger > escapeThreshold || ballInDanger) State = AIState.EscapeEdge;
            else if ((threat = SpotThreat(pos)) != null) State = AIState.AvoidObstacle;
            else if ((pickup = PickPickup(pos, stamina, rearDanger)) != null) State = AIState.CollectBoost;
            else if ((victim = PickVictim(pos)) != null) State = AIState.AttackOpponent;
            else if (Mathf.Abs(fromRear - HomeDistance(len)) > len * 0.15f) State = AIState.Reposition;
            else State = AIState.Survive;

            Vector2 steer;
            boostWanted = false;
            switch (State)
            {
                case AIState.Recover:
                    // Ride out the wobble: small, steady inputs back toward the middle.
                    steer = new Vector2(-Mathf.Sign(localX) * 0.3f, Mathf.Lerp(0.2f, 0.7f, p.recovery));
                    break;

                case AIState.EscapeEdge:
                    steer = new Vector2(-localX / half * 0.5f, 1f);
                    boostWanted = stamina > 10f && Random.value < p.boostUsage;
                    break;

                case AIState.AvoidObstacle:
                {
                    var dx = threat.transform.position.x - pos.x;
                    // Dodge toward whichever side has more room.
                    float side = Mathf.Abs(localX) > half * 0.5f ? -Mathf.Sign(localX) : (dx > 0f ? -1f : 1f);
                    steer = new Vector2(side, 0.45f);
                    break;
                }

                case AIState.CollectBoost:
                {
                    var to = pickup.transform.position - pos;
                    steer = new Vector2(to.x, to.z + 0.5f);
                    break;
                }

                case AIState.AttackOpponent:
                {
                    // Get just in front of the victim, then drive back into them to shove them toward the rear.
                    var vpos = victim.Body.position;
                    bool inFront = pos.z > vpos.z + 0.4f;
                    var aim = inFront ? vpos : vpos + new Vector3(0f, 0f, 1.6f);
                    var to = aim - pos;
                    steer = new Vector2(to.x, to.z);
                    if (inFront && to.magnitude < 2.5f) boostWanted = stamina > 20f && Random.value < p.boostUsage;
                    break;
                }

                default:
                {
                    // Survive / Reposition: hold a home spot. The belt always pulls back, so lean into it.
                    float wander = (Mathf.PerlinNoise(wanderSeed, Time.time * 0.25f) - 0.5f) * half * 0.6f;
                    float homeX = treadmill.transform.position.x + Mathf.Clamp(laneBias * half * 0.5f + wander, -half * 0.7f, half * 0.7f);
                    float homeZ = treadmill.RearZ + HomeDistance(len);
                    float gain = State == AIState.Reposition ? 0.7f : 0.45f;
                    // Ahead of home: just ease off and let the belt bring us back. Never reverse hard toward the
                    // rear edge to reposition; that plus the belt's pull overshoots right off the back.
                    float dz = homeZ - est.z;
                    float forward = dz > 0f ? dz * gain : Mathf.Max(-0.15f, dz * 0.05f);
                    steer = new Vector2((homeX - est.x) * gain, forward + treadmill.speed * 0.08f);
                    steer *= Mathf.Lerp(0.6f, 1f, p.correctionStrength);
                    break;
                }
            }

            // Always respect the side walls a little, and the rear a lot.
            float sideDanger = Mathf.InverseLerp(half * 0.6f, half, Mathf.Abs(localX));
            steer.x -= Mathf.Sign(localX) * sideDanger * 0.8f;
            steer.y += rearDanger * 0.8f;

            // The hidden no-camp zones apply to bots too: step back out before a warning (better AIs notice sooner,
            // weak ones sometimes get warned or disqualified).
            if (racer.CampTime > Mathf.Lerp(4.5f, 1f, p.awareness) || racer.CampWarningActive)
            {
                steer.y = Mathf.Min(steer.y, -0.5f);
                steer.x -= Mathf.Sign(localX) * 0.6f;
            }

            // Imperfection: steering noise and the odd silly decision.
            steer += new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * p.steeringNoise * 0.6f;
            if (Random.value < p.mistakeChance)
            {
                if (Random.value < 0.5f) steer.y = 0f;
                else steer.x += Random.Range(-1f, 1f);
            }

            // Reverse is only allowed deliberately (attacks, camp escape) and never near the rear edge.
            float maxReverse = State == AIState.AttackOpponent || racer.CampTime > 0f ? 0.6f : 0.2f;
            if (rearDanger > 0.2f) maxReverse = 0f;
            steer.y = Mathf.Max(steer.y, -maxReverse);

            targetInput = Vector2.ClampMagnitude(steer, 1f);
        }

        // Home spot sits mid-belt (safer AIs a little further forward), keeping the pack in the thick of the action
        // and well clear of the hidden no-camp zone by the console.
        float HomeDistance(float len) => len * Mathf.Clamp(P.rearSafety + 0.17f, 0.32f, 0.62f);

        bool IsTumbling()
        {
            // Spheres spin whenever they roll, so for them "tumbling" means bouncing clear of the belt.
            if (racer.Shape == RacerShape.Ball)
                return !racer.Grounded && racer.Body.linearVelocity.y > 1.5f;
            // Sliders are "tumbling" when knocked off their feet.
            if (racer.Shape == RacerShape.Slider)
                return Vector3.Angle(racer.transform.up, Vector3.up) > 40f;
            // Cans roll about their own axis; any other spin is a wobble.
            var w = racer.Body.angularVelocity;
            w -= Vector3.Project(w, racer.transform.right);
            return w.magnitude > 6f;
        }

        Obstacle SpotThreat(Vector3 pos)
        {
            if (Random.value > P.awareness) return null;
            float range = 1.5f + P.lookahead * (treadmill.speed + 1.5f);
            var hits = Physics.SphereCastAll(pos + Vector3.up * 0.1f, 0.55f, Vector3.forward, range, ~0, QueryTriggerInteraction.Ignore);
            Obstacle nearest = null;
            float best = float.MaxValue;
            foreach (var h in hits)
            {
                var ob = h.collider.GetComponentInParent<Obstacle>();
                if (ob != null && h.distance < best) { best = h.distance; nearest = ob; }
            }
            return nearest;
        }

        Component PickPickup(Vector3 pos, float stamina, float rearDanger)
        {
            float greed = Mathf.Clamp01(P.pickupGreed + greedBias);
            if (greed <= 0.05f) return null;
            // Lower stamina makes orbs more tempting; low-risk racers won't chase into the danger zone.
            float need = 1f - stamina / GameConfig.Current.maxStamina;
            float range = 2f + 5f * greed * (0.5f + need);
            Component best = StaminaOrb.Nearest(pos, range);
            if (best == null) best = PowerUp.Nearest(pos, range * 0.7f);
            if (best == null) return null;
            float targetFromRear = best.transform.position.z - treadmill.RearZ;
            if (targetFromRear < treadmill.beltLength * (0.3f - P.riskTolerance * 0.2f)) return null;
            return Random.value < greed + 0.2f ? best : null;
        }

        Racer PickVictim(Vector3 pos)
        {
            float aggression = Mathf.Clamp01(P.aggression + aggressionBias);
            if (GameManager.Instance.ElapsedTime < 4f || Random.value > aggression) return null;
            Racer best = null;
            float bestScore = 0.5f;
            foreach (var r in GameManager.Instance.Racers)
            {
                if (r == racer || r.IsEliminated) continue;
                var d = r.Body.position - pos;
                if (d.magnitude > 5f) continue;
                // Victims close to the rear are worth the most; top AIs also like picking on the player.
                float danger = Mathf.InverseLerp(treadmill.RearZ + treadmill.beltLength * 0.5f, treadmill.RearZ, r.Body.position.z);
                float score = danger + (r.isPlayer ? aggression * 0.3f : 0f) - d.magnitude * 0.08f;
                if (score > bestScore) { bestScore = score; best = r; }
            }
            return best;
        }
    }
}
