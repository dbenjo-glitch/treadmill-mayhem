using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Can: rolls about its long (X) axis, kept lying across the belt.
    /// Ball: free-rolling sphere.
    /// Slider: doesn't roll; stays upright, skids, turns to face where it's going and flops (the fish).
    /// </summary>
    public enum RacerShape { Can, Ball, Slider }

    /// <summary>
    /// One racer's look and physics profile. To swap in a new model (e.g. a fresh Tripo export), drop it in
    /// Assets/TreadmillMayhem/Models, assign it to <see cref="visualModel"/>, and run
    /// Tools ▸ Treadmill Mayhem ▸ Build Scene. Physics colliders are generated separately from the visual.
    /// </summary>
    [CreateAssetMenu(menuName = "Treadmill Mayhem/Racer Definition", fileName = "Racer")]
    public class RacerDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Racer";
        public Color color = Color.white;
        public RacerShape shape = RacerShape.Can;

        [Header("Visual (replaceable)")]
        public GameObject visualModel;
        [Tooltip("Uniform scale applied to the model; the collider is fitted to the scaled model bounds.")]
        public float visualSize = 1f;
        public Vector3 visualRotation;

        [Header("Physics")]
        public float mass = 1f;
        [Range(0f, 1f)] public float friction = 0.6f;
        [Range(0f, 1f)] public float bounciness = 0.25f;
        public float linearDamping = 0.15f;
        public float angularDamping = 0.4f;

        [Header("Movement")]
        [Tooltip("Forward/back push (acceleration, m/s²).")]
        public float acceleration = 22f;
        [Tooltip("Sideways push (acceleration, m/s²).")]
        public float steering = 18f;
        [Tooltip("Max speed relative to the belt from its own pushing.")]
        public float maxRelativeSpeed = 7f;
        [Tooltip("How quickly input direction can change (higher = snappier).")]
        public float responsiveness = 5f;

        [Header("Stability")]
        [Range(0f, 1f), Tooltip("How strongly a can rights itself after wobbling.")]
        public float stability = 0.7f;
        [Tooltip("Multiplier on off-center impact torque.")]
        public float wobbleSusceptibility = 1f;
        [Range(0f, 1f), Tooltip("Random wobble while rolling (the apple's lumpy roll).")]
        public float rollingInstability = 0f;

        [Header("Collisions")]
        public float bumpStrength = 4f;
        public float knockbackTaken = 1f;
    }
}
