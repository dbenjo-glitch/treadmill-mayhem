using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Gentle sine drift and sway for decor (balloons, signs, speaker cones).</summary>
    public class Bob : MonoBehaviour
    {
        public Vector3 amplitude = new Vector3(0f, 0.15f, 0f);
        public float frequency = 0.6f;
        public float swayDegrees = 4f;
        [Tooltip("Pulse the scale instead of moving (speaker cones).")]
        public float scalePulse;

        Vector3 basePos, baseScale;
        Quaternion baseRot;
        float seed;

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            baseScale = transform.localScale;
            seed = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time * frequency * Mathf.PI * 2f + seed;
            transform.localPosition = basePos + new Vector3(amplitude.x * Mathf.Sin(t * 0.7f), amplitude.y * Mathf.Sin(t), amplitude.z * Mathf.Cos(t * 0.8f));
            if (swayDegrees > 0f) transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 0.9f) * swayDegrees, 0f, Mathf.Cos(t * 0.6f) * swayDegrees);
            if (scalePulse > 0f) transform.localScale = baseScale * (1f + Mathf.Max(0f, Mathf.Sin(t)) * scalePulse);
        }
    }
}
