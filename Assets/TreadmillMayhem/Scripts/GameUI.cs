using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>
    /// All UI, built in code so the scene needs no hand-wired references. Display only: it reads game state and
    /// calls GameManager for actions. Menu screens are registered by id (<see cref="RegisterScreen"/>) and
    /// bigger features (shop, results, achievements, offline rewards) live in their own screen classes.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public const string MainScreen = "main", LeaderboardScreen = "leaderboard", SettingsScreen = "settings";

        RectTransform root, canvasRect;
        CanvasScaler scaler;
        TouchControls touch;
        float lastAspect;

        // Screens.
        readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>();
        string currentScreen;
        GameObject menuRoot, hudPanel;
        ShopScreen shop;
        ResultsScreen results;
        AchievementsScreen achievementsScreen;
        OfflinePopup offline;
        PauseScreen pause;
        CharacterScreen characters;
        DailyRewardPopup daily;
        Button pauseButton;

        // HUD.
        Text scoreValue, timeValue, racersLabel, racersValue, staminaValue, superValue, accelLine, speedText, modeText, currencyText, campText, controlsHint;
        Image staminaFill, superFill, levelFill;
        GameObject levelBar;
        RectTransform stickBase, stickKnob;
        float coinPulse;

        // Banners and popups.
        Text bannerText, bannerSub, noticeText, hintText;
        GameObject hintBox;
        float bannerTime, noticeTime, hintTime = -100f;
        readonly List<(Text text, Vector3 world, float born)> floaters = new List<(Text, Vector3, float)>();

        // Menu.
        Text menuHigh, diffBlurb, coinPill, gearPill, achievementsLabel, levelLabel, levelName, freeBest, upgradeBadge;
        readonly List<Button> diffButtons = new List<Button>();
        readonly Dictionary<GameMode, Button> modeButtons = new Dictionary<GameMode, Button>();
        GameObject difficultyGroup, levelGroup, freeGroup;
        Button upgradesButton;
        LeaderboardTable menuBoard;

        public bool IsTyping => results != null && results.IsTyping;
        public bool MenuSubscreenOpen =>
            (menuRoot != null && menuRoot.activeSelf && currentScreen != MainScreen) ||
            (shop != null && shop.gameObject.activeSelf) ||
            (achievementsScreen != null && achievementsScreen.gameObject.activeSelf) ||
            (characters != null && characters.gameObject.activeSelf) ||
            (daily != null && daily.gameObject.activeSelf) ||
            (offline != null && offline.gameObject.activeSelf);

        void Awake()
        {
            EnsureEventSystem();
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            FitAspect();
            gameObject.AddComponent<GraphicRaycaster>();
            canvasRect = (RectTransform)transform;
            // HUD and menus live inside the device safe area; full-screen overlays bleed to the edges themselves.
            var safe = UIKit.Node(canvasRect, "Safe");
            SafeArea.Add(safe);
            root = (RectTransform)safe.transform;

            BuildHud();
            bannerText = UIKit.Text(root, "", 110, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), TextAnchor.MiddleCenter);
            bannerSub = UIKit.Text(root, "", 42, new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), TextAnchor.MiddleCenter);
            noticeText = UIKit.Text(root, "", 36, new Vector2(0.5f, 1f), new Vector2(0f, -300f), TextAnchor.UpperCenter);
            hintBox = UIKit.Box(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(1000f, 96f), new Color(0.05f, 0.04f, 0.14f, 0.82f));
            hintText = UIKit.Text(hintBox.transform, "", 34, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, false, 960f);
            hintBox.SetActive(false);

            menuRoot = UIKit.Node(root, "Menu");
            UIKit.Stretch((RectTransform)menuRoot.transform);
            RegisterScreen(MainScreen, BuildMainScreen());
            RegisterScreen(LeaderboardScreen, BuildLeaderboardScreen());
            RegisterScreen(SettingsScreen, BuildSettingsScreen());

            results = ResultsScreen.Build(canvasRect);
            results.PlayAgain = () => GameManager.Instance?.Retry();
            results.NextLevel = () => GameManager.Instance?.NextLevel();
            results.MainMenu = () => GameManager.Instance?.MainMenu();
            results.OpenShop = () => OpenShop(Session.IsObstacle ? ShopScreen.Tab.Obstacle : ShopScreen.Tab.Tuna);

            shop = ShopScreen.Build(canvasRect);
            shop.Closed = () => { results.RefreshGoal(); RefreshMenuInfo(); };
            achievementsScreen = AchievementsScreen.Build(canvasRect);
            achievementsScreen.Closed = RefreshMenuInfo;
            offline = OfflinePopup.Build(canvasRect);
            daily = DailyRewardPopup.Build(canvasRect);
            daily.Closed = RefreshMenuInfo;
            offline.Closed = () => { RefreshMenuInfo(); daily.TryShow(); };
            characters = CharacterScreen.Build(canvasRect);
            characters.Closed = () =>
            {
                RefreshMenuInfo();
                // A new pick needs the lineup respawned with that character on the belt.
                var gm = GameManager.Instance;
                if (gm != null && gm.Player != null && gm.Player.name != Characters.Selected) gm.MainMenu();
            };
            pause = PauseScreen.Build(canvasRect);
            pause.Resume = () => GameManager.Instance?.SetPaused(false);
            pause.Restart = () => GameManager.Instance?.Retry();
            pause.Quit = () => GameManager.Instance?.MainMenu();
            AchievementToast.Build(root);

            stickBase = MakeStick(220f, new Color(1f, 1f, 1f, 0.18f));
            stickKnob = MakeStick(100f, new Color(1f, 1f, 1f, 0.5f));
            touch = TouchControls.Build(root);
            SaveSystem.CurrencyChanged += RefreshMenuInfo;
            HidePanels();
        }

        void OnDestroy() => SaveSystem.CurrencyChanged -= RefreshMenuInfo;

        /// <summary>Phones are wider than 16:9 (match height so the HUD keeps its size); tablets are narrower (match width so nothing clips).</summary>
        void FitAspect()
        {
            float aspect = UnityEngine.Screen.height > 0 ? (float)UnityEngine.Screen.width / UnityEngine.Screen.height : 16f / 9f;
            if (Mathf.Abs(aspect - lastAspect) < 0.001f) return;
            lastAspect = aspect;
            scaler.matchWidthOrHeight = aspect >= 16f / 9f ? 1f : 0f;
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }

        // ================= Screen registry =================

        /// <summary>Adds a menu screen. Future features (character select, skins…) call this with their own panel.</summary>
        public void RegisterScreen(string id, GameObject screen)
        {
            screen.transform.SetParent(menuRoot.transform, false);
            screens[id] = screen;
            screen.SetActive(false);
        }

        public void ShowScreen(string id)
        {
            foreach (var kv in screens) kv.Value.SetActive(kv.Key == id);
            currentScreen = id;
            if (id == LeaderboardScreen) menuBoard.Fill(null);
            if (id == MainScreen) RefreshMenuInfo();
        }

        public void ShowMenu()
        {
            menuRoot.SetActive(true);
            results.Hide();
            ShowScreen(MainScreen);
            OnDifficultyChanged();
            OnModeChanged();
        }

        public void HidePanels()
        {
            menuRoot.SetActive(false);
            results.Hide();
        }

        public void OpenShop(ShopScreen.Tab tab) => shop.Show(tab);

        /// <summary>Menu arrival popups, one after another: offline earnings, then the daily reward.</summary>
        public void TryShowOffline()
        {
            if (!offline.TryShow()) daily.TryShow();
        }

        public void ShowResults(RunSummary s)
        {
            menuRoot.SetActive(false);
            bannerText.text = bannerSub.text = "";
            results.Show(s);
        }

        // ================= HUD =================

        void BuildHud()
        {
            hudPanel = UIKit.Box(root, "HUD", new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(1000f, 255f), new Color(UIKit.Panel.r, UIKit.Panel.g, UIKit.Panel.b, 0.82f));
            hudPanel.transform.localScale = Vector3.one * 0.78f;
            var p = hudPanel.transform;

            Label(p, "SCORE", new Vector2(-470f, -22f), TextAnchor.UpperLeft);
            scoreValue = UIKit.Text(p, "", 64, new Vector2(0.5f, 1f), new Vector2(-470f, -40f), TextAnchor.UpperLeft, false);
            Label(p, "SURVIVAL", new Vector2(0f, -30f), TextAnchor.UpperCenter);
            timeValue = UIKit.Text(p, "", 50, new Vector2(0.5f, 1f), new Vector2(0f, -48f), TextAnchor.UpperCenter, false);
            racersLabel = Label(p, "RACERS", new Vector2(470f, -22f), TextAnchor.UpperRight);
            racersValue = UIKit.Text(p, "", 56, new Vector2(0.5f, 1f), new Vector2(470f, -40f), TextAnchor.UpperRight, false);

            Label(p, "STAMINA", new Vector2(-470f, -120f), TextAnchor.UpperLeft).color = Color.white;
            staminaValue = Small(p, new Vector2(470f, -120f), TextAnchor.UpperRight, UIKit.Cyan);
            staminaFill = UIKit.Bar(p, new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(940f, 16f), UIKit.Cyan);

            Label(p, "SUPER SPEED", new Vector2(-470f, -172f), TextAnchor.UpperLeft).color = Color.white;
            superValue = Small(p, new Vector2(470f, -172f), TextAnchor.UpperRight, UIKit.Purple);
            superFill = UIKit.Bar(p, new Vector2(0.5f, 1f), new Vector2(0f, -204f), new Vector2(940f, 16f), UIKit.Purple);

            accelLine = UIKit.Text(p, "", 30, new Vector2(0.5f, 1f), new Vector2(0f, -222f), TextAnchor.UpperCenter, false);

            pauseButton = UIKit.Button(root, "", new Vector2(0f, 1f), new Vector2(80f, -75f), new Vector2(104f, 104f), new Color(1f, 1f, 1f, 0.85f), () => GameManager.Instance?.TogglePause(), 46);
            // Two bars drawn as boxes (the font's "II" merges into one block at this size).
            foreach (var x in new[] { -15f, 15f })
                UIKit.Box(pauseButton.transform, "Bar", new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(16f, 50f), UIKit.Ink).GetComponent<Image>().raycastTarget = false;
            speedText = UIKit.Text(root, "", 32, new Vector2(0f, 1f), new Vector2(160f, -28f), TextAnchor.UpperLeft);
            modeText = UIKit.Text(root, "", 26, new Vector2(0f, 1f), new Vector2(160f, -70f), TextAnchor.UpperLeft);
            currencyText = UIKit.Text(root, "", 36, new Vector2(1f, 1f), new Vector2(-30f, -28f), TextAnchor.UpperRight);
            currencyText.color = UIKit.Gold;
            campText = UIKit.Text(root, "", 64, new Vector2(0.5f, 0f), new Vector2(0f, 150f), TextAnchor.LowerCenter);
            campText.color = new Color(1f, 0.85f, 0.15f);
            controlsHint = UIKit.Text(root, "SHIFT boost   •   SPACE super speed", 22, new Vector2(1f, 0f), new Vector2(-30f, 24f), TextAnchor.LowerRight);
            controlsHint.color = new Color(1f, 1f, 1f, 0.55f);

            // Obstacle level progress: a slim bar across the very top of the screen.
            levelBar = UIKit.Node(root, "LevelProgress");
            var lr = (RectTransform)levelBar.transform;
            lr.anchorMin = new Vector2(0f, 1f);
            lr.anchorMax = new Vector2(1f, 1f);
            lr.pivot = new Vector2(0.5f, 1f);
            lr.sizeDelta = new Vector2(0f, 10f);
            lr.anchoredPosition = Vector2.zero;
            levelFill = UIKit.Bar(levelBar.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 10f), UIKit.Green);
        }

        Text Label(Transform p, string s, Vector2 pos, TextAnchor a)
        {
            var t = UIKit.Text(p, s, 20, new Vector2(0.5f, 1f), pos, a, false);
            t.color = UIKit.Muted;
            return t;
        }

        Text Small(Transform p, Vector2 pos, TextAnchor a, Color c)
        {
            var t = UIKit.Text(p, "", 22, new Vector2(0.5f, 1f), pos, a, false);
            t.color = c;
            return t;
        }

        public void Refresh(GameManager manager)
        {
            bool hud = manager.State == GameState.Racing || manager.State == GameState.Countdown ||
                       (manager.State == GameState.GameOver && !results.gameObject.activeSelf);
            bool obstacle = Session.IsObstacle;
            hudPanel.SetActive(hud);
            speedText.enabled = modeText.enabled = currencyText.enabled = hud;
            controlsHint.enabled = hud && !TouchControls.Active;
            pauseButton.gameObject.SetActive(hud && manager.CanPause && !manager.Paused);
            levelBar.SetActive(hud && Session.Mode == GameMode.ObstacleLevels);

            var p = manager.Player;
            bool camping = hud && manager.State == GameState.Racing && p != null && !p.IsEliminated && p.CampWarningActive;
            campText.enabled = camping && Mathf.Repeat(Time.unscaledTime * 3f, 1f) < 0.7f;
            if (camping)
            {
                float left = Mathf.Max(0f, GameConfig.Current.campLoseSeconds - p.CampTime);
                campText.text = "MOVE!  NO CAMPING  " + left.ToString("0.0") + "s\n<size=34>Warning " + p.CampWarnings + " of " + GameConfig.Current.campMaxWarnings + "</size>";
            }

            if (hud && p != null)
            {
                scoreValue.text = manager.Score.Score.ToString("N0");
                int secs = Mathf.FloorToInt(manager.ElapsedTime);
                timeValue.text = (secs / 60).ToString("00") + ":" + (secs % 60).ToString("00");
                if (obstacle)
                {
                    racersLabel.text = "LIVES";
                    racersValue.text = (p.IsEliminated ? 0 : 1 + manager.LivesLeft).ToString();
                }
                else
                {
                    racersLabel.text = "RACERS";
                    racersValue.text = manager.AliveCount + " / " + manager.Racers.Count;
                }

                if (p.Stamina)
                {
                    staminaFill.fillAmount = Mathf.Lerp(staminaFill.fillAmount, p.Stamina.Normalized, Time.unscaledDeltaTime * 12f);
                    staminaValue.text = Mathf.RoundToInt(p.Stamina.Current) + " / " + Mathf.RoundToInt(p.Stamina.Max) + (p.Stamina.IsBoosting ? "   BOOSTING" : "");
                }
                if (p.Super)
                {
                    int need = GameConfig.Current.pointsPerSuperCharge;
                    if (p.Super.IsActive)
                    {
                        superFill.fillAmount = p.Super.Remaining / GameConfig.Current.superDuration;
                        superValue.text = "ACTIVE!";
                    }
                    else if (p.Super.Charges > 0)
                    {
                        superFill.fillAmount = 1f;
                        superValue.text = Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.6f ? "READY - PRESS SPACE" : "";
                    }
                    else
                    {
                        superFill.fillAmount = p.Super.Progress / need;
                        superValue.text = Mathf.FloorToInt(p.Super.Progress).ToString("N0") + " / " + need.ToString("N0");
                    }
                }
                if (p.Acceleration && p.Acceleration.IsActive)
                {
                    accelLine.color = UIKit.Gold;
                    accelLine.text = "»  ACCELERATION ACTIVE   <size=24>+" + GameConfig.Current.accelerationPointsPerSecond + " pts/s</size>";
                }
                else if (p.Acceleration && p.Acceleration.HoldTime > 0.4f)
                {
                    accelLine.color = new Color(1f, 1f, 1f, 0.6f);
                    accelLine.text = "hold the line...  " + Mathf.Max(0f, p.Acceleration.RequiredSeconds - p.Acceleration.HoldTime).ToString("0.0") + "s";
                }
                else accelLine.text = "";

                var tm = FindAnyObjectByType<Treadmill>();
                speedText.text = "SPEED  " + ((tm ? tm.speed : 0f) * 3.6f).ToString("0") + " km/h" + (obstacle ? "" : "   •   LV " + manager.SpeedLevel);
                if (obstacle)
                {
                    modeText.text = Session.Mode == GameMode.ObstacleLevels && manager.CurrentLevel != null
                        ? "LEVEL " + manager.CurrentLevel.number + " - " + manager.CurrentLevel.name + "   " + Mathf.Max(0f, manager.CurrentLevel.duration - manager.ElapsedTime).ToString("0") + "s left"
                        : "FREE PLAY   •   " + (manager.Director ? manager.Director.Dodged : 0) + " dodged";
                    modeText.color = UIKit.GearColor;
                    currencyText.text = "GEARS  " + manager.RunGears;
                    currencyText.color = UIKit.GearColor;
                    levelFill.fillAmount = manager.LevelProgress;
                }
                else
                {
                    modeText.text = manager.Difficulty ? "AI: " + manager.Difficulty.displayName : "";
                    if (manager.Difficulty) modeText.color = manager.Difficulty.color;
                    currencyText.text = "COINS  " + manager.Coins;
                    currencyText.color = UIKit.Gold;
                }
                coinPulse = Mathf.MoveTowards(coinPulse, 0f, Time.unscaledDeltaTime * 4f);
                currencyText.transform.localScale = Vector3.one * (1f + coinPulse * 0.3f);
            }

            // Banner pops in, holds, then fades.
            float age = Time.unscaledTime - bannerTime;
            bannerText.transform.localScale = Vector3.one * (age < 0.15f ? Mathf.Lerp(1.6f, 1f, age / 0.15f) : 1f);
            float alpha = Mathf.Clamp01(1f - (age - 1.1f) / 0.4f);
            UIKit.SetAlpha(bannerText, alpha);
            UIKit.SetAlpha(bannerSub, alpha);
            UIKit.SetAlpha(noticeText, Mathf.Clamp01(1f - (Time.unscaledTime - noticeTime - 1.6f) / 0.4f));

            UpdateFloaters();
            if (hintBox.activeSelf && Time.unscaledTime - hintTime > 6f) hintBox.SetActive(false);

            FitAspect();
            touch.Tick(manager.State == GameState.Racing && !manager.Paused);
            bool showStick = !TouchControls.Active && PlayerController.Dragging && manager.State == GameState.Racing;
            stickBase.gameObject.SetActive(showStick);
            stickKnob.gameObject.SetActive(showStick);
            if (showStick)
            {
                var origin = PlayerController.DragOrigin;
                stickBase.position = origin;
                stickKnob.position = origin + Vector2.ClampMagnitude(PlayerController.DragCurrent - origin, 110f);
            }
        }

        /// <summary>Esc / Android back: close the top-most menu overlay or sub-screen.</summary>
        public void HandleBack()
        {
            if (characters.gameObject.activeSelf) characters.Close();
            else if (shop.gameObject.activeSelf) shop.Close();
            else if (achievementsScreen.gameObject.activeSelf) achievementsScreen.Close();
            else if (menuRoot.activeSelf && currentScreen != MainScreen) ShowScreen(MainScreen);
        }

        public void OnPauseChanged(bool paused)
        {
            if (paused) pause.Show();
            else pause.Hide();
        }

        public void PulseCoins() => coinPulse = 1f;

        public void Banner(string text, Color color, string sub = "")
        {
            bannerText.text = text;
            bannerText.color = color;
            bannerSub.text = sub;
            bannerTime = Time.unscaledTime;
        }

        /// <summary>Coaching tip in a box near the bottom of the screen; stays up for about six seconds.</summary>
        public void Hint(string text)
        {
            hintText.text = text;
            hintTime = Time.unscaledTime;
            hintBox.SetActive(true);
            ScalePunch.On(hintBox.transform, 0.12f);
            AudioFX.Play(Sfx.Beep, 0.4f, 1.3f);
        }

        public void SmallNotice(string text, Color color)
        {
            noticeText.text = text;
            noticeText.color = color;
            noticeTime = Time.unscaledTime;
        }

        /// <summary>"+100" / "+250" popping up from a world position.</summary>
        public void FloatingPoints(int points, Vector3? world)
        {
            if (!world.HasValue) return;
            var t = UIKit.Text(root, "+" + points, points >= 200 ? 48 : 40, new Vector2(0f, 0f), Vector2.zero, TextAnchor.MiddleCenter, true, 300f);
            t.color = points >= 200 ? UIKit.Gold : UIKit.Cyan;
            floaters.Add((t, world.Value + Vector3.up * 0.8f, Time.unscaledTime));
        }

        void UpdateFloaters()
        {
            var cam = Camera.main;
            for (int i = floaters.Count - 1; i >= 0; i--)
            {
                var (t, w, born) = floaters[i];
                float age = Time.unscaledTime - born;
                if (age > 1.1f || cam == null) { Destroy(t.gameObject); floaters.RemoveAt(i); continue; }
                var sp = cam.WorldToScreenPoint(w);
                t.transform.position = sp + Vector3.up * age * 90f;
                t.transform.localScale = Vector3.one * (age < 0.12f ? Mathf.Lerp(1.5f, 1f, age / 0.12f) : 1f);
                UIKit.SetAlpha(t, Mathf.Clamp01(1f - (age - 0.7f) / 0.4f));
            }
        }

        // ================= Main menu =================

        GameObject BuildMainScreen()
        {
            var s = Screen("Main", false);
            var t = s.transform;
            UIKit.Text(t, "TREADMILL MAYHEM", 104, new Vector2(0.5f, 1f), new Vector2(0f, -26f), TextAnchor.UpperCenter).color = UIKit.Gold;
            menuHigh = UIKit.Text(t, "", 34, new Vector2(0.5f, 1f), new Vector2(0f, -156f), TextAnchor.UpperCenter);
            menuHigh.color = UIKit.Gold;
            coinPill = UIKit.CurrencyPill(t, new Vector2(1f, 1f), new Vector2(-30f, -30f), Currency.Coins, out _);
            gearPill = UIKit.CurrencyPill(t, new Vector2(1f, 1f), new Vector2(-30f, -106f), Currency.Gears, out _);

            // ---- Left: mode + its options.
            UIKit.Text(t, "GAME MODE", 28, new Vector2(0f, 0.5f), new Vector2(60f, 330f), TextAnchor.MiddleLeft).color = new Color(1f, 1f, 1f, 0.85f);
            var modes = new[] { (GameMode.Race, "RACE", UIKit.Orange), (GameMode.ObstacleFree, "OBSTACLE: FREE PLAY", UIKit.Cyan), (GameMode.ObstacleLevels, "OBSTACLE: LEVELS", UIKit.Green) };
            for (int i = 0; i < modes.Length; i++)
            {
                var (mode, label, color) = modes[i];
                var b = UIKit.Button(t, label, new Vector2(0f, 0.5f), new Vector2(60f, 265f - i * 78f), new Vector2(380f, 66f), color, () => GameManager.Instance?.SetMode(mode), 28);
                ((RectTransform)b.transform).pivot = new Vector2(0f, 0.5f);
                modeButtons[mode] = b;
            }

            difficultyGroup = UIKit.Node(t, "Difficulty");
            UIKit.Stretch((RectTransform)difficultyGroup.transform);
            var dg = difficultyGroup.transform;
            UIKit.Text(dg, "AI DIFFICULTY", 26, new Vector2(0f, 0.5f), new Vector2(60f, 10f), TextAnchor.MiddleLeft).color = new Color(1f, 1f, 1f, 0.85f);
            var gm = FindAnyObjectByType<GameManager>();
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var prof = gm && gm.difficulties != null && i < gm.difficulties.Length ? gm.difficulties[i] : null;
                var c = prof ? prof.color : Color.gray;
                var b = UIKit.Button(dg, (i + 1) + "  " + (prof ? prof.displayName : "LEVEL " + (i + 1)), new Vector2(0f, 0.5f), new Vector2(60f, -45f - i * 62f), new Vector2(300f, 54f), c, () => GameManager.Instance?.SetDifficulty(idx), 26);
                ((RectTransform)b.transform).pivot = new Vector2(0f, 0.5f);
                diffButtons.Add(b);
            }
            diffBlurb = UIKit.Text(dg, "", 22, new Vector2(0f, 0.5f), new Vector2(60f, -360f), TextAnchor.UpperLeft, true, 430f);
            diffBlurb.horizontalOverflow = HorizontalWrapMode.Wrap;
            ((RectTransform)diffBlurb.transform).sizeDelta = new Vector2(430f, 120f);

            levelGroup = UIKit.Node(t, "LevelPicker");
            UIKit.Stretch((RectTransform)levelGroup.transform);
            var lg = levelGroup.transform;
            UIKit.Text(lg, "PICK A LEVEL", 26, new Vector2(0f, 0.5f), new Vector2(60f, 10f), TextAnchor.MiddleLeft).color = new Color(1f, 1f, 1f, 0.85f);
            UIKit.Button(lg, "<", new Vector2(0f, 0.5f), new Vector2(95f, -60f), new Vector2(70f, 70f), UIKit.PanelLight, () => StepLevel(-1), 40);
            UIKit.Button(lg, ">", new Vector2(0f, 0.5f), new Vector2(405f, -60f), new Vector2(70f, 70f), UIKit.PanelLight, () => StepLevel(1), 40);
            levelLabel = UIKit.Text(lg, "", 44, new Vector2(0f, 0.5f), new Vector2(250f, -60f), TextAnchor.MiddleCenter, true, 240f);
            levelName = UIKit.Text(lg, "", 26, new Vector2(0f, 0.5f), new Vector2(250f, -125f), TextAnchor.MiddleCenter, true, 420f);
            levelName.color = UIKit.GearColor;

            freeGroup = UIKit.Node(t, "FreePlayInfo");
            UIKit.Stretch((RectTransform)freeGroup.transform);
            freeBest = UIKit.Text(freeGroup.transform, "", 28, new Vector2(0f, 0.5f), new Vector2(60f, -20f), TextAnchor.UpperLeft, true, 460f);

            // ---- Right: shop and info.
            MenuButton(t, "CHARACTERS", 360f, UIKit.Orange, () => characters.Show());
            upgradesButton = MenuButton(t, "UPGRADES", 270f, UIKit.Purple, () => OpenShop(Session.IsObstacle ? ShopScreen.Tab.Obstacle : ShopScreen.Tab.Tuna));
            upgradeBadge = UIKit.Text(upgradesButton.transform, "!", 40, new Vector2(1f, 1f), new Vector2(-6f, 10f), TextAnchor.UpperRight, true, 60f);
            upgradeBadge.color = UIKit.Gold;
            upgradeBadge.gameObject.AddComponent<AttentionPulse>();
            MenuButton(t, "ARENA DECOR", 180f, new Color(1f, 0.5f, 0.75f), () => OpenShop(ShopScreen.Tab.Decor));
            var ach = MenuButton(t, "ACHIEVEMENTS", 90f, UIKit.Gold, () => achievementsScreen.Show());
            achievementsLabel = ach.GetComponentInChildren<Text>();
            MenuButton(t, "LEADERBOARD", 0f, UIKit.Cyan, () => ShowScreen(LeaderboardScreen));
            MenuButton(t, "SETTINGS", -90f, new Color(0.75f, 0.75f, 0.85f), () => ShowScreen(SettingsScreen));

            var prompt = UIKit.Text(t, "PRESS THE BIG YELLOW BUTTON TO PLAY", 50, new Vector2(0.5f, 0f), new Vector2(0f, 110f), TextAnchor.LowerCenter);
            prompt.gameObject.AddComponent<Pulse>();
            UIKit.Text(t, Application.isMobilePlatform ? "Tap it to start!" : "Click it  •  or press SPACE / ENTER", 26, new Vector2(0.5f, 0f), new Vector2(0f, 68f), TextAnchor.LowerCenter).color = new Color(1f, 1f, 1f, 0.8f);
            UIKit.Text(t, Application.isMobilePlatform ? "Left thumb: steer   •   hold BOOST for stamina boost   •   tap SUPER when it glows" : "WASD / Arrows / drag to roll   •   SHIFT stamina boost   •   SPACE super speed", 22, new Vector2(0.5f, 0f), new Vector2(0f, 28f), TextAnchor.LowerCenter).color = new Color(1f, 1f, 1f, 0.7f);
            return s;
        }

        Button MenuButton(Transform t, string label, float y, Color c, UnityEngine.Events.UnityAction action)
        {
            var b = UIKit.Button(t, label, new Vector2(1f, 0.5f), new Vector2(-60f, y - 45f), new Vector2(340f, 74f), c, action, 32);
            ((RectTransform)b.transform).pivot = new Vector2(1f, 0.5f);
            return b;
        }

        void StepLevel(int dir)
        {
            int max = SaveSystem.Data.obstacleLevel;
            Session.Level = Mathf.Clamp(Session.Level + dir, 1, max);
            OnModeChanged();
        }

        public void OnModeChanged()
        {
            foreach (var kv in modeButtons)
            {
                bool on = kv.Key == Session.Mode;
                kv.Value.transform.localScale = Vector3.one * (on ? 1.06f : 0.92f);
                var img = kv.Value.GetComponent<Image>();
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, on ? 1f : 0.45f);
                var pb = kv.Value.GetComponent<PressBounce>();
                if (pb) pb.Rebase(kv.Value.transform.localScale);
            }
            difficultyGroup.SetActive(Session.Mode == GameMode.Race);
            levelGroup.SetActive(Session.Mode == GameMode.ObstacleLevels);
            freeGroup.SetActive(Session.Mode == GameMode.ObstacleFree);
            Session.Level = Mathf.Clamp(Session.Level, 1, SaveSystem.Data.obstacleLevel);
            var lvl = Levels.Get(Session.Level);
            levelLabel.text = "LEVEL " + lvl.number;
            levelName.text = lvl.name + "  •  survive " + lvl.duration + "s\n<size=22>Unlocked " + SaveSystem.Data.obstacleLevel + " / " + Levels.Count + "   •   reward " + lvl.gearReward + " gears</size>";
            freeBest.text = "Dodge endless waves of junk.\nBest time  " + SaveSystem.Data.obstacleBestTime.ToString("0.0") + "s\nBest score  " + SaveSystem.Data.obstacleHighScore.ToString("N0");
            RefreshMenuInfo();
        }

        public void OnDifficultyChanged()
        {
            var manager = FindAnyObjectByType<GameManager>();
            if (manager == null) return;
            for (int i = 0; i < diffButtons.Count; i++)
            {
                bool on = i == manager.DifficultyIndex;
                var prof = i < manager.difficulties.Length ? manager.difficulties[i] : null;
                var c = prof ? prof.color : Color.gray;
                diffButtons[i].transform.localScale = Vector3.one * (on ? 1.06f : 0.94f);
                diffButtons[i].GetComponent<Image>().color = on ? c : new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.5f, 0.85f);
                var pb = diffButtons[i].GetComponent<PressBounce>();
                if (pb) pb.Rebase(diffButtons[i].transform.localScale);
            }
            if (diffBlurb && manager.Difficulty) diffBlurb.text = manager.Difficulty.blurb;
        }

        void RefreshMenuInfo()
        {
            if (coinPill == null) return;
            var d = SaveSystem.Data;
            coinPill.text = d.totalCoins.ToString("N0");
            gearPill.text = d.gears.ToString("N0");
            menuHigh.text = Session.IsObstacle
                ? "BEST FREE PLAY  " + d.obstacleBestTime.ToString("0.0") + "s   •   LEVELS " + (d.obstacleLevel - 1) + " / " + Levels.Count + " CLEARED"
                : "HIGH SCORE  " + d.highScore.ToString("N0") + "   •   BEST TIME  " + (d.bestTime > 0f ? d.bestTime.ToString("0.0") + "s" : "--");
            upgradeBadge.gameObject.SetActive(Upgrades.AnyAffordable(Currency.Coins) || Upgrades.AnyAffordable(Currency.Gears));
            if (achievementsLabel) achievementsLabel.text = "ACHIEVEMENTS  " + Achievements.UnlockedCount + "/" + Achievements.All.Count;
        }

        GameObject BuildLeaderboardScreen()
        {
            var s = Screen("Leaderboard", true);
            var card = UIKit.Box(s.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1250f, 900f), UIKit.Panel);
            UIKit.Text(card.transform, "LOCAL LEADERBOARD", 64, new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.UpperCenter).color = UIKit.Gold;
            UIKit.Text(card.transform, "Your top 10 race runs, saved on this device only", 26, new Vector2(0.5f, 1f), new Vector2(0f, -110f), TextAnchor.UpperCenter).color = UIKit.Muted;
            menuBoard = new LeaderboardTable(card.transform, -170f, true);
            UIKit.Button(card.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(260f, 80f), new Color(0.75f, 0.75f, 0.85f), () => ShowScreen(MainScreen), 34);
            return s;
        }

        GameObject BuildSettingsScreen()
        {
            var s = Screen("Settings", true);
            var card = UIKit.Box(s.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 720f), UIKit.Panel);
            UIKit.Text(card.transform, "SETTINGS", 64, new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.UpperCenter).color = UIKit.Gold;
            var d = SaveSystem.Data;
            VolumeSlider(card.transform, "MASTER", -160f, d.masterVolume, v => { SaveSystem.Data.masterVolume = v; AudioFX.ApplyVolumes(); });
            VolumeSlider(card.transform, "MUSIC", -270f, d.musicVolume, v => { SaveSystem.Data.musicVolume = v; AudioFX.ApplyVolumes(); });
            VolumeSlider(card.transform, "SOUND FX", -380f, d.sfxVolume, v => { SaveSystem.Data.sfxVolume = v; AudioFX.ApplyVolumes(); AudioFX.Play(Sfx.Click, 0.8f); });
            Text vibLabel;
            UIKit.Button(card.transform, "", new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(420f, 74f), new Color(0.3f, 0.32f, 0.45f), null, out vibLabel, 30)
                .onClick.AddListener(() => { Haptics.Enabled = !Haptics.Enabled; vibLabel.text = "VIBRATION: " + (Haptics.Enabled ? "ON" : "OFF"); Haptics.Buzz(); });
            vibLabel.text = "VIBRATION: " + (Haptics.Enabled ? "ON" : "OFF");
            UIKit.Button(card.transform, "BACK", new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(260f, 80f), new Color(0.75f, 0.75f, 0.85f), () => { SaveSystem.Save(); ShowScreen(MainScreen); }, 34);
            return s;
        }

        void VolumeSlider(Transform parent, string label, float y, float value, UnityEngine.Events.UnityAction<float> onChange)
        {
            var l = UIKit.Text(parent, label, 32, new Vector2(0.5f, 1f), new Vector2(-380f, y), TextAnchor.MiddleLeft, false, 240f);
            ((RectTransform)l.transform).pivot = new Vector2(0f, 0.5f);

            var go = UIKit.Node(parent, "Slider_" + label);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(110f, y);
            rt.sizeDelta = new Vector2(500f, 30f);

            var bg = UIKit.Box(go.transform, "Background", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 16f), UIKit.Track, true);
            var fillArea = UIKit.Node(go.transform, "Fill Area");
            var fa = (RectTransform)fillArea.transform;
            fa.anchorMin = new Vector2(0f, 0.5f);
            fa.anchorMax = new Vector2(1f, 0.5f);
            fa.sizeDelta = new Vector2(0f, 16f);
            var fill = UIKit.Box(fillArea.transform, "Fill", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 16f), UIKit.Cyan);
            var handleArea = UIKit.Node(go.transform, "Handle Area");
            UIKit.Stretch((RectTransform)handleArea.transform);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            ((RectTransform)handle.transform).sizeDelta = new Vector2(44f, 44f);
            var himg = handle.GetComponent<Image>();
            himg.sprite = UIKit.Circle;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = (RectTransform)handle.transform;
            slider.targetGraphic = himg;
            slider.value = value;
            slider.onValueChanged.AddListener(onChange);
        }

        GameObject Screen(string name, bool dim)
        {
            var go = new GameObject(name, typeof(RectTransform));
            UIKit.Stretch((RectTransform)go.transform);
            if (dim)
            {
                var img = go.AddComponent<Image>();
                img.color = new Color(0.05f, 0.04f, 0.14f, 0.6f);
            }
            return go;
        }

        RectTransform MakeStick(float size, Color c)
        {
            var img = UIKit.Disc(root, new Vector2(0f, 0f), Vector2.zero, size, c);
            img.gameObject.SetActive(false);
            return (RectTransform)img.transform;
        }
    }
}
