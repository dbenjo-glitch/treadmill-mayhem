using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>All gameplay balance in one inspector-editable asset (Assets/TreadmillMayhem/Config/GameConfig).</summary>
    [CreateAssetMenu(menuName = "Treadmill Mayhem/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        static GameConfig current;

        /// <summary>The active config; falls back to defaults so nothing breaks if the asset is missing.</summary>
        public static GameConfig Current
        {
            get
            {
                if (current == null) current = CreateInstance<GameConfig>();
                return current;
            }
            set => current = value;
        }

        [Header("Belt speed curve: start + (max - start) * (1 - e^(-t / timeConstant))")]
        public float startSpeed = 1.4f;
        public float maxSpeed = 11f;
        [Tooltip("Seconds for the belt to cover ~63% of the way from start to max speed.")]
        public float speedTimeConstant = 60f;
        public float secondsPerSpeedLevel = 15f;

        [Header("Scoring")]
        public float survivalPointsPerSecond = 50f;
        public int orbPoints = 100;
        public float accelerationPointsPerSecond = 250f;

        [Header("Stamina")]
        public float maxStamina = 100f;
        public float startingStamina = 50f;
        public float orbStaminaRestore = 25f;
        public float boostDrainPerSecond = 20f;
        public float boostForceMultiplier = 1.7f;

        [Header("Straight-line acceleration")]
        public float accelerationHoldSeconds = 3f;
        [Tooltip("Max angle (degrees) the input may wander from the held direction.")]
        public float accelerationMaxAngle = 22f;
        public float accelerationMinInput = 0.5f;
        [Tooltip("Impacts at or above this relative speed reset the timer.")]
        public float accelerationImpactReset = 2.5f;
        public float accelerationForceMultiplier = 1.3f;

        [Header("Super Speed")]
        public int pointsPerSuperCharge = 2500;
        public int maxSuperCharges = 1;
        public float superDuration = 3f;
        public float superForceMultiplier = 2.2f;
        [Tooltip("Knockback taken while Super Speed is active (1 = normal).")]
        public float superKnockbackTaken = 0.3f;
        [Tooltip("Collision strength dealt while Super Speed is active.")]
        public float superHitMultiplier = 1.8f;

        [Header("Force stacking")]
        [Tooltip("Caps stamina boost × acceleration × Super Speed so impulses never get silly.")]
        public float maxForceMultiplier = 3.4f;

        [Header("Wobble")]
        public float offCenterTorque = 1.2f;
        public float severeImpactSpeed = 6f;
        public float controlLossSeconds = 0.6f;
        public float maxAngularVelocity = 18f;

        [Header("Anti-camping")]
        [Tooltip("Depth (m) of the no-camp strip in front of the console.")]
        public float campFrontDepth = 2.4f;
        [Tooltip("Width (m) of the no-camp strips along the side rails, in the front corners only.")]
        public float campSideDepth = 0.9f;
        [Range(0.1f, 0.5f), Tooltip("How far back from the front the corner strips reach, as a fraction of belt length.")]
        public float campCornerFraction = 0.3f;
        [Tooltip("Continuous seconds in a no-camp zone before a warning is given.")]
        public float campWarnSeconds = 5f;
        [Tooltip("Continuous seconds in a no-camp zone before the racer is disqualified.")]
        public float campLoseSeconds = 7f;
        [Tooltip("Total warnings in one run that disqualify the racer.")]
        public int campMaxWarnings = 2;

        [Header("Elimination")]
        [Tooltip("Metres below the belt surface that count as fallen.")]
        public float eliminationDepth = 1f;
        [Tooltip("Metres behind the rear roller that count as fallen.")]
        public float eliminationRearMargin = 1.5f;

        [Header("Spawning")]
        public bool obstaclesEnabled = true;
        public Vector2 obstacleInterval = new Vector2(5f, 1.8f);
        public Vector2 orbInterval = new Vector2(2.5f, 4.5f);
        public Vector2 coinInterval = new Vector2(2f, 3.5f);
        public Vector2 powerUpInterval = new Vector2(8f, 13f);
        [Tooltip("Seconds over which hazard frequency ramps from easy to hard.")]
        public float hazardRampSeconds = 90f;

        public float BeltSpeedAt(float t) =>
            startSpeed + (maxSpeed - startSpeed) * (1f - Mathf.Exp(-t / Mathf.Max(1f, speedTimeConstant)));
    }
}
