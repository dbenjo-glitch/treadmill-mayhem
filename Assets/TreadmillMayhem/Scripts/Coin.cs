using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Spinning gold coin that rides the belt. Only the player can collect it.</summary>
    public class Coin : MonoBehaviour
    {
        public const int Points = 25;
        static Material goldMat;

        Treadmill treadmill;
        Transform visual;
        float baseY;
        float spawnTime;

        public static Coin Spawn(Vector3 pos)
        {
            var go = new GameObject("Coin");
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;

            if (goldMat == null)
            {
                goldMat = Effects.MakeMat(new Color(1f, 0.78f, 0.1f), 0.85f);
                goldMat.SetFloat("_Metallic", 0.6f);
                goldMat.EnableKeyword("_EMISSION");
                goldMat.SetColor("_EmissionColor", new Color(0.45f, 0.3f, 0f));
            }

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(disc.GetComponent<Collider>());
            disc.name = "Disc";
            disc.transform.SetParent(go.transform, false);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
            disc.GetComponent<Renderer>().sharedMaterial = goldMat;

            var face = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(face.GetComponent<Collider>());
            face.transform.SetParent(disc.transform, false);
            face.transform.localScale = new Vector3(0.6f, 1.15f, 0.6f);
            face.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(new Color(1f, 0.92f, 0.45f));

            var c = go.AddComponent<Coin>();
            c.visual = disc.transform;
            return c;
        }

        void Start()
        {
            treadmill = FindAnyObjectByType<Treadmill>();
            baseY = transform.position.y;
            spawnTime = Time.time;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            float age = Time.time - spawnTime;
            transform.localScale = Vector3.one * Mathf.Min(1f, age * 5f);
            transform.Rotate(0f, 240f * Time.deltaTime, 0f, Space.World);

            if (treadmill)
            {
                var p = transform.position + treadmill.BeltVelocity * Time.deltaTime;
                p.y = baseY + Mathf.Sin(age * 4f + p.z) * 0.08f;
                transform.position = p;
                if (p.z < treadmill.RearZ - 0.5f) Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var racer = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<Racer>() : null;
            if (racer == null || !racer.isPlayer || racer.IsEliminated) return;
            Effects.Burst(transform.position, new Color(1f, 0.85f, 0.2f), 10);
            AudioFX.Play(Sfx.Coin, 0.6f);
            GameManager.Instance?.AddCoin();
            Destroy(gameObject);
        }
    }
}
