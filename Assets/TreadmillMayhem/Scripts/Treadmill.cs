using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Conveyor belt. The belt body is kinematic and teleported back each step, so PhysX sees a surface
    /// moving at BeltVelocity and friction drags (and rolls) everything sitting on it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Treadmill : MonoBehaviour
    {
        [Header("Belt shape (local space, belt runs along Z)")]
        public float beltWidth = 5f;
        public float beltLength = 12f;
        public float beltTopY = 0f;

        [Header("Speed")]
        public float speed = 2.5f;
        public float extraPull = 2.2f;

        [Header("Visuals")]
        public Renderer beltRenderer;
        public float textureTilesPerMeter = 0.5f;
        [Tooltip("Visual-only belt scroll used while the physics belt is stopped (menu backdrop).")]
        public float idleScrollSpeed;

        public Vector3 BeltVelocity => -transform.forward * speed;
        public float FrontZ => transform.position.z + beltLength * 0.5f;
        public float RearZ => transform.position.z - beltLength * 0.5f;
        public float HalfWidth => beltWidth * 0.5f;
        public float TopY => transform.position.y + beltTopY;

        Rigidbody body;
        Material beltMat;
        float scroll;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            if (beltRenderer) beltMat = beltRenderer.material;
        }

        ParticleSystem rearDust;

        void Start()
        {
            // Little puffs flicking off the back roller; their rate tracks belt speed.
            var go = new GameObject("RearDust");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.05f, -beltLength * 0.5f);
            go.transform.localRotation = Quaternion.Euler(20f, 180f, 0f);
            rearDust = go.AddComponent<ParticleSystem>();
            rearDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rearDust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new Color(1f, 1f, 1f, 0.7f);
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = rearDust.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(beltWidth, 0.05f, 0.05f);
            var sol = rearDust.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            rearDust.Play();
        }

        void FixedUpdate()
        {
            var p = body.position;
            body.position = p - BeltVelocity * Time.fixedDeltaTime;
            body.MovePosition(p);
        }

        void Update()
        {
            if (rearDust)
            {
                var em = rearDust.emission;
                em.rateOverTime = speed > 0.1f ? 4f + speed * 5f : 0f;
            }
            if (!beltMat) return;
            scroll += (speed > 0.01f ? speed : idleScrollSpeed) * Time.deltaTime * textureTilesPerMeter;
            var offset = new Vector2(0f, scroll % 1f);
            if (beltMat.HasProperty("_BaseMap")) beltMat.SetTextureOffset("_BaseMap", offset);
            else beltMat.mainTextureOffset = offset;
        }

        void OnCollisionStay(Collision c)
        {
            var rb = c.rigidbody;
            if (rb == null || rb.isKinematic) return;
            // Friction alone lets a rolling racer coast in place; this keeps steady pressure toward the back.
            rb.AddForce(BeltVelocity.normalized * extraPull * Mathf.Lerp(0.6f, 1.6f, speed / 10f), ForceMode.Acceleration);
        }

        public bool IsOffTreadmill(Vector3 worldPos)
        {
            // Elimination planes: below the belt, behind the rear roller, or well past the side rails.
            // Wobbling or briefly bouncing off the surface never counts.
            var cfg = GameConfig.Current;
            var local = worldPos - transform.position;
            if (worldPos.y < TopY - cfg.eliminationDepth) return true;
            if (local.z < -beltLength * 0.5f - cfg.eliminationRearMargin) return true;
            if (Mathf.Abs(local.x) > HalfWidth + 2f) return true;
            return false;
        }

        public Vector3 RandomPointOnBelt(float frontBias, float margin = 0.6f)
        {
            float x = Random.Range(-HalfWidth + margin, HalfWidth - margin);
            float z = Mathf.Lerp(RearZ + beltLength * 0.35f, FrontZ - margin - 0.6f, frontBias);
            return new Vector3(transform.position.x + x, TopY, z);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * beltTopY, new Vector3(beltWidth, 0.05f, beltLength));
        }
    }
}
