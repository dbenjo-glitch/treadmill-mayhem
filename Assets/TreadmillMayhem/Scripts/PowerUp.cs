using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    public enum PowerUpType { Shield, Shockwave }

    /// <summary>Floating pickup that rides the belt toward the back edge.</summary>
    public class PowerUp : MonoBehaviour
    {
        public static readonly List<PowerUp> Active = new List<PowerUp>();

        public PowerUpType type;
        Treadmill treadmill;
        float baseY;
        float spawnTime;

        public static Color ColorFor(PowerUpType t) => t == PowerUpType.Shield ? new Color(0.35f, 0.75f, 1f) : new Color(1f, 0.25f, 0.8f);
        public static string LabelFor(PowerUpType t) => t == PowerUpType.Shield ? "SHIELD!" : "SHOCKWAVE!";

        public static PowerUp Spawn(PowerUpType type, Vector3 pos)
        {
            var go = new GameObject("PowerUp_" + type);
            go.transform.position = pos;
            var p = go.AddComponent<PowerUp>();
            p.type = type;

            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var visual = GameObject.CreatePrimitive(type == PowerUpType.Shield ? PrimitiveType.Sphere : PrimitiveType.Cube);
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.55f;
            visual.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            visual.GetComponent<Renderer>().sharedMaterial = Effects.SharedMat(ColorFor(type));

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            ring.transform.localScale = new Vector3(0.9f, 0.02f, 0.9f);
            ring.GetComponent<Renderer>().sharedMaterial = Effects.MakeMat(new Color(1f, 1f, 1f, 0.5f), 0.2f, true);
            return p;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

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
            transform.localScale = Vector3.one * Mathf.Min(1f, age * 4f) * (1f + Mathf.Sin(Time.time * 6f) * 0.06f);
            if (transform.childCount > 0) transform.GetChild(0).Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);

            if (treadmill)
            {
                var p = transform.position + treadmill.BeltVelocity * Time.deltaTime;
                p.y = baseY + Mathf.Sin(age * 3f) * 0.12f;
                transform.position = p;
                if (p.z < treadmill.RearZ - 0.5f) Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var racer = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<Racer>() : null;
            if (racer == null || racer.IsEliminated) return;
            Apply(racer);
            Effects.Burst(transform.position, ColorFor(type), 24);
            AudioFX.Play(type == PowerUpType.Shockwave ? Sfx.Shockwave : Sfx.Pickup, racer.isPlayer ? 0.9f : 0.4f);
            racer.Squash(0.35f);
            GameManager.Instance?.OnPowerUpCollected(racer, type);
            Destroy(gameObject);
        }

        void Apply(Racer racer)
        {
            switch (type)
            {
                case PowerUpType.Shield:
                    racer.ShieldTimer = 5f;
                    break;
                case PowerUpType.Shockwave:
                    foreach (var r in GameManager.Instance.Racers)
                    {
                        if (r == racer || r.IsEliminated) continue;
                        var d = r.Body.position - racer.Body.position;
                        d.y = 0f;
                        float dist = d.magnitude;
                        if (dist > 5f) continue;
                        r.TakeKnock(d.normalized * Mathf.Lerp(11f, 4f, dist / 5f) + Vector3.up * 2.5f);
                    }
                    Effects.Burst(racer.Body.position, ColorFor(type), 60);
                    CameraRig.Shake(0.35f);
                    break;
            }
        }

        public static PowerUp Nearest(Vector3 pos, float maxDist)
        {
            PowerUp best = null;
            float bestD = maxDist;
            foreach (var p in Active)
            {
                float d = Vector3.Distance(p.transform.position, pos);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }
    }
}
