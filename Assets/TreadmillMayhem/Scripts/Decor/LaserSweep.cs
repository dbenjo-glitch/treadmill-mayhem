using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Sweeps a laser beam back and forth across the stage.</summary>
    public class LaserSweep : MonoBehaviour
    {
        public float sweepDegrees = 35f;
        public float speed = 0.7f;
        public float phase;
        Quaternion baseRot;

        void Start() => baseRot = transform.localRotation;

        void Update()
        {
            float a = Mathf.Sin(Time.time * speed + phase) * sweepDegrees;
            float b = Mathf.Cos(Time.time * speed * 0.7f + phase) * sweepDegrees * 0.4f;
            transform.localRotation = baseRot * Quaternion.Euler(b, a, 0f);
        }
    }
}
