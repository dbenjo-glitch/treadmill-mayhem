using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Blinks a renderer's emission on and off (the "LIVE" light on the billboard).</summary>
    public class Blink : MonoBehaviour
    {
        public Color color = new Color(1f, 0.2f, 0.25f);
        public float period = 1.2f;
        [Range(0f, 1f)] public float duty = 0.6f;

        Material mat;

        void Start()
        {
            var r = GetComponent<Renderer>();
            mat = Effects.MakeMat(color, 0.8f);
            mat.EnableKeyword("_EMISSION");
            r.sharedMaterial = mat;
        }

        void Update()
        {
            bool on = Mathf.Repeat(Time.time / period, 1f) < duty;
            mat.SetColor("_EmissionColor", on ? color * 2.5f : Color.black);
        }
    }
}
