using System;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Run scoring, accumulated as floats so frame timing never skews results:
    /// survival points per second, orb pickups, and acceleration bonus per second.
    /// Every point earned also feeds the player's Super Speed meter.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        /// <summary>Points awarded, with a world position for floating text (null = no popup).</summary>
        public event Action<int, Vector3?> PointsPopup;

        public float RawScore { get; private set; }
        public int Score => Mathf.FloorToInt(RawScore);
        public float SurvivalTime { get; private set; }
        public int OrbsCollected { get; private set; }
        public float AccelerationTime { get; private set; }

        SuperSpeedSystem super;
        Racer player;
        float accelPopupBank;

        public void Begin(Racer playerRacer)
        {
            player = playerRacer;
            super = player ? player.Super : null;
            RawScore = SurvivalTime = AccelerationTime = 0f;
            OrbsCollected = 0;
            accelPopupBank = 0f;
        }

        /// <summary>Called every frame while the run is live.</summary>
        public void Tick(float dt)
        {
            var cfg = GameConfig.Current;
            SurvivalTime += dt;
            Earn(cfg.survivalPointsPerSecond * dt);

            if (player && player.Acceleration && player.Acceleration.IsActive)
            {
                AccelerationTime += dt;
                float bonus = cfg.accelerationPointsPerSecond * dt;
                Earn(bonus);
                // Show the bonus as one "+250" popup per full second earned.
                accelPopupBank += bonus;
                if (accelPopupBank >= cfg.accelerationPointsPerSecond)
                {
                    accelPopupBank -= cfg.accelerationPointsPerSecond;
                    PointsPopup?.Invoke(Mathf.RoundToInt(cfg.accelerationPointsPerSecond), player.transform.position);
                }
            }
            else accelPopupBank = 0f;
        }

        public void AddOrb(Vector3 where)
        {
            OrbsCollected++;
            int pts = GameConfig.Current.orbPoints;
            Earn(pts);
            PointsPopup?.Invoke(pts, where);
        }

        /// <summary>One-off bonus points with a popup (e.g. Obstacle Mode dodges).</summary>
        public void AddBonus(int points, Vector3 where)
        {
            Earn(points);
            PointsPopup?.Invoke(points, where);
        }

        void Earn(float points)
        {
            RawScore += points;
            if (super) super.AddPoints(points);
        }
    }
}
