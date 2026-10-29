using System;

namespace TreadmillMayhem
{
    /// <summary>
    /// Seven-day login calendar. Claiming on consecutive days walks the streak up to the big day-7 prize;
    /// missing a day starts again from day 1. Uses the device's local calendar date.
    /// </summary>
    public static class DailyRewards
    {
        public struct Reward { public int coins, gears; }

        public static readonly Reward[] Calendar =
        {
            new Reward { coins = 100 },
            new Reward { coins = 150 },
            new Reward { coins = 100, gears = 25 },
            new Reward { coins = 250 },
            new Reward { coins = 150, gears = 50 },
            new Reward { coins = 400 },
            new Reward { coins = 1000, gears = 100 },
        };

        static DateTime Today => DateTime.Now.Date;
        static DateTime LastClaim => SaveSystem.Data.lastDailyClaimTicks > 0 ? new DateTime(SaveSystem.Data.lastDailyClaimTicks).Date : DateTime.MinValue;

        public static bool Available => LastClaim < Today;

        /// <summary>True if the streak survives (claimed yesterday or today).</summary>
        static bool StreakAlive => LastClaim >= Today.AddDays(-1);

        /// <summary>0-based calendar slot that today's claim would pay (or that was paid today).</summary>
        public static int TodayIndex
        {
            get
            {
                int streak = StreakAlive ? SaveSystem.Data.dailyStreak : 0;
                if (!Available) streak = Math.Max(0, streak - 1);
                return streak % Calendar.Length;
            }
        }

        public static Reward Claim()
        {
            if (!Available) return default;
            int streak = StreakAlive ? SaveSystem.Data.dailyStreak : 0;
            var r = Calendar[streak % Calendar.Length];
            SaveSystem.Data.dailyStreak = streak + 1;
            SaveSystem.Data.lastDailyClaimTicks = Today.Ticks;
            if (r.coins > 0) SaveSystem.AddCoins(r.coins);
            if (r.gears > 0) SaveSystem.AddGears(r.gears);
            SaveSystem.Save();
            return r;
        }
    }
}
