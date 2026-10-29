using System;
using System.Collections.Generic;

namespace TreadmillMayhem
{
    public class AchievementDef
    {
        public string id;
        public string name;
        public string description;
        public int reward;          // one-time Tuna Coins
        public float target;        // for progress bars (0 = no progress tracking)
        public Func<float> progress; // lifetime/best progress shown on the list
    }

    /// <summary>
    /// One-time goals that pay Tuna Coins once. Unlocks are saved immediately and never pay out twice,
    /// however many times the player restarts.
    /// </summary>
    public static class Achievements
    {
        public static event Action<AchievementDef> Unlocked;

        public static readonly List<AchievementDef> All = new List<AchievementDef>
        {
            new AchievementDef { id = "first_steps", name = "FIRST STEPS", description = "Survive 10 seconds in a race.", reward = 50, target = 10, progress = () => SaveSystem.Data.bestTime },
            new AchievementDef { id = "tuna_runner", name = "TUNA RUNNER", description = "Survive 30 seconds in a race.", reward = 100, target = 30, progress = () => SaveSystem.Data.bestTime },
            new AchievementDef { id = "treadmill_master", name = "TREADMILL MASTER", description = "Survive 60 seconds in a race.", reward = 250, target = 60, progress = () => SaveSystem.Data.bestTime },
            new AchievementDef { id = "speed_demon", name = "SPEED DEMON", description = "Stay accelerated for 15 seconds in one run.", reward = 150, target = 15 },
            new AchievementDef { id = "boost_collector", name = "BOOST COLLECTOR", description = "Collect 10 stamina orbs in one run.", reward = 150, target = 10 },
            new AchievementDef { id = "unstoppable", name = "UNSTOPPABLE TUNA", description = "Reach 10,000 points in a race.", reward = 300, target = 10000, progress = () => SaveSystem.Data.highScore },
            new AchievementDef { id = "last_one", name = "LAST CAN STANDING", description = "Outlast every opponent in a race.", reward = 300 },
            new AchievementDef { id = "first_upgrade", name = "SHOPPER", description = "Buy your first upgrade.", reward = 50 },
            new AchievementDef { id = "decorator", name = "INTERIOR DESIGNER", description = "Buy a decoration for the arena.", reward = 75 },
            new AchievementDef { id = "new_look", name = "NEW LOOK", description = "Unlock a new character.", reward = 100 },
            new AchievementDef { id = "dodger", name = "DODGER", description = "Survive 30 seconds in Obstacle Free Play.", reward = 100, target = 30, progress = () => SaveSystem.Data.obstacleBestTime },
            new AchievementDef { id = "level_5", name = "HALFWAY THERE", description = "Clear Obstacle Level 5.", reward = 200, target = 5, progress = () => SaveSystem.Data.obstacleLevel - 1 },
            new AchievementDef { id = "level_12", name = "TUNA LEGEND", description = "Clear every Obstacle Level.", reward = 1000, target = 12, progress = () => SaveSystem.Data.obstacleLevel - 1 },
            new AchievementDef { id = "rich_tuna", name = "RICH TUNA", description = "Earn 5,000 Tuna Coins in total.", reward = 250, target = 5000, progress = () => SaveSystem.Data.lifetimeCoins },
        };

        public static bool IsUnlocked(string id) => SaveSystem.Data.achievements.Contains(id);

        /// <summary>Unlocks and pays out once. Returns true only the first time.</summary>
        public static bool Unlock(string id)
        {
            if (IsUnlocked(id)) return false;
            var def = All.Find(a => a.id == id);
            if (def == null) return false;
            SaveSystem.Data.achievements.Add(id);
            SaveSystem.AddCoins(def.reward);   // also saves
            Unlocked?.Invoke(def);
            return true;
        }

        /// <summary>Checks the lifetime/best-based achievements (cheap; safe to call often).</summary>
        public static void CheckLifetime()
        {
            foreach (var a in All)
                if (a.progress != null && a.target > 0 && !IsUnlocked(a.id) && a.progress() >= a.target)
                    Unlock(a.id);
        }

        public static int UnlockedCount => SaveSystem.Data.achievements.Count;
    }
}
