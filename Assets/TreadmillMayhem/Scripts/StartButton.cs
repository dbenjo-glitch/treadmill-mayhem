using System.Collections;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// The big yellow console button used to start a run. Built at runtime on top of the model's own button,
    /// it glows to invite a press, sinks when pressed, and lets the camera take over.
    /// Local +Y is the button's face normal.
    /// </summary>
    public class StartButton : MonoBehaviour
    {
        public float radius = 0.58f;

        public bool Pressed { get; private set; }
        public Vector3 Normal => transform.up;

        Transform cap;
        Material capMat;
        Vector3 capRest;
        bool hovered;

        static readonly Color Yellow = new Color(1f, 0.8f, 0.08f);

        void Awake()
        {
            var bezel = Part(PrimitiveType.Cylinder, transform, "Bezel", new Vector3(0f, 0.02f, 0f), new Vector3(radius * 2.35f, 0.04f, radius * 2.35f), Effects.MakeMat(new Color(0.16f, 0.16f, 0.2f), 0.7f));
            Part(PrimitiveType.Cylinder, transform, "Ring", new Vector3(0f, 0.06f, 0f), new Vector3(radius * 2.15f, 0.05f, radius * 2.15f), Effects.MakeMat(new Color(0.85f, 0.55f, 0.05f), 0.8f));

            cap = new GameObject("Cap").transform;
            cap.SetParent(transform, false);
            cap.localPosition = new Vector3(0f, 0.1f, 0f);
            capRest = cap.localPosition;

            capMat = Effects.MakeMat(Yellow, 0.75f);
            capMat.EnableKeyword("_EMISSION");
            // Deeper gold on the sides so the cap reads as a thick, pressable button rather than a flat disc.
            var sideMat = Effects.MakeMat(new Color(0.95f, 0.55f, 0.03f), 0.7f);
            Part(PrimitiveType.Cylinder, cap, "Body", new Vector3(0f, 0.07f, 0f), new Vector3(radius * 2f, 0.07f, radius * 2f), sideMat);
            Part(PrimitiveType.Sphere, cap, "Dome", new Vector3(0f, 0.14f, 0f), new Vector3(radius * 2f, radius * 0.55f, radius * 2f), capMat);
            // Cartoon specular highlight.
            var shine = Part(PrimitiveType.Sphere, cap, "Shine", new Vector3(-radius * 0.35f, 0.26f, radius * 0.3f), new Vector3(radius * 0.45f, 0.04f, radius * 0.25f), Effects.MakeMat(new Color(1f, 1f, 0.92f), 1f));
            shine.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);

            // One trigger the menu can raycast against; generous so it is easy to hit on touch screens.
            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius * 1.3f;
            col.center = new Vector3(0f, 0.15f, 0f);
            bezel.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static GameObject Part(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public void SetHover(bool on) => hovered = on;

        void Update()
        {
            if (Pressed) return;
            // Breathing glow and a gentle bob invite the press.
            float t = Time.unscaledTime;
            float glow = (Mathf.Sin(t * 3.5f) * 0.5f + 0.5f) * (hovered ? 0.35f : 0.18f) + (hovered ? 0.15f : 0f);
            capMat.SetColor("_EmissionColor", Yellow * glow);
            float s = 1f + Mathf.Sin(t * 3.5f) * 0.025f + (hovered ? 0.05f : 0f);
            cap.localScale = new Vector3(s, 1f, s);
        }

        public bool IsHit(Ray ray) =>
            Physics.Raycast(ray, out var hit, 200f, ~0, QueryTriggerInteraction.Collide) && hit.collider.gameObject == gameObject;

        /// <summary>Plays the press animation; the callback fires once the button has bottomed out.</summary>
        public void Press(System.Action onBottomedOut)
        {
            if (Pressed) return;
            Pressed = true;
            StartCoroutine(PressRoutine(onBottomedOut));
        }

        IEnumerator PressRoutine(System.Action onBottomedOut)
        {
            AudioFX.Play(Sfx.Click, 1f, 0.7f);
            var down = capRest - new Vector3(0f, 0.09f, 0f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.09f)
            {
                cap.localPosition = Vector3.Lerp(capRest, down, t);
                cap.localScale = new Vector3(1f + t * 0.08f, 1f - t * 0.25f, 1f + t * 0.08f);
                yield return null;
            }
            capMat.SetColor("_EmissionColor", Yellow * 0.9f);
            Effects.Burst(transform.position + Normal * 0.3f, Yellow, 30);
            AudioFX.Play(Sfx.Go, 0.9f);
            CameraRig.Shake(0.12f);
            onBottomedOut?.Invoke();

            yield return new WaitForSecondsRealtime(0.15f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                float e = 1f - Mathf.Pow(1f - t, 3f);
                cap.localPosition = Vector3.Lerp(down, capRest, e);
                cap.localScale = Vector3.Lerp(new Vector3(1.08f, 0.75f, 1.08f), Vector3.one, e);
                capMat.SetColor("_EmissionColor", Yellow * Mathf.Lerp(0.9f, 0f, t));
                yield return null;
            }
        }
    }
}
