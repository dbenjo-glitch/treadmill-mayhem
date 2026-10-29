using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreadmillMayhem
{
    /// <summary>
    /// Automated playtest: N simulated players of evenly spread skill play real runs across every mode,
    /// sped up, and the results are written to Temp/Playtest/ (report.md + runs.jsonl).
    /// The player's save file is backed up first and restored at the end, so testing never touches real progress.
    /// Start it in Play mode with <see cref="Begin"/> (the editor menu does this for you).
    /// </summary>
    public class PlaytestRunner : MonoBehaviour
    {
        [Serializable]
        public class RunLog
        {
            public int run;
            public string mode;
            public string character;
            public float skill;
            public string skillBand;
            public string difficulty;
            public int level;
            public bool levelCleared;
            public float time;
            public int score;
            public int place;
            public int racers;
            public string outcome;
            public string cause;
            public int coins;
            public int gears;
            public int orbs;
            public int superUses;
            public float minFps;
            public float avgFps;
            public int errors;
            public string firstError;
            public List<string> aiOuts = new List<string>();
            public List<string> stuck = new List<string>();
        }

        class Plan { public GameMode mode; public float skill; public int difficulty; public int level; public int character; }

        public static PlaytestRunner Instance { get; private set; }
        public static bool Running => Instance != null;
        public static string OutDir => Path.Combine(Application.dataPath, "..", "Temp", "Playtest");

        const float MaxRunSeconds = 150f;

        readonly List<Plan> plans = new List<Plan>();
        readonly List<RunLog> logs = new List<RunLog>();
        float timeScale = 3f;
        string saveBackup;
        RunLog current;
        int errorCount;
        string firstError;

        bool hadRunInBackground;
        int onlyMode = -1;

        /// <param name="onlyMode">-1 = mixed (50% Race, 25% Free Play, 25% Levels), otherwise a GameMode index.</param>
        public static void Begin(int count = 50, float speed = 3f, int seed = 20261028, int onlyMode = -1)
        {
            if (Instance != null || GameManager.Instance == null) return;
            var go = new GameObject("PlaytestRunner");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PlaytestRunner>();
            Instance.timeScale = speed;
            Instance.onlyMode = onlyMode;
            // Keep simulating when the editor window loses focus (otherwise the batch crawls).
            Instance.hadRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            Instance.MakePlans(count, seed);
            Instance.StartCoroutine(Instance.RunAll());
        }

        void MakePlans(int count, int seed)
        {
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                var mode = onlyMode >= 0 ? (GameMode)onlyMode : (i % 4) switch { 2 => GameMode.ObstacleFree, 3 => GameMode.ObstacleLevels, _ => GameMode.Race };
                plans.Add(new Plan
                {
                    mode = mode,
                    skill = count > 1 ? i / (float)(count - 1) : 0.5f,
                    difficulty = rng.Next(0, 5),
                    level = 1 + rng.Next(0, Levels.Count),
                    character = rng.Next(0, 1000),
                });
            }
            // Shuffle so modes and skills interleave (a crash late in the batch still leaves a fair sample).
            for (int i = plans.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (plans[i], plans[j]) = (plans[j], plans[i]); }
        }

        void OnEnable() => Application.logMessageReceived += OnLog;
        void OnDisable() => Application.logMessageReceived -= OnLog;

        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            errorCount++;
            if (firstError == null) firstError = message.Length > 200 ? message.Substring(0, 200) : message;
        }

        IEnumerator RunAll()
        {
            try { saveBackup = File.Exists(SaveSystem.FilePath) ? File.ReadAllText(SaveSystem.FilePath) : null; }
            catch (Exception e) { Debug.LogWarning("Playtest: could not back up save: " + e.Message); }
            Directory.CreateDirectory(OutDir);
            // Keep a copy on disk straight away, in case the editor is stopped mid-batch.
            if (saveBackup != null) File.WriteAllText(Path.Combine(OutDir, "save_backup.json"), saveBackup);
            File.WriteAllText(Path.Combine(OutDir, "runs.jsonl"), "");

            try
            {
                for (int i = 0; i < plans.Count; i++)
                {
                    yield return RunOne(i + 1, plans[i]);
                    File.AppendAllText(Path.Combine(OutDir, "runs.jsonl"), JsonUtility.ToJson(current) + "\n");
                    WriteReport(false);
                }
            }
            finally
            {
                // Runs even if a run throws, so the player's real progress always comes back.
                Time.timeScale = 1f;
                RestoreSave();
            }
            WriteReport(true);
            Debug.Log("Playtest finished: " + logs.Count + " runs. Report: " + Path.GetFullPath(Path.Combine(OutDir, "report.md")));
            Session.Mode = GameMode.Race;
            if (GameManager.Instance != null) GameManager.Instance.MainMenu();
            Instance = null;
            Destroy(gameObject);
        }

        IEnumerator RunOne(int number, Plan plan)
        {
            Session.Mode = plan.mode;
            Session.Level = plan.level;
            SaveSystem.Data.difficulty = plan.difficulty;
            // Clean upgrade slate so runs are comparable. (Levels are picked via Session.Level directly,
            // so unlock progress and its achievements are left alone.)
            SaveSystem.Data.upgrades.Clear();
            // First-timer bots play as brand-new players (beginner safety net on); everyone else as veterans.
            SaveSystem.Data.runsPlayed = plan.skill < 0.25f ? 0 : 50;
            // Play as a random character (every racer gets coverage); restored with the save afterwards.
            var roster = Characters.AllPrefabs(GameManager.Instance);
            string character = roster.Count > 0 ? roster[plan.character % roster.Count].name : Characters.Default;
            if (!SaveSystem.Data.charactersOwned.Contains(character)) SaveSystem.Data.charactersOwned.Add(character);
            SaveSystem.Data.selectedCharacter = character;

            bool loaded = false;
            void OnLoaded(Scene s, LoadSceneMode m) => loaded = true;
            SceneManager.sceneLoaded += OnLoaded;
            GameManager.Instance.Retry();
            float wait = 0f;
            while (!loaded && wait < 10f) { wait += Time.unscaledDeltaTime; yield return null; }
            SceneManager.sceneLoaded -= OnLoaded;
            yield return null; // let GameManager.Start run

            var gm = GameManager.Instance;
            current = new RunLog
            {
                run = number,
                mode = plan.mode.ToString(),
                character = gm != null && gm.Player != null ? gm.Player.name : "?",
                skill = Mathf.Round(plan.skill * 100f) / 100f,
                skillBand = plan.skill < 0.2f ? "first-timer" : plan.skill < 0.5f ? "casual" : plan.skill < 0.8f ? "regular" : "expert",
                difficulty = plan.mode == GameMode.Race && gm != null && gm.Difficulty ? gm.Difficulty.displayName : "-",
                level = plan.mode == GameMode.ObstacleLevels ? plan.level : 0,
            };
            errorCount = 0;
            firstError = null;
            if (gm == null || gm.Player == null) { current.outcome = "NO PLAYER"; logs.Add(current); yield break; }

            var bot = PlaytestBot.Attach(gm.Player, plan.skill, gm.difficulties);
            int superUses = 0;
            if (gm.Player.Super != null) gm.Player.Super.Activated += () => superUses++;
            var outTimes = new Dictionary<Racer, float>();
            var recoverTime = new Dictionary<Racer, float>();
            var stuckLogged = new HashSet<Racer>();

            float frames = 0, fpsSum = 0, minFps = 999f;
            float lastElapsed = -1f, stalledFor = 0f;
            while (true)
            {
                if (gm == null) break;
                if (gm.State == GameState.Racing && !gm.Paused) Time.timeScale = timeScale;
                float dt = Time.unscaledDeltaTime;
                if (dt > 0f)
                {
                    float fps = 1f / dt;
                    frames++; fpsSum += fps;
                    if (frames > 30) minFps = Mathf.Min(minFps, fps);
                }

                foreach (var r in gm.Racers)
                {
                    if (r == null || r == gm.Player) continue;
                    if (r.IsEliminated && !outTimes.ContainsKey(r)) outTimes[r] = gm.ElapsedTime;
                    var ai = r.GetComponent<AIController>();
                    if (ai == null || r.IsEliminated) continue;
                    if (ai.State == AIState.Recover) recoverTime[r] = (recoverTime.TryGetValue(r, out var t) ? t : 0f) + Time.deltaTime;
                    else recoverTime[r] = 0f;
                    if (recoverTime[r] > 8f && stuckLogged.Add(r)) current.stuck.Add(r.name + " stuck in Recover at " + gm.ElapsedTime.ToString("0") + "s");
                }

                if (gm.State == GameState.GameOver && gm.LastSummary != null) break;
                if (gm.ElapsedTime > MaxRunSeconds) { current.outcome = "TIMEOUT (survived " + MaxRunSeconds + "s)"; break; }
                // Softlock = the run clock stops while racing (measured in real time, so editor throttling or a
                // slow frame never counts; only a genuinely stuck run does).
                if (gm.State == GameState.Racing && !gm.Paused && Mathf.Approximately(gm.ElapsedTime, lastElapsed) && Time.unscaledDeltaTime < 0.5f) stalledFor += Time.unscaledDeltaTime;
                else stalledFor = 0f;
                lastElapsed = gm.ElapsedTime;
                if (stalledFor > 20f) { current.outcome = "SOFTLOCK (clock stopped, state " + gm.State + ")"; break; }
                yield return null;
            }
            Time.timeScale = 1f;

            if (gm != null)
            {
                var s = gm.LastSummary;
                current.time = Mathf.Round((s != null ? s.time : gm.ElapsedTime) * 10f) / 10f;
                current.score = s != null ? s.score : gm.Score.Score;
                current.place = s != null ? s.place : 0;
                current.racers = s != null ? s.racers : gm.Racers.Count;
                current.levelCleared = s != null && s.levelCleared;
                current.coins = s != null ? s.coinsEarned : 0;
                current.gears = s != null ? s.gearsEarned : 0;
                current.orbs = s != null ? s.orbs : 0;
                if (current.outcome == null)
                    current.outcome = s == null ? "?" : s.levelCleared ? "LEVEL CLEAR" : (!Session.IsObstacle && s.place == 1) ? "WON" : "OUT";
                current.cause = gm.Player != null && gm.Player.IsEliminated ? (gm.Player.EliminationReason ?? "fell off") : "-";
                if (gm.GraceUsed) current.cause += " (rescued once)";
                foreach (var kv in outTimes.OrderBy(k => k.Value))
                    current.aiOuts.Add(kv.Key.name + "@" + kv.Value.ToString("0") + "s");
            }
            current.superUses = superUses;
            current.avgFps = frames > 0 ? Mathf.Round(fpsSum / frames) : 0;
            current.minFps = minFps < 999f ? Mathf.Round(minFps) : 0;
            current.errors = errorCount;
            current.firstError = firstError;
            logs.Add(current);
            if (bot) Destroy(bot);
        }

        bool restored;

        /// <summary>Editor safety net: called before a script reload or when leaving Play mode mid-batch.</summary>
        public static void AbortAndRestore()
        {
            if (Instance == null) return;
            Time.timeScale = 1f;
            Instance.RestoreSave();
        }

        void OnDestroy()
        {
            if (!restored && plans.Count > 0) RestoreSave();
        }

        void RestoreSave()
        {
            if (restored) return;
            restored = true;
            try
            {
                if (saveBackup != null) File.WriteAllText(SaveSystem.FilePath, saveBackup);
                else if (File.Exists(SaveSystem.FilePath)) File.Delete(SaveSystem.FilePath);
                SaveSystem.ReloadFromDisk();
            }
            catch (Exception e) { Debug.LogError("Playtest: could not restore save! Backup text kept in Temp/Playtest/save_backup.json. " + e.Message); }
            if (saveBackup != null) File.WriteAllText(Path.Combine(OutDir, "save_backup.json"), saveBackup);
        }

        // ---------------- Report ----------------

        void WriteReport(bool final)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Treadmill Mayhem - bot playtest report");
            sb.AppendLine();
            sb.AppendLine((final ? "Finished" : "In progress") + ": " + logs.Count + " / " + plans.Count + " runs  •  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "  •  speed x" + timeScale);
            sb.AppendLine();

            sb.AppendLine("## Survival by skill band and mode (median seconds, runs)");
            sb.AppendLine("| Band | Race | Free Play | Levels (cleared/played) |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var band in new[] { "first-timer", "casual", "regular", "expert" })
            {
                var race = logs.Where(l => l.skillBand == band && l.mode == "Race").ToList();
                var free = logs.Where(l => l.skillBand == band && l.mode == "ObstacleFree").ToList();
                var lv = logs.Where(l => l.skillBand == band && l.mode == "ObstacleLevels").ToList();
                sb.AppendLine("| " + band + " | " + Med(race) + " | " + Med(free) + " | " + lv.Count(l => l.levelCleared) + "/" + lv.Count + " |");
            }
            sb.AppendLine();

            var races = logs.Where(l => l.mode == "Race").ToList();
            if (races.Count > 0)
            {
                sb.AppendLine("## Race results by AI difficulty");
                sb.AppendLine("| AI | Runs | Wins | Median survival | Avg coins |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (var g in races.GroupBy(r => r.difficulty).OrderBy(g => g.Key))
                    sb.AppendLine("| " + g.Key + " | " + g.Count() + " | " + g.Count(r => r.outcome == "WON") + " | " + Med(g.ToList()) + " | " + Mathf.RoundToInt((float)g.Average(r => r.coins)) + " |");
                sb.AppendLine();

                // Which NPCs drop out first (balance check).
                var firstOut = new Dictionary<string, List<float>>();
                foreach (var r in races)
                    foreach (var o in r.aiOuts)
                    {
                        var parts = o.Split('@');
                        if (parts.Length != 2 || !float.TryParse(parts[1].TrimEnd('s'), out var t)) continue;
                        if (!firstOut.ContainsKey(parts[0])) firstOut[parts[0]] = new List<float>();
                        firstOut[parts[0]].Add(t);
                    }
                if (firstOut.Count > 0)
                {
                    sb.AppendLine("## NPC elimination times (median s, count) - lower = weaker racer");
                    foreach (var kv in firstOut.OrderBy(k => Median(k.Value)))
                        sb.AppendLine("- " + kv.Key + ": " + Median(kv.Value).ToString("0") + "s (" + kv.Value.Count + " outs)");
                    sb.AppendLine();
                }
            }

            if (logs.Any(l => !string.IsNullOrEmpty(l.character)))
            {
                sb.AppendLine("## Playing as each character - Race only (median survival, runs)");
                foreach (var g in logs.Where(l => l.mode == "Race").GroupBy(l => l.character).OrderByDescending(g => Median(g.Select(r => r.time).ToList())))
                    sb.AppendLine("- " + g.Key + ": " + Med(g.ToList()) + "  •  avg skill " + g.Average(r => r.skill).ToString("0.00"));
                sb.AppendLine();
            }

            int superRuns = logs.Count(l => l.superUses > 0);
            sb.AppendLine("## Pacing");
            sb.AppendLine("- Runs that used Super Speed at least once: " + superRuns + " / " + logs.Count);
            sb.AppendLine("- Causes of player elimination: " + string.Join(", ", logs.GroupBy(l => l.cause).Select(g => g.Key + " x" + g.Count())));
            sb.AppendLine("- Outcomes: " + string.Join(", ", logs.GroupBy(l => l.outcome).Select(g => g.Key + " x" + g.Count())));
            sb.AppendLine("- FPS (unscaled, editor): avg " + (logs.Count > 0 ? Mathf.RoundToInt(logs.Average(l => l.avgFps)) : 0) + ", worst min " + (logs.Count > 0 ? logs.Min(l => l.minFps) : 0));
            sb.AppendLine();

            var problems = logs.Where(l => l.errors > 0 || l.stuck.Count > 0 || (l.outcome != null && (l.outcome.StartsWith("SOFTLOCK") || l.outcome == "NO PLAYER"))).ToList();
            sb.AppendLine("## Problems (" + problems.Count + " runs)");
            foreach (var p in problems)
            {
                sb.Append("- Run " + p.run + " (" + p.mode + ", skill " + p.skill + "): ");
                if (p.errors > 0) sb.Append(p.errors + " errors, first: `" + p.firstError + "` ");
                if (p.stuck.Count > 0) sb.Append(string.Join("; ", p.stuck) + " ");
                if (p.outcome != null && (p.outcome.StartsWith("SOFTLOCK") || p.outcome == "NO PLAYER")) sb.Append(p.outcome);
                sb.AppendLine();
            }
            sb.AppendLine();

            sb.AppendLine("## All runs");
            sb.AppendLine("| # | Char | Mode | Skill | AI/Lvl | Time | Score | Outcome | Cause | Place | Coins | Gears | Super | FPS min |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var l in logs)
                sb.AppendLine("| " + l.run + " | " + l.character + " | " + l.mode + " | " + l.skill + " | " + (l.mode == "ObstacleLevels" ? "L" + l.level : l.difficulty) + " | " + l.time + " | " + l.score + " | " + l.outcome + " | " + l.cause + " | " + (l.mode == "Race" ? l.place + "/" + l.racers : "-") + " | " + l.coins + " | " + l.gears + " | " + l.superUses + " | " + l.minFps + " |");

            File.WriteAllText(Path.Combine(OutDir, "report.md"), sb.ToString());
        }

        static string Med(List<RunLog> runs) => runs.Count == 0 ? "-" : Median(runs.Select(r => r.time).ToList()).ToString("0") + "s (" + runs.Count + ")";

        static float Median(List<float> v)
        {
            if (v.Count == 0) return 0f;
            var s = v.OrderBy(x => x).ToList();
            return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) * 0.5f;
        }
    }
}
