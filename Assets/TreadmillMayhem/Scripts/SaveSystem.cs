using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TreadmillMayhem
{
    [Serializable]
    public class RunRecord
    {
        public string id;
        public string name;
        public int score;
        public float time;
        public int orbs;
        public float accelerationTime;
        public int coins;
        public int place;
        public string difficulty;
        public string date;
        public string mode;
    }

    [Serializable]
    public class UpgradeLevel
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class SaveData
    {
        // Race mode.
        public int highScore;
        public float bestTime;
        /// <summary>Tuna Coins: the main permanent currency (field name kept for old saves).</summary>
        public int totalCoins;
        public int lifetimeCoins;
        public int runsPlayed;

        // Obstacle mode.
        public int gears;
        public int obstacleLevel = 1;
        public float obstacleBestTime;
        public int obstacleHighScore;

        // Progression.
        public List<UpgradeLevel> upgrades = new List<UpgradeLevel>();
        public List<string> achievements = new List<string>();
        public List<string> decorOwned = new List<string>();
        public List<string> decorHidden = new List<string>();
        public string selectedCharacter = "Tuna";
        public List<string> charactersOwned = new List<string>();
        public int dailyStreak;
        public long lastDailyClaimTicks;
        public bool tutorialDone;
        public bool haptics = true;

        // Offline rewards.
        public long lastSeenUtcTicks;

        // Preferences.
        public string lastName = "Player";
        public int difficulty = 1;
        public float masterVolume = 1f;
        public float musicVolume = 0.6f;
        public float sfxVolume = 1f;

        public List<RunRecord> runs = new List<RunRecord>();
    }

    /// <summary>
    /// Local persistence: one JSON file in Application.persistentDataPath. Values are clamped on load so a
    /// corrupted or hand-edited file can never produce invalid state.
    /// </summary>
    public static class SaveSystem
    {
        public const int MaxRuns = 10;
        static SaveData data;

        public static string FilePath => Path.Combine(Application.persistentDataPath, "treadmill_mayhem_save.json");

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        /// <summary>Drops the cached data so the next access re-reads the file (used after tools rewrite it).</summary>
        public static void ReloadFromDisk() => data = null;

        static void Load()
        {
            MigrateLegacySave();
            try
            {
                data = File.Exists(FilePath) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Treadmill Mayhem: save file unreadable, starting fresh. " + e.Message);
                data = null;
            }
            if (data == null) data = new SaveData();
            Sanitize(data);
        }

        /// <summary>
        /// Development saves lived under the placeholder "DefaultCompany/My project" folder; once the real
        /// company and product names are set, copy that save across so no progress is lost.
        /// </summary>
        static void MigrateLegacySave()
        {
            try
            {
                if (File.Exists(FilePath)) return;
                var parent = Directory.GetParent(Application.persistentDataPath);
                if (parent == null || parent.Parent == null) return;
                var legacy = Path.Combine(parent.Parent.FullName, "DefaultCompany", "My project", "treadmill_mayhem_save.json");
                if (!File.Exists(legacy)) return;
                Directory.CreateDirectory(Application.persistentDataPath);
                File.Copy(legacy, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Treadmill Mayhem: could not migrate old save. " + e.Message);
            }
        }

        static void Sanitize(SaveData d)
        {
            d.runs ??= new List<RunRecord>();
            d.upgrades ??= new List<UpgradeLevel>();
            d.achievements ??= new List<string>();
            d.decorOwned ??= new List<string>();
            d.decorHidden ??= new List<string>();
            d.charactersOwned ??= new List<string>();
            // Older versions never recorded a best time; derive it from the leaderboard runs.
            foreach (var r in d.runs) if (r != null && r.mode != "OBSTACLE") d.bestTime = Mathf.Max(d.bestTime, r.time);
            if (string.IsNullOrEmpty(d.selectedCharacter)) d.selectedCharacter = "Tuna";
            d.dailyStreak = Mathf.Clamp(d.dailyStreak, 0, 10000);
            // Corrupt or future dates (clock moved back) would break or lock the daily calendar.
            if (d.lastDailyClaimTicks < 0 || d.lastDailyClaimTicks > DateTime.Now.Ticks) d.lastDailyClaimTicks = 0;
            d.totalCoins = Mathf.Max(0, d.totalCoins);
            d.gears = Mathf.Max(0, d.gears);
            d.highScore = Mathf.Max(0, d.highScore);
            d.obstacleLevel = Mathf.Clamp(d.obstacleLevel, 1, Levels.Count);
            d.difficulty = Mathf.Clamp(d.difficulty, 0, 4);
            d.masterVolume = Mathf.Clamp01(d.masterVolume);
            d.musicVolume = Mathf.Clamp01(d.musicVolume);
            d.sfxVolume = Mathf.Clamp01(d.sfxVolume);
            foreach (var u in d.upgrades)
            {
                var def = Upgrades.Find(u.id);
                u.level = def != null ? Mathf.Clamp(u.level, 0, def.maxLevel) : 0;
            }
        }

        public static void Save()
        {
            try
            {
                Data.lastSeenUtcTicks = DateTime.UtcNow.Ticks;
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
            }
            catch (Exception e) { Debug.LogWarning("Treadmill Mayhem: could not write save file. " + e.Message); }
        }

        // ---------- Currency ----------

        public static event Action CurrencyChanged;

        public static void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Data.totalCoins += amount;
            Data.lifetimeCoins += amount;
            Save();
            CurrencyChanged?.Invoke();
        }

        public static void AddGears(int amount)
        {
            if (amount <= 0) return;
            Data.gears += amount;
            Save();
            CurrencyChanged?.Invoke();
        }

        public static bool TrySpend(Currency currency, int amount)
        {
            if (amount < 0) return false;
            if (currency == Currency.Coins)
            {
                if (Data.totalCoins < amount) return false;
                Data.totalCoins -= amount;
            }
            else
            {
                if (Data.gears < amount) return false;
                Data.gears -= amount;
            }
            Save();
            CurrencyChanged?.Invoke();
            return true;
        }

        public static int Balance(Currency currency) => currency == Currency.Coins ? Data.totalCoins : Data.gears;

        // ---------- Upgrades ----------

        public static int UpgradeLevelOf(string id)
        {
            foreach (var u in Data.upgrades) if (u.id == id) return u.level;
            return 0;
        }

        public static void SetUpgradeLevel(string id, int level)
        {
            foreach (var u in Data.upgrades) if (u.id == id) { u.level = level; Save(); return; }
            Data.upgrades.Add(new UpgradeLevel { id = id, level = level });
            Save();
        }

        // ---------- Leaderboard ----------

        /// <summary>Records a finished run. Returns its leaderboard position (1-based) or -1 if outside the top 10.</summary>
        public static int AddRun(RunRecord run)
        {
            run.id = Guid.NewGuid().ToString("N");
            var d = Data;
            d.runsPlayed++;
            if (run.mode != "OBSTACLE")
            {
                d.highScore = Mathf.Max(d.highScore, run.score);
                d.bestTime = Mathf.Max(d.bestTime, run.time);
            }
            d.runs.Add(run);
            d.runs.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : b.time.CompareTo(a.time));
            int pos = d.runs.IndexOf(run);
            if (d.runs.Count > MaxRuns) d.runs.RemoveRange(MaxRuns, d.runs.Count - MaxRuns);
            Save();
            return pos < MaxRuns ? pos + 1 : -1;
        }

        public static void RenameRun(string id, string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
            Data.lastName = name;
            foreach (var r in Data.runs) if (r.id == id) r.name = name;
            Save();
        }
    }
}
