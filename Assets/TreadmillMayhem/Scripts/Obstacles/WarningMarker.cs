using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Pulsing red target on the belt showing where a crate is about to land. Rides with the belt.</summary>
    public class WarningMarker : MonoBehaviour
    {
        float life, age;
        Treadmill treadmill;
        Transform ring;
        static Material mat;

        public static void Spawn(Vector3 pos, float seconds, Treadmill tm)
        {
            var go = new GameObject("WarningMarker");
            go.transform.position = pos;
            if (mat == null)
            {
                mat = Effects.MakeMat(new Color(1f, 0.2f, 0.25f, 0.65f), 0.2f, true);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.2f));
            }
            var r = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(r.GetComponent<Collider>());
            r.transform.SetParent(go.transform, false);
            r.transform.localScale = new Vector3(1.4f, 0.01f, 1.4f);
            r.GetComponent<Renderer>().sharedMaterial = mat;
            r.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var m = go.AddComponent<WarningMarker>();
            m.life = seconds;
            m.treadmill = tm;
            m.ring = r.transform;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (treadmill) transform.position += treadmill.BeltVelocity * Time.deltaTime;
            float pulse = 1f + Mathf.Sin(age * 18f) * 0.12f;
            float shrink = Mathf.Lerp(1.6f, 0.9f, age / life);
            ring.localScale = new Vector3(shrink * pulse, 0.01f, shrink * pulse);
            if (age >= life + 0.1f) Destroy(gameObject);
        }
    }
}
