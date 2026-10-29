using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// How smart an AI racer is. Difficulty changes decisions, reaction latency, prediction and risk —
    /// never raw speed, which stays the same physics as the player.
    /// </summary>
    [CreateAssetMenu(menuName = "Treadmill Mayhem/AI Difficulty", fileName = "AI_Difficulty")]
    public class AIDifficultyProfile : ScriptableObject
    {
        public string displayName = "NORMAL";
        public Color color = Color.white;
        [TextArea] public string blurb;

        [Header("Perception")]
        [Tooltip("Seconds between decisions.")]
        public float reactionTime = 0.3f;
        [Tooltip("How far ahead (seconds) the racer predicts positions.")]
        public float lookahead = 0.7f;
        [Range(0f, 1f), Tooltip("0 = uses current position only, 1 = fully trusts its prediction.")]
        public float predictionAccuracy = 0.5f;
        [Range(0f, 1f), Tooltip("Chance to notice an obstacle or threat each decision.")]
        public float awareness = 0.55f;

        [Header("Control")]
        [Range(0f, 1f), Tooltip("Random steering noise.")]
        public float steeringNoise = 0.35f;
        [Range(0f, 1f), Tooltip("Chance per decision to do something dumb.")]
        public float mistakeChance = 0.12f;
        [Range(0f, 1f), Tooltip("How hard it fights back after a hit.")]
        public float recovery = 0.5f;
        [Range(0f, 1f), Tooltip("Strength of position corrections.")]
        public float correctionStrength = 0.6f;

        [Header("Strategy")]
        [Range(0f, 1f), Tooltip("Fraction of belt length it tries to keep between itself and the rear edge.")]
        public float rearSafety = 0.3f;
        [Range(0f, 1f), Tooltip("Willingness to go hunting other racers.")]
        public float aggression = 0.2f;
        [Range(0f, 1f), Tooltip("Interest in orbs and power-ups.")]
        public float pickupGreed = 0.4f;
        [Range(0f, 1f), Tooltip("How cleverly it spends stamina boosts.")]
        public float boostUsage = 0.3f;
        [Range(0f, 1f), Tooltip("Risk tolerance: high values take chances near edges to attack or grab pickups.")]
        public float riskTolerance = 0.3f;
    }
}
