using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Obstacle Mode currency pickup: a spinning cyan gear that rides the belt and is pulled in by the Gear Magnet.</summary>
    public class Gear : MonoBehaviour
    {
        static Material mat;
        Treadmill treadmill;
        float baseY, born;

        public static Gear Spawn(Vector3 pos)
        {
            var go = new GameObject("Gear");
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            if (mat == null)
            {
                mat = Effects.MakeMat(new Color(0.5f, 0.9f, 1f), 0.85f);
                mat.SetFloat("_Metallic", 0.7f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.1f, 0.35f, 0.45f));
            }
            // Hub plus teeth: a cog silhouette from primitives.
            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(hub.GetComponent<Collider>());
            hub.transform.SetParent(go.transform, false);
            hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hub.transform.localScale = new Vector3(0.45f, 0.06f, 0.45f);
            hub.GetComponent<Renderer>().sharedMaterial = mat;
            for (int i = 0; i < 8; i++)
            {
                var tooth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(tooth.GetComponent<Collider>());
                tooth.transform.SetParent(go.transform, false);
                float a = i * 45f;
                tooth.transform.localRotation = Quaternion.Euler(0f, 0f, a);
                tooth.transform.localPosition = Quaternion.Euler(0f, 0f, a) * Vector3.up * 0.26f;
                tooth.transform.localScale = new Vector3(0.12f, 0.12f, 0.1f);
                tooth.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return go.AddComponent<Gear>();
        }

        void Start()
        {
            treadmill = FindAnyObjectByType<Treadmill>();
            baseY = transform.position.y;
            born = Time.time;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            float age = Time.time - born;
            transform.localScale = Vector3.one * Mathf.Min(1f, age * 5f);
            transform.Rotate(0f, 200f * Time.deltaTime, 0f, Space.World);
            if (!treadmill) return;
            var p = transform.position + treadmill.BeltVelocity * Time.deltaTime;
            p.y = baseY + Mathf.Sin(age * 4f) * 0.08f;

            // Gear Magnet upgrade pulls gears toward the player.
            var player = GameManager.Instance ? GameManager.Instance.Player : null;
            if (player && !player.IsEliminated)
            {
                var to = player.Body.position - p;
                to.y = 0f;
                float range = Upgrades.MagnetRange;
                if (to.magnitude < range) p += to.normalized * Time.deltaTime * 8f;
            }
            transform.position = p;
            if (p.z < treadmill.RearZ - 0.5f) Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            var racer = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<Racer>() : null;
            if (racer == null || !racer.isPlayer || racer.IsEliminated) return;
            Effects.Burst(transform.position, new Color(0.5f, 0.9f, 1f), 10);
            AudioFX.Play(Sfx.Coin, 0.6f, 0.8f);
            GameManager.Instance?.AddGear(transform.position);
            Destroy(gameObject);
        }
    }
}
