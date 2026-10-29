using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Chasing marquee bulbs: every child renderer takes a turn glowing in sequence, cycling through a palette.
    /// One shared material per palette colour keeps it cheap.
    /// </summary>
    public class MarqueeLights : MonoBehaviour
    {
        public Color[] palette = { new Color(1f, 0.85f, 0.3f), new Color(1f, 0.45f, 0.7f), new Color(0.4f, 0.85f, 1f) };
        public float speed = 6f;
        public int groupSize = 3;

        Renderer[] bulbs;
        Material[] lit, dim;

        void Start()
        {
            bulbs = GetComponentsInChildren<Renderer>();
            lit = new Material[palette.Length];
            dim = new Material[palette.Length];
            for (int i = 0; i < palette.Length; i++)
            {
                lit[i] = Effects.MakeMat(palette[i], 0.8f);
                lit[i].EnableKeyword("_EMISSION");
                lit[i].SetColor("_EmissionColor", palette[i] * 2.2f);
                dim[i] = Effects.MakeMat(Color.Lerp(palette[i], Color.white, 0.5f) * 0.8f, 0.6f);
            }
        }

        void Update()
        {
            if (bulbs == null || bulbs.Length == 0) return;
            int step = Mathf.FloorToInt(Time.time * speed);
            for (int i = 0; i < bulbs.Length; i++)
            {
                int colour = (i / Mathf.Max(1, groupSize) + step / bulbs.Length) % palette.Length;
                bool on = (i + step) % groupSize == 0;
                bulbs[i].sharedMaterial = on ? lit[colour] : dim[colour];
            }
        }
    }
}
