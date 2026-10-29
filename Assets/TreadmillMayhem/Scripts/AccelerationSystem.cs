using System;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Rewards holding a steady line: after N uninterrupted seconds of meaningful input in roughly one direction,
    /// acceleration mode turns on (force bonus, trail, bonus points). Sharp turns, letting go, or a real hit reset it.
    /// </summary>
    [RequireComponent(typeof(Racer))]
    public class AccelerationSystem : MonoBehaviour
    {
        public event Action Activated;
        public event Action Deactivated;

        public bool IsActive { get; private set; }
        public float HoldTime { get; private set; }
        /// <summary>Seconds of straight-line input needed; the Acceleration upgrade shortens it for the player.</summary>
        public float RequiredSeconds => racer && racer.isPlayer
            ? Upgrades.AccelHoldSeconds(GameConfig.Current.accelerationHoldSeconds)
            : GameConfig.Current.accelerationHoldSeconds;
        public float Progress => Mathf.Clamp01(HoldTime / RequiredSeconds);
        public float ForceMultiplier => IsActive ? GameConfig.Current.accelerationForceMultiplier : 1f;
        public float ActiveSeconds { get; private set; }

        Racer racer;
        Vector2 heldDirection;
        TrailRenderer streaks;

        void Awake()
        {
            racer = GetComponent<Racer>();
            racer.Impacted += OnImpacted;
        }

        void Start()
        {
            streaks = Effects.CreateTrail(transform, new Color(1f, 0.85f, 0.25f), 0.35f);
        }

        void OnDestroy()
        {
            if (racer) racer.Impacted -= OnImpacted;
        }

        void OnImpacted(float strength)
        {
            if (strength >= GameConfig.Current.accelerationImpactReset) ResetTimer();
        }

        void ResetTimer()
        {
            HoldTime = 0f;
            heldDirection = Vector2.zero;
            if (IsActive)
            {
                IsActive = false;
                Deactivated?.Invoke();
            }
        }

        void FixedUpdate()
        {
            var cfg = GameConfig.Current;
            bool racing = GameManager.Instance != null && GameManager.Instance.IsRacing;
            var input = racer.MoveInput;
            if (!racing || racer.IsEliminated || racer.ControlLost || input.magnitude < cfg.accelerationMinInput)
            {
                ResetTimer();
                return;
            }

            var dir = input.normalized;
            if (heldDirection == Vector2.zero) heldDirection = dir;
            if (Vector2.Angle(heldDirection, dir) > cfg.accelerationMaxAngle)
            {
                ResetTimer();
                heldDirection = dir;
                return;
            }
            // Let the reference direction drift slowly so gentle corrections don't count as turning.
            heldDirection = Vector2.Lerp(heldDirection, dir, Time.fixedDeltaTime * 0.5f).normalized;

            HoldTime += Time.fixedDeltaTime;
            if (!IsActive && HoldTime >= RequiredSeconds)
            {
                IsActive = true;
                Activated?.Invoke();
                if (racer.isPlayer) AudioFX.Play(Sfx.Accel, 0.7f);
            }
            if (IsActive) ActiveSeconds += Time.fixedDeltaTime;
        }

        void Update()
        {
            if (streaks) streaks.emitting = IsActive && !racer.IsEliminated && !(racer.Super != null && racer.Super.IsActive);
        }
    }
}
