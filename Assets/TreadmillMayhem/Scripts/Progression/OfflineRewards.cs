using System;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Coins trickle in while the player is away (capped so it rewards returning, not abandoning).
    /// The rate scales with the Offline Earnings upgrade from Obstacle Mode.
    /// </summary>
    public static class OfflineRewards
    {
        public const float MaxHours = 8f;
        public const float MinMinutes = 2f;

        /// <summary>Coins earned since the last save, or 0 if too little time passed / first launch.</summary>
        public static int Pending(out TimeSpan away)
        {
            away = TimeSpan.Zero;
            long last = SaveSystem.Data.lastSeenUtcTicks;
            if (last <= 0) return 0;
            away = DateTime.UtcNow - new DateTime(last, DateTimeKind.Utc);
            if (away.TotalMinutes < MinMinutes) return 0;
            double hours = Math.Min(away.TotalHours, MaxHours);
            float rate = Upgrades.OfflineCoinsPerHour(Upgrades.Level(Upgrades.OOffline));
            return Mathf.FloorToInt((float)(hours * rate));
        }

        public static float RatePerHour => Upgrades.OfflineCoinsPerHour(Upgrades.Level(Upgrades.OOffline));

        /// <summary>Credits the pending coins (optionally doubled) and resets the clock.</summary>
        public static void Claim(int amount)
        {
            if (amount > 0) SaveSystem.AddCoins(amount);
            else SaveSystem.Save();
        }

        public static string Describe(TimeSpan away)
        {
            if (away.TotalHours >= MaxHours) return "8h+ (max)";
            if (away.TotalHours >= 1) return Mathf.FloorToInt((float)away.TotalHours) + "h " + away.Minutes + "m";
            return Mathf.Max(1, Mathf.FloorToInt((float)away.TotalMinutes)) + "m";
        }
    }
}
