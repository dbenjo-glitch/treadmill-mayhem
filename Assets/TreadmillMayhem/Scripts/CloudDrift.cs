using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Slow sideways drift with wrap-around, for background clouds.</summary>
    public class CloudDrift : MonoBehaviour
    {
        public float speed = 0.6f;
        public float wrapX = 70f;
        float bobSeed;

        void Start() => bobSeed = Random.value * 10f;

        void Update()
        {
            var p = transform.position;
            p.x += speed * Time.deltaTime;
            if (p.x > wrapX) p.x = -wrapX;
            p.y += Mathf.Sin(Time.time * 0.4f + bobSeed) * 0.002f;
            transform.position = p;
        }
    }
}
