using System;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// 0–100 stamina. Normal movement is free; holding boost (Shift) while moving amplifies the racer's forces
    /// and drains stamina. Refilled by stamina orbs.
    /// </summary>
    [RequireComponent(typeof(Racer))]
    public class StaminaSystem : MonoBehaviour
    {
        public event Action<float> Changed;

        public float Current { get; private set; }
        /// <summary>Stamina Capacity upgrade raises the player's cap.</summary>
        public float Max => GameConfig.Current.maxStamina * (racer && racer.isPlayer ? Upgrades.StaminaMultiplier : 1f);
        public float Normalized => Max > 0f ? Current / Max : 0f;
        public bool IsBoosting { get; private set; }

        /// <summary>Boost Power upgrade scales the extra push the boost adds (not the base force).</summary>
        public float ForceMultiplier
        {
            get
            {
                if (!IsBoosting) return 1f;
                float extra = GameConfig.Current.boostForceMultiplier - 1f;
                return 1f + extra * (racer && racer.isPlayer ? Upgrades.BoostMultiplier : 1f);
            }
        }

        Racer racer;

        void Awake()
        {
            racer = GetComponent<Racer>();
            Current = GameConfig.Current.startingStamina * (racer && racer.isPlayer ? Upgrades.StaminaMultiplier : 1f);
        }

        public void Add(float amount)
        {
            Current = Mathf.Clamp(Current + amount, 0f, Max);
            Changed?.Invoke(Current);
        }

        void FixedUpdate()
        {
            bool racing = GameManager.Instance != null && GameManager.Instance.IsRacing;
            bool moving = racer.MoveInput.sqrMagnitude > 0.04f;
            IsBoosting = racing && !racer.IsEliminated && racer.BoostHeld && moving && Current > 0f;
            if (!IsBoosting) return;

            Current = Mathf.Max(0f, Current - GameConfig.Current.boostDrainPerSecond * Time.fixedDeltaTime);
            Changed?.Invoke(Current);
            if (Current <= 0f) IsBoosting = false;
        }
    }
}
