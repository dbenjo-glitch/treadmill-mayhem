using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Gentle breathing scale for "tap to start" prompts.</summary>
    public class Pulse : MonoBehaviour
    {
        void Update() => transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.05f);
    }
}
