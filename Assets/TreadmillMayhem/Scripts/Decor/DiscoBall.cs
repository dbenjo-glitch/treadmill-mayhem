using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Spinning mirror ball: rotates, and swings coloured spotlights around the stage.</summary>
    public class DiscoBall : MonoBehaviour
    {
        public float spinDegrees = 40f;
        public Light[] beams;
        Color[] palette = { new Color(1f, 0.4f, 0.7f), new Color(0.4f, 0.8f, 1f), new Color(1f, 0.9f, 0.4f), new Color(0.6f, 1f, 0.5f) };

        void Update()
        {
            transform.Rotate(0f, spinDegrees * Time.deltaTime, 0f, Space.World);
            if (beams == null) return;
            for (int i = 0; i < beams.Length; i++)
            {
                if (!beams[i]) continue;
                float a = Time.time * 0.6f + i * Mathf.PI * 2f / beams.Length;
                var target = new Vector3(Mathf.Cos(a) * 9f, 0f, Mathf.Sin(a) * 9f - 2f);
                beams[i].transform.rotation = Quaternion.LookRotation(target - beams[i].transform.position);
                beams[i].color = palette[(i + Mathf.FloorToInt(Time.time * 0.5f)) % palette.Length];
            }
        }
    }
}
