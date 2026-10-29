using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Glowing cyan orb that rides the belt. Any racer can grab it for stamina; the player also scores.</summary>
    public class StaminaOrb : MonoBehaviour
    {
        public static readonly List<StaminaOrb> Active = new List<StaminaOrb>();
        static readonly Color Cyan = new Color(0.25f, 0.95f, 1f);
        static Material orbMat;

        Treadmill treadmill;
        Transform core;
        float baseY, spawnTime;

        public static StaminaOrb Spawn(Vector3 pos)
        {
            var go = new GameObject("StaminaOrb");
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            if (orbMat == null)
            {
                orbMat = Effects.MakeMat(Cyan, 0.9f);
                orbMat.EnableKeyword("_EMISSION");
                orbMat.SetColor("_EmissionColor", Cyan * 1.6f);
            }
            var coreGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(coreGo.GetComponent<Collider>());
            coreGo.transform.SetParent(go.transform, false);
            coreGo.transform.localScale = Vector3.one * 0.38f;
            coreGo.GetComponent<Renderer>().sharedMaterial = orbMat;
            coreGo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(halo.GetComponent<Collider>());
            halo.transform.SetParent(go.transform, false);
            halo.transform.localScale = Vector3.one * 0.7f;
            halo.GetComponent<Renderer>().sharedMaterial = Effects.MakeMat(new Color(Cyan.r, Cyan.g, Cyan.b, 0.22f), 0.9f, true);
            halo.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Cyan;
            light.range = 2.2f;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;

            var orb = go.AddComponent<StaminaOrb>();
            orb.core = coreGo.transform;
            return orb;
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
            float pulse = 1f + Mathf.Sin(age * 6f) * 0.12f;
            transform.localScale = Vector3.one * Mathf.Min(1f, age * 4f) * pulse;
            if (treadmill)
            {
                var p = transform.position + treadmill.BeltVelocity * Time.deltaTime;
                p.y = baseY + Mathf.Sin(age * 3f) * 0.1f;
                transform.position = p;
                if (p.z < treadmill.RearZ - 0.3f) Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var racer = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<Racer>() : null;
            if (racer == null || racer.IsEliminated || racer.Stamina == null) return;
            racer.Stamina.Add(GameConfig.Current.orbStaminaRestore);
            Effects.Burst(transform.position, Cyan, 20);
            if (racer.isPlayer)
            {
                AudioFX.Play(Sfx.Orb, 0.8f);
                GameManager.Instance?.OnPlayerOrb(transform.position);
            }
            Destroy(gameObject);
        }

        public static StaminaOrb Nearest(Vector3 pos, float maxDist)
        {
            StaminaOrb best = null;
            float bestD = maxDist;
            foreach (var o in Active)
            {
                float d = Vector3.Distance(o.transform.position, pos);
                if (d < bestD) { bestD = d; best = o; }
            }
            return best;
        }
    }
}
