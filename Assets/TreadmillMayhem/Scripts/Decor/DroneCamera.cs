using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>A camera drone that hovers beside the belt, drifts after the player and keeps its lens on them.</summary>
    public class DroneCamera : MonoBehaviour
    {
        public Transform[] rotors;
        Vector3 home;

        void Start() => home = transform.position;

        void Update()
        {
            var player = GameManager.Instance ? GameManager.Instance.Player : null;
            var focus = player && !player.IsEliminated ? player.transform.position : new Vector3(0f, 2.3f, -2f);
            float t = Time.time;
            var target = home + new Vector3(Mathf.Sin(t * 0.5f) * 1.5f, Mathf.Sin(t * 1.3f) * 0.3f, (focus.z - home.z) * 0.35f);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 1.5f);
            var look = Quaternion.LookRotation(focus - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 3f);
            if (rotors != null) foreach (var r in rotors) if (r) r.Rotate(0f, 1440f * Time.deltaTime, 0f, Space.Self);
        }
    }
}
