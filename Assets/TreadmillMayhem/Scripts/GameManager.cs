using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace TreadmillMayhem
{
    public enum GameState { Menu, Intro, Countdown, Racing, GameOver }

    /// <summary>
    /// Owns a run in any mode: menu → intro fly-out → countdown → racing → results → retry / next level / menu.
    /// Race mode: survive against AI racers, earn Tuna Coins. Obstacle mode: dodge waves solo, earn Gears.
    /// Balance lives in <see cref="GameConfig"/>; AI smarts in the five <see cref="AIDifficultyProfile"/>s.
    /// </summary>
    [RequireComponent(typeof(ScoreManager))]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        /// <summary>Fired for big moments (level clear, new high score) so decor like confetti cannons can react.</summary>
        public static event Action Celebrate;
        static bool skipMenuOnLoad;

        public GameConfig config;
        [Tooltip("Easy, Normal, Hard, Expert, Insane.")]
        public AIDifficultyProfile[] difficulties = new AIDifficultyProfile[5];

        [Header("Lineup")]
        public GameObject playerPrefab;
        [Tooltip("Every opponent that can show up; each match draws a random lineup from this pool.")]
        public GameObject[] opponentPool;
        [Tooltip("Start grid. Slot 0 is the player.")]
        public Vector3[] spawnPoints;
        public int opponentCount = 5;
        [Tooltip("Testing: force these opponents (by display name) instead of a random draw.")]
        public string[] forceOpponents;

        [Header("Testing")]
        public bool autoStart;

        public GameState State { get; private set; }
        public bool IsRacing => State == GameState.Racing;
        public float ElapsedTime => Score ? Score.SurvivalTime : 0f;
        public ScoreManager Score { get; private set; }
        public int Coins { get; private set; }
        public int RunGears { get; private set; }
        public int LivesLeft { get; private set; }

        /// <summary>Beginner safety net: in a player's first few runs, one fall in the opening seconds is forgiven.</summary>
        public const int GraceRuns = 3;
        public const float GraceSeconds = 8f;
        public bool GraceUsed { get; private set; }
        public bool GraceActive => !GraceUsed && ElapsedTime < GraceSeconds && SaveSystem.Data.runsPlayed < GraceRuns;
        public int SpeedLevel => 1 + Mathf.FloorToInt(ElapsedTime / Mathf.Max(1f, Cfg.secondsPerSpeedLevel));
        public List<Racer> Racers { get; } = new List<Racer>();
        public Racer Player { get; private set; }
        public int PlayerPlace { get; private set; }
        public int DifficultyIndex { get; private set; }
        public AIDifficultyProfile Difficulty => difficulties != null && DifficultyIndex < difficulties.Length ? difficulties[DifficultyIndex] : null;
        public int AliveCount { get { int n = 0; foreach (var r in Racers) if (!r.IsEliminated) n++; return n; } }
        public LevelDef CurrentLevel { get; private set; }
        public ObstacleDirector Director { get; private set; }
        public RunSummary LastSummary { get; private set; }
        /// <summary>0..1 progress through the current obstacle level (0 for race / free play).</summary>
        public float LevelProgress => Session.Mode == GameMode.ObstacleLevels && CurrentLevel != null ? Mathf.Clamp01(ElapsedTime / CurrentLevel.duration) : 0f;

        GameConfig Cfg => GameConfig.Current;

        Treadmill treadmill;
        GameUI ui;
        CameraRig cameraRig;
        StartButton startButton;
        float nextObstacle, nextOrb, nextCoin, nextPowerUp, nextAchievementCheck;
        int lastLevel = 1;
        bool restarting;
        readonly List<AchievementDef> unlockedThisRun = new List<AchievementDef>();

        void Awake()
        {
            Instance = this;
            if (config != null) GameConfig.Current = config;
            Score = GetComponent<ScoreManager>();
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            SpawnLineup();
        }

        void OnEnable()
        {
            Racer.Eliminated += OnRacerEliminated;
            Racer.Bumped += OnBumped;
            Racer.CampWarned += OnCampWarned;
            Achievements.Unlocked += OnAchievement;
        }

        void OnDisable()
        {
            Racer.Eliminated -= OnRacerEliminated;
            Racer.Bumped -= OnBumped;
            Racer.CampWarned -= OnCampWarned;
            Achievements.Unlocked -= OnAchievement;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            SaveSystem.Save();
            if (CanPause && !PlaytestRunner.Running) SetPaused(true);
        }
        void OnApplicationQuit() => SaveSystem.Save();

        /// <summary>Spawns the player and (in Race mode) a random set of opponents on the start grid.</summary>
        void SpawnLineup()
        {
            if (playerPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;
            var parent = GameObject.Find("Racers");
            var root = parent ? parent.transform : null;
            // In Obstacle Mode the tuna starts mid-belt with room to dodge both ways.
            var playerSpawn = Session.IsObstacle ? spawnPoints[0] + new Vector3(0f, 0f, -1f) : spawnPoints[0];
            // The chosen character plays as you; if it isn't the tuna, the tuna joins the opponent pool instead.
            var all = Characters.AllPrefabs(this);
            var chosen = all.Find(p => p.name == Characters.Selected);
            if (chosen == null) chosen = playerPrefab;
            var player = SpawnRacer(chosen, playerSpawn, root, true);

            if (!Session.IsObstacle)
            {
                var pool = new List<GameObject>();
                foreach (var p in all) if (p && p != chosen) pool.Add(p);
                var picks = new List<GameObject>();
                if (forceOpponents != null && forceOpponents.Length > 0)
                    foreach (var n in forceOpponents) { var p = pool.Find(x => x.name == n); if (p) picks.Add(p); }
                while (picks.Count < opponentCount && pool.Count > 0)
                {
                    var p = pool[Random.Range(0, pool.Count)];
                    pool.Remove(p);
                    if (!picks.Contains(p)) picks.Add(p);
                }
                for (int i = 0; i < picks.Count && i + 1 < spawnPoints.Length; i++)
                    SpawnRacer(picks[i], spawnPoints[i + 1], root, false);
            }

            var rig = FindAnyObjectByType<CameraRig>();
            if (rig) rig.target = player.transform;
        }

        /// <summary>
        /// Spawns a racer prefab as the player or as an NPC, swapping its controller if the prefab was built for
        /// the other role. It is instantiated under an inactive holder so Awake only runs after the swap.
        /// </summary>
        static GameObject SpawnRacer(GameObject prefab, Vector3 pos, Transform parent, bool asPlayer)
        {
            var holder = new GameObject("SpawnHolder");
            holder.SetActive(false);
            var go = Instantiate(prefab, pos, Quaternion.identity, holder.transform);
            go.name = prefab.name;
            var racer = go.GetComponent<Racer>();
            if (racer != null && racer.isPlayer != asPlayer)
            {
                racer.isPlayer = asPlayer;
                if (asPlayer)
                {
                    var ai = go.GetComponent<AIController>();
                    if (ai) DestroyImmediate(ai);
                    if (!go.GetComponent<SuperSpeedSystem>()) go.AddComponent<SuperSpeedSystem>();
                    if (!go.GetComponent<PlayerController>()) go.AddComponent<PlayerController>();
                }
                else
                {
                    var pc = go.GetComponent<PlayerController>();
                    if (pc) DestroyImmediate(pc);
                    var ss = go.GetComponent<SuperSpeedSystem>();
                    if (ss) DestroyImmediate(ss);
                    if (!go.GetComponent<AIController>()) go.AddComponent<AIController>().laneBias = Random.Range(-0.5f, 0.5f);
                }
            }
            go.transform.SetParent(parent, true);
            Destroy(holder);
            return go;
        }

        void Start()
        {
            treadmill = FindAnyObjectByType<Treadmill>();
            ui = FindAnyObjectByType<GameUI>();
            cameraRig = FindAnyObjectByType<CameraRig>();
            startButton = FindAnyObjectByType<StartButton>();
            if (Session.IsObstacle) ApplyBeltUpgrades();
            Racers.AddRange(FindObjectsByType<Racer>());
            Player = Racers.Find(r => r.isPlayer);
            treadmill.speed = 0f;
            WirePlayerEvents();
            SetDifficulty(SaveSystem.Data.difficulty);
            Achievements.CheckLifetime();

            // Retry (and test runs) skip the menu and go straight to the countdown.
            if (skipMenuOnLoad || autoStart || startButton == null || cameraRig == null)
            {
                skipMenuOnLoad = false;
                StartCoroutine(Countdown());
            }
            else
            {
                State = GameState.Menu;
                treadmill.idleScrollSpeed = 1.2f;
                cameraRig.EnterMenu(startButton);
                ui?.ShowMenu();
                ui?.TryShowOffline();
            }
        }

        /// <summary>Longer / Wider Belt upgrades stretch the whole treadmill (Obstacle Mode only).</summary>
        void ApplyBeltUpgrades()
        {
            float len = Upgrades.BeltLengthMultiplier, wid = Upgrades.BeltWidthMultiplier;
            if (Mathf.Approximately(len, 1f) && Mathf.Approximately(wid, 1f)) return;
            var root = treadmill.transform.parent;
            if (root == null) return;
            root.localScale = new Vector3(root.localScale.x * wid, root.localScale.y, root.localScale.z * len);
            treadmill.beltWidth *= wid;
            treadmill.beltLength *= len;
            Physics.SyncTransforms();
        }

        void WirePlayerEvents()
        {
            if (Player == null) return;
            Score.PointsPopup += (pts, where) => ui?.FloatingPoints(pts, where);
            if (Player.Acceleration)
                Player.Acceleration.Activated += () => ui?.Banner("ACCELERATION!", new Color(1f, 0.85f, 0.25f));
            if (Player.Super)
            {
                Player.Super.ChargeReady += () =>
                {
                    ui?.Banner("SUPER SPEED READY!", new Color(0.7f, 0.5f, 1f), TouchControls.Active ? "Tap SUPER" : "Press SPACE");
                    AudioFX.Play(Sfx.SuperReady, 0.8f);
                };
                Player.Super.Activated += () =>
                {
                    ui?.Banner("SUPER SPEED!", new Color(0.75f, 0.55f, 1f));
                    Haptics.Buzz();
                    AudioFX.Play(Sfx.SuperGo, 0.9f);
                    CameraRig.Shake(0.3f);
                };
            }
        }

        public void SetDifficulty(int index)
        {
            if (State == GameState.Racing || State == GameState.GameOver) return;
            DifficultyIndex = Mathf.Clamp(index, 0, Mathf.Max(0, (difficulties?.Length ?? 1) - 1));
            SaveSystem.Data.difficulty = DifficultyIndex;
            SaveSystem.Save();
            foreach (var ai in FindObjectsByType<AIController>()) ai.SetProfile(Difficulty);
            ui?.OnDifficultyChanged();
        }

        /// <summary>Switching mode respawns the lineup, so it reloads the scene straight back to the menu.</summary>
        public void SetMode(GameMode mode, int level = -1)
        {
            if (State != GameState.Menu) return;
            if (level > 0) Session.Level = Mathf.Clamp(level, 1, SaveSystem.Data.obstacleLevel);
            if (Session.Mode == mode) { ui?.OnModeChanged(); return; }
            Session.Mode = mode;
            Reload(false);
        }

        // ---------- Pause ----------

        public bool Paused { get; private set; }
        public bool CanPause => State == GameState.Racing || State == GameState.Countdown;

        public void SetPaused(bool paused)
        {
            if (paused && !CanPause) return;
            if (Paused == paused) return;
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (paused) SaveSystem.Save();
            ui?.OnPauseChanged(paused);
        }

        public void TogglePause() => SetPaused(!Paused);

        void OnApplicationFocus(bool focused)
        {
            // Phones: an incoming call or app switch pauses the run instead of losing it.
            if (!focused && Application.isMobilePlatform && CanPause) SetPaused(true);
        }

        void Update()
        {
            var keys = Keyboard.current;
            if (keys != null && (keys.escapeKey.wasPressedThisFrame || keys.pKey.wasPressedThisFrame) && (CanPause || Paused)) TogglePause();
            else if (keys != null && keys.escapeKey.wasPressedThisFrame && State == GameState.Menu && ui != null) ui.HandleBack();
            if (Paused) { ui?.Refresh(this); return; }

            switch (State)
            {
                case GameState.Menu:
                    TickMenu();
                    break;
                case GameState.Racing:
                    TickRace();
                    break;
                case GameState.GameOver:
                    treadmill.speed = Mathf.MoveTowards(treadmill.speed, 0f, Time.deltaTime * 1.5f);
                    var kb = Keyboard.current;
                    if (kb != null && kb.rKey.wasPressedThisFrame && !(ui != null && ui.IsTyping)) Retry();
                    break;
            }
            ui?.Refresh(this);
        }

        void TickMenu()
        {
            var kb = Keyboard.current;
            if (ui != null && ui.MenuSubscreenOpen) { startButton.SetHover(false); return; }
            if (kb != null && !Session.IsObstacle)
            {
                if (kb.digit1Key.wasPressedThisFrame) SetDifficulty(0);
                if (kb.digit2Key.wasPressedThisFrame) SetDifficulty(1);
                if (kb.digit3Key.wasPressedThisFrame) SetDifficulty(2);
                if (kb.digit4Key.wasPressedThisFrame) SetDifficulty(3);
                if (kb.digit5Key.wasPressedThisFrame) SetDifficulty(4);
            }

            var cam = cameraRig.GetComponent<Camera>();
            Vector2? pointer = null;
            bool clicked = false;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointer = Touchscreen.current.primaryTouch.position.ReadValue();
                clicked = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
            else if (Mouse.current != null)
            {
                pointer = Mouse.current.position.ReadValue();
                clicked = Mouse.current.leftButton.wasPressedThisFrame;
            }

            bool over = pointer.HasValue && startButton.IsHit(cam.ScreenPointToRay(pointer.Value));
            startButton.SetHover(over);

            bool keyPress = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
            if ((clicked && over) || keyPress) PressStart();
        }

        /// <summary>PLAY: press the console button, fly the camera out to the tuna, then count down.</summary>
        public void PressStart()
        {
            if (State != GameState.Menu) return;
            State = GameState.Intro;
            ui?.HidePanels();
            startButton.Press(() => cameraRig.PlayIntro(() => StartCoroutine(Countdown())));
        }

        IEnumerator Countdown()
        {
            State = GameState.Countdown;
            ui?.HidePanels();
            Score.Begin(Player);
            Coins = 0;
            RunGears = 0;
            GraceUsed = false;
            unlockedThisRun.Clear();
            if (Session.IsObstacle)
            {
                CurrentLevel = Session.Mode == GameMode.ObstacleLevels ? Levels.Get(Session.Level) : Levels.FreePlay;
                LivesLeft = Upgrades.ExtraLives;
                Director = gameObject.AddComponent<ObstacleDirector>();
                Director.Begin(CurrentLevel, treadmill, Player);
                Director.DodgedOne += pos => Score.AddBonus(25, pos);
                ui?.Banner(Session.Mode == GameMode.ObstacleLevels ? "LEVEL " + CurrentLevel.number : "FREE PLAY", UIKit.GearColor,
                    Session.Mode == GameMode.ObstacleLevels ? CurrentLevel.name + "  -  survive " + CurrentLevel.duration + "s" : "Dodge as long as you can");
                yield return new WaitForSeconds(1.2f);
            }
            for (int i = 3; i > 0; i--)
            {
                ui?.Banner(i.ToString(), Color.white);
                AudioFX.Play(Sfx.Beep, 0.7f);
                yield return new WaitForSeconds(0.8f);
            }
            ui?.Banner("GO!", new Color(1f, 0.85f, 0.2f));
            AudioFX.Play(Sfx.Go, 0.8f);
            treadmill.idleScrollSpeed = 0f;
            State = GameState.Racing;
            if (TutorialHints.ShouldRun) TutorialHints.Begin(this, ui);
            nextObstacle = 3f;
            nextOrb = 1.5f;
            nextCoin = 1f;
            nextPowerUp = 6f;
        }

        void TickRace()
        {
            Score.Tick(Time.deltaTime);
            float t = ElapsedTime;

            if (Session.IsObstacle) TickObstacleMode(t);
            else TickRaceMode(t);

            if (Time.time >= nextAchievementCheck)
            {
                nextAchievementCheck = Time.time + 0.5f;
                CheckRunAchievements();
            }
        }

        void TickRaceMode(float t)
        {
            treadmill.speed = Cfg.BeltSpeedAt(t);
            if (SpeedLevel != lastLevel)
            {
                lastLevel = SpeedLevel;
                ui?.Banner("SPEED UP!", new Color(1f, 0.35f, 0.3f), "Level " + SpeedLevel);
                CameraRig.Shake(0.15f);
                AudioFX.Play(Sfx.Whoosh, 0.7f);
            }

            float ramp = Mathf.Clamp01(t / Cfg.hazardRampSeconds);
            if (Cfg.obstaclesEnabled && t >= nextObstacle)
            {
                nextObstacle = t + Mathf.Lerp(Cfg.obstacleInterval.x, Cfg.obstacleInterval.y, ramp) * Random.Range(0.8f, 1.2f);
                SpawnObstacleSafely();
            }
            if (t >= nextOrb)
            {
                nextOrb = t + Random.Range(Cfg.orbInterval.x, Cfg.orbInterval.y);
                if (FindClearSpot(0.9f, 1f, out var p)) StaminaOrb.Spawn(p + Vector3.up * 0.6f);
            }
            if (t >= nextCoin)
            {
                nextCoin = t + Random.Range(Cfg.coinInterval.x, Cfg.coinInterval.y);
                SpawnPickupRow(false);
            }
            if (t >= nextPowerUp)
            {
                nextPowerUp = t + Random.Range(Cfg.powerUpInterval.x, Cfg.powerUpInterval.y);
                if (FindClearSpot(1f, Random.Range(0.6f, 1f), out var p)) PowerUp.Spawn((PowerUpType)Random.Range(0, 2), p + Vector3.up * 0.7f);
            }
        }

        void TickObstacleMode(float t)
        {
            treadmill.speed = Mathf.Lerp(CurrentLevel.beltStart, CurrentLevel.beltEnd, Director.Ramp(t)) + Director.Overtime * 2f;
            Director.Tick(Time.deltaTime);

            if (t >= nextOrb)
            {
                nextOrb = t + Random.Range(5f, 8f);
                if (FindClearSpot(0.9f, 1f, out var p)) StaminaOrb.Spawn(p + Vector3.up * 0.6f);
            }
            if (t >= nextCoin)
            {
                nextCoin = t + Random.Range(1.8f, 3f);
                SpawnPickupRow(true);
            }
            if (t >= nextPowerUp)
            {
                nextPowerUp = t + Random.Range(14f, 20f);
                if (FindClearSpot(1f, 1f, out var p)) PowerUp.Spawn(PowerUpType.Shield, p + Vector3.up * 0.7f);
            }

            if (Session.Mode == GameMode.ObstacleLevels && t >= CurrentLevel.duration) StartCoroutine(LevelCleared());
        }

        /// <summary>Picks a front-of-belt spot not overlapping racers, obstacles or pickups.</summary>
        bool FindClearSpot(float clearance, float frontBias, out Vector3 spot)
        {
            for (int i = 0; i < 8; i++)
            {
                spot = treadmill.RandomPointOnBelt(frontBias, 0.7f);
                var hits = Physics.OverlapSphere(spot + Vector3.up * 0.6f, clearance, ~0, QueryTriggerInteraction.Collide);
                bool blocked = false;
                foreach (var h in hits)
                    if (h.GetComponentInParent<Racer>() || h.GetComponentInParent<Obstacle>() || h.GetComponentInParent<StaminaOrb>() ||
                        h.GetComponentInParent<PowerUp>() || h.GetComponentInParent<Coin>() || h.GetComponentInParent<Gear>())
                    { blocked = true; break; }
                if (!blocked) return true;
            }
            spot = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Fair obstacle drops: never straight into the player's lane when they're near the front with little
        /// time to react, and never so many at once that the belt is walled off.
        /// </summary>
        void SpawnObstacleSafely()
        {
            int onFrontHalf = 0;
            foreach (var o in Obstacle.Active)
                if (o && o.transform.position.z > treadmill.transform.position.z) onFrontHalf++;
            if (onFrontHalf >= 3) return;

            for (int attempt = 0; attempt < 8; attempt++)
            {
                if (!FindClearSpot(1.2f, 1f, out var spot)) return;
                if (Player != null && !Player.IsEliminated)
                {
                    var pp = Player.Body.position;
                    float reactDist = 2.5f + treadmill.speed * 0.8f;
                    if (Mathf.Abs(spot.x - pp.x) < 1.4f && spot.z - pp.z < reactDist) continue;
                }
                Obstacle.Spawn((ObstacleKind)Random.Range(0, 3), spot + Vector3.up * 2f);
                return;
            }
        }

        /// <summary>Rows (or diagonal sweeps) of coins in Race mode, gears in Obstacle mode.</summary>
        void SpawnPickupRow(bool gears)
        {
            int count = Random.Range(3, 6);
            var start = treadmill.RandomPointOnBelt(1f, 0.8f) + Vector3.up * 0.55f;
            float drift = Random.value < 0.4f ? Random.Range(-0.45f, 0.45f) : 0f;
            for (int i = 0; i < count; i++)
            {
                var p = start + new Vector3(drift * i, 0f, 0.9f * i);
                p.x = Mathf.Clamp(p.x, treadmill.transform.position.x - treadmill.HalfWidth + 0.5f, treadmill.transform.position.x + treadmill.HalfWidth - 0.5f);
                if (p.z > treadmill.FrontZ - 0.4f) break;
                if (gears) Gear.Spawn(p);
                else Coin.Spawn(p);
            }
        }

        public void AddCoin()
        {
            if (State != GameState.Racing) return;
            Coins++;
            ui?.PulseCoins();
        }

        public void AddGear(Vector3 where)
        {
            if (State != GameState.Racing) return;
            RunGears++;
            ui?.PulseCoins();
        }

        public void OnPlayerOrb(Vector3 where)
        {
            if (State != GameState.Racing) return;
            Score.AddOrb(where);
        }

        void OnCampWarned(Racer r, int count)
        {
            if (State != GameState.Racing || count >= Cfg.campMaxWarnings) return;
            if (r.isPlayer)
            {
                Haptics.Buzz();
                ui?.Banner("NO CAMPING!", new Color(1f, 0.85f, 0.15f), "Warning " + count + " of " + Cfg.campMaxWarnings + " - keep moving");
                AudioFX.Play(Sfx.Beep, 1f, 0.6f);
                CameraRig.Shake(0.15f);
            }
            else ui?.SmallNotice(r.DisplayName.ToUpper() + " WARNED FOR CAMPING", r.Color);
        }

        void OnRacerEliminated(Racer r)
        {
            if (State != GameState.Racing) return;
            if (r.isPlayer)
            {
                if (GraceActive && r.EliminationReason != "CAMPING")
                {
                    GraceUsed = true;
                    StartCoroutine(GraceRescueRoutine());
                    return;
                }
                // Extra Life (Obstacle Mode upgrade): rescued back onto the belt instead of losing.
                if (Session.IsObstacle && LivesLeft > 0 && r.EliminationReason != "CAMPING")
                {
                    LivesLeft--;
                    StartCoroutine(ReviveRoutine());
                    return;
                }
                PlayerPlace = AliveCount + 1;
                StartCoroutine(PlayerOut());
                return;
            }
            string why = r.EliminationReason == "CAMPING" ? " DISQUALIFIED FOR CAMPING!  " : " IS OUT!  ";
            ui?.SmallNotice(r.DisplayName.ToUpper() + why + AliveCount + " left", r.Color);
            CameraRig.Shake(0.2f);
            if (Player != null && !Player.IsEliminated && AliveCount == 1)
            {
                ui?.Banner("LAST ONE ROLLING!", new Color(1f, 0.85f, 0.2f), "Survive as long as you can");
                Achievements.Unlock("last_one");
                Celebrate?.Invoke();
            }
        }

        IEnumerator GraceRescueRoutine()
        {
            ui?.Banner("SAVED!", new Color(0.4f, 0.9f, 1f), "Free rescue for new players - stay on the belt!");
            AudioFX.Play(Sfx.SuperReady, 0.9f);
            yield return new WaitForSeconds(0.6f);
            var spot = treadmill.transform.position + new Vector3(0f, 1.2f, treadmill.beltLength * 0.1f);
            Player.Revive(spot);
        }

        IEnumerator ReviveRoutine()
        {
            ui?.Banner("EXTRA LIFE!", new Color(1f, 0.45f, 0.6f), LivesLeft + " left");
            AudioFX.Play(Sfx.SuperReady, 0.9f);
            yield return new WaitForSeconds(0.7f);
            var spot = treadmill.transform.position + new Vector3(0f, 1.2f, treadmill.beltLength * 0.1f);
            Player.Revive(spot);
            CameraRig.Shake(0.2f);
        }

        IEnumerator LevelCleared()
        {
            if (State != GameState.Racing) yield break;
            Haptics.Buzz();
            State = GameState.GameOver;
            ui?.Banner("LEVEL CLEAR!", new Color(0.4f, 1f, 0.55f), CurrentLevel.name);
            AudioFX.Play(Sfx.SuperGo, 1f, 1.1f);
            AudioFX.Play(Sfx.Cheer, 0.7f);
            Celebrate?.Invoke();
            if (Player) Player.ShieldTimer = 99f;   // safe lap of honour while the belt winds down
            yield return new WaitForSecondsRealtime(2f);
            FinishRun(true);
        }

        IEnumerator PlayerOut()
        {
            State = GameState.GameOver;
            CameraRig.Shake(0.5f);
            cameraRig?.FollowFall();
            if (Player.EliminationReason == "CAMPING") ui?.Banner("DISQUALIFIED!", new Color(1f, 0.3f, 0.3f), "No camping allowed");
            else ui?.Banner(Session.IsObstacle ? "WIPED OUT!" : "TUNA DOWN!", new Color(1f, 0.3f, 0.3f));
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(1.4f);
            Time.timeScale = 1f;
            yield return new WaitForSecondsRealtime(0.4f);
            if (restarting) yield break;
            FinishRun(false);
        }

        /// <summary>Pays out the run, records bests and achievements, and shows the results screen.</summary>
        void FinishRun(bool cleared)
        {
            var d = SaveSystem.Data;
            var s = new RunSummary
            {
                mode = Session.Mode,
                level = Session.Level,
                levelCleared = cleared,
                disqualified = Player && Player.EliminationReason == "CAMPING",
                time = Score.SurvivalTime,
                score = Score.Score,
                orbs = Score.OrbsCollected,
                accelTime = Score.AccelerationTime,
                place = cleared ? 1 : PlayerPlace,
                racers = Racers.Count,
                coinsBefore = d.totalCoins,
                gearsBefore = d.gears,
            };

            if (Session.IsObstacle)
            {
                int gears = RunGears + Mathf.FloorToInt(s.time / 10f) + (cleared ? CurrentLevel.gearReward : 0);
                s.gearsEarned = gears;
                SaveSystem.AddGears(gears);
                if (Session.Mode == GameMode.ObstacleFree)
                {
                    s.newHighScore = s.score > d.obstacleHighScore;
                    d.obstacleBestTime = Mathf.Max(d.obstacleBestTime, s.time);
                    d.obstacleHighScore = Mathf.Max(d.obstacleHighScore, s.score);
                }
                if (cleared && Session.Level >= d.obstacleLevel) d.obstacleLevel = Mathf.Min(Levels.Count, Session.Level + 1);
                if (cleared && Session.Level >= 5) Achievements.Unlock("level_5");
                if (cleared && Session.Level >= Levels.Count) Achievements.Unlock("level_12");
                s.bestScore = d.obstacleHighScore;
                d.runsPlayed++;
                SaveSystem.Save();
            }
            else
            {
                // Tuna Coins: 20 + score/20 + coins picked up, times the Coin Multiplier upgrade.
                int baseCoins = 20 + Mathf.FloorToInt(s.score / 20f) + Coins;
                s.coinsEarned = Mathf.RoundToInt(baseCoins * Upgrades.CoinMultiplier);
                s.newHighScore = s.score > d.highScore;
                SaveSystem.AddCoins(s.coinsEarned);
                s.record = new RunRecord
                {
                    name = d.lastName,
                    score = s.score,
                    time = s.time,
                    orbs = s.orbs,
                    accelerationTime = s.accelTime,
                    coins = s.coinsEarned,
                    place = s.place,
                    difficulty = Difficulty ? Difficulty.displayName : "?",
                    date = DateTime.Now.ToString("yyyy-MM-dd"),
                    mode = "RACE",
                };
                SaveSystem.AddRun(s.record);
                s.bestScore = d.highScore;
            }

            Achievements.CheckLifetime();
            s.achievements.AddRange(unlockedThisRun);
            if (s.newHighScore) Celebrate?.Invoke();
            LastSummary = s;
            ui?.ShowResults(s);
            Ads.MaybeShowInterstitial(d.runsPlayed);
        }

        /// <summary>Unlocks achievements the moment they're earned, mid-run.</summary>
        void CheckRunAchievements()
        {
            if (Session.IsObstacle)
            {
                if (Session.Mode == GameMode.ObstacleFree && ElapsedTime >= 30f) Achievements.Unlock("dodger");
                return;
            }
            if (ElapsedTime >= 10f) Achievements.Unlock("first_steps");
            if (ElapsedTime >= 30f) Achievements.Unlock("tuna_runner");
            if (ElapsedTime >= 60f) Achievements.Unlock("treadmill_master");
            if (Score.AccelerationTime >= 15f) Achievements.Unlock("speed_demon");
            if (Score.OrbsCollected >= 10) Achievements.Unlock("boost_collector");
            if (Score.Score >= 10000) Achievements.Unlock("unstoppable");
        }

        void OnAchievement(AchievementDef a)
        {
            if (State == GameState.Racing || State == GameState.GameOver) unlockedThisRun.Add(a);
        }

        void OnBumped(Racer a, Racer b, float impact)
        {
            if (a.isPlayer || b.isPlayer) CameraRig.Shake(Mathf.Clamp(impact * 0.04f, 0.05f, 0.3f));
        }

        public void OnPowerUpCollected(Racer r, PowerUpType type)
        {
            if (r.isPlayer) ui?.Banner(PowerUp.LabelFor(type), PowerUp.ColorFor(type));
        }

        /// <summary>Clean new match: reload the scene so no physics state or spawned objects survive.</summary>
        public void Retry() => Reload(true);

        public void NextLevel()
        {
            Session.Level = Mathf.Min(Levels.Count, Session.Level + 1);
            Reload(true);
        }

        public void MainMenu() => Reload(false);

        void Reload(bool skipMenu)
        {
            if (restarting) return;
            restarting = true;
            skipMenuOnLoad = skipMenu;
            Paused = false;
            AudioListener.pause = false;
            Time.timeScale = 1f;
            StopAllCoroutines();
            SaveSystem.Save();
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.path);
        }
    }
}
