using System;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Super Speed: earned points fill a meter; each full meter stores a charge (max set in config).
    /// Activating gives a few seconds of big propulsion, heavy knockback resistance and harder hits.
    /// It never makes the racer invincible or stops them falling, and it doesn't use stamina.
    /// </summary>
    [RequireComponent(typeof(Racer))]
    public class SuperSpeedSystem : MonoBehaviour
    {
        public event Action ChargeReady;
        public event Action Activated;
        public event Action Ended;

        public int Charges { get; private set; }
        public float Progress { get; private set; }
        public bool IsActive => activeTimer > 0f;
        public float Remaining => activeTimer;
        public bool CanActivate => Charges > 0 && !IsActive;
        /// <summary>Boost Power also strengthens Super Speed's extra push.</summary>
        public float ForceMultiplier => IsActive ? 1f + (GameConfig.Current.superForceMultiplier - 1f) * Upgrades.BoostMultiplier : 1f;

        Racer racer;
        float activeTimer;
        TrailRenderer trail;

        void Awake() => racer = GetComponent<Racer>();

        void Start()
        {
            trail = Effects.CreateTrail(transform, new Color(0.65f, 0.4f, 1f), 0.9f);
            trail.time = 0.6f;
        }

        /// <summary>Feeds newly earned points into the meter.</summary>
        public void AddPoints(float points)
        {
            var cfg = GameConfig.Current;
            if (Charges >= cfg.maxSuperCharges) { Progress = cfg.pointsPerSuperCharge; return; }
            Progress += points;
            while (Progress >= cfg.pointsPerSuperCharge && Charges < cfg.maxSuperCharges)
            {
                Progress -= cfg.pointsPerSuperCharge;
                Charges++;
                ChargeReady?.Invoke();
            }
            if (Charges >= cfg.maxSuperCharges) Progress = cfg.pointsPerSuperCharge;
        }

        public bool TryActivate()
        {
            if (!CanActivate || racer.IsEliminated) return false;
            Charges--;
            if (Charges < GameConfig.Current.maxSuperCharges) Progress = 0f;
            activeTimer = GameConfig.Current.superDuration;
            Activated?.Invoke();
            return true;
        }

        void Update()
        {
            if (activeTimer > 0f)
            {
                activeTimer -= Time.deltaTime;
                if (activeTimer <= 0f || racer.IsEliminated)
                {
                    activeTimer = 0f;
                    Ended?.Invoke();
                }
            }
            if (trail) trail.emitting = IsActive;
        }
    }
}
