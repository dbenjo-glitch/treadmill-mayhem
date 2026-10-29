using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>Everything the results screen needs to know about a finished run.</summary>
    public class RunSummary
    {
        public GameMode mode;
        public int level;
        public bool levelCleared;
        public bool disqualified;
        public float time;
        public int score;
        public int bestScore;
        public bool newHighScore;
        public int orbs;
        public float accelTime;
        public int place;
        public int racers;
        public int coinsEarned;
        public int coinsBefore;
        public int gearsEarned;
        public int gearsBefore;
        public RunRecord record;
        public List<AchievementDef> achievements = new List<AchievementDef>();
    }

    /// <summary>
    /// Post-run screen: what happened, what you earned (animated), your next goal, and one-tap replay.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        public Action PlayAgain, NextLevel, OpenShop, MainMenu;

        Text title, scoreText, scoreLabel, highScore, stats, coinsEarned, coinsTotal, gearsEarned, goal, achievementLine;
        Button playButton, nextButton, shopButton, doubleButton;
        Text playLabel, shopLabel, boardTitle, obstacleBoard;
        GameObject rightPanel, nameRow, boardTable;
        LeaderboardTable table;
        InputField nameField;
        Button saveName;
        Text saveLabel;
        RunSummary summary;

        public bool IsTyping => nameField != null && nameField.isFocused;

        public static ResultsScreen Build(Transform canvas)
        {
            var root = UIKit.Backdrop(canvas, "Results", 0.72f);
            var r = root.AddComponent<ResultsScreen>();
            r.Construct();
            root.SetActive(false);
            return r;
        }

        void Construct()
        {
            var t = transform;
            var left = UIKit.Box(t, "Left", new Vector2(0.5f, 0.5f), new Vector2(-420f, 0f), new Vector2(860f, 1000f), UIKit.Panel).transform;

            title = UIKit.Text(left, "", 96, new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.UpperCenter);
            scoreLabel = UIKit.Text(left, "FINAL SCORE", 24, new Vector2(0.5f, 1f), new Vector2(0f, -150f), TextAnchor.UpperCenter, false);
            scoreLabel.color = UIKit.Muted;
            scoreText = UIKit.Text(left, "", 92, new Vector2(0.5f, 1f), new Vector2(0f, -178f), TextAnchor.UpperCenter);
            scoreText.color = UIKit.Gold;
            highScore = UIKit.Text(left, "NEW HIGH SCORE!", 44, new Vector2(0.5f, 1f), new Vector2(0f, -292f), TextAnchor.UpperCenter);
            highScore.color = new Color(1f, 0.45f, 0.7f);
            highScore.gameObject.AddComponent<AttentionPulse>();
            stats = UIKit.Text(left, "", 30, new Vector2(0.5f, 1f), new Vector2(0f, -350f), TextAnchor.UpperCenter, false);

            var rewards = UIKit.Box(left, "Rewards", new Vector2(0.5f, 1f), new Vector2(0f, -520f), new Vector2(780f, 190f), new Color(0f, 0f, 0f, 0.3f)).transform;
            coinsEarned = UIKit.Text(rewards, "", 50, new Vector2(0.5f, 1f), new Vector2(0f, -16f), TextAnchor.UpperCenter);
            coinsEarned.color = UIKit.Gold;
            coinsTotal = UIKit.Text(rewards, "", 28, new Vector2(0.5f, 1f), new Vector2(0f, -78f), TextAnchor.UpperCenter, false);
            gearsEarned = UIKit.Text(rewards, "", 30, new Vector2(0.5f, 1f), new Vector2(0f, -116f), TextAnchor.UpperCenter, false);
            gearsEarned.color = UIKit.GearColor;
            // Optional rewarded ad: double the coins from this run.
            doubleButton = UIKit.Button(rewards, "WATCH AD  x2 COINS", new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(380f, 56f), UIKit.Purple, OnDoubleCoins, 26);

            goal = UIKit.Text(left, "", 32, new Vector2(0.5f, 1f), new Vector2(0f, -720f), TextAnchor.UpperCenter);
            achievementLine = UIKit.Text(left, "", 24, new Vector2(0.5f, 1f), new Vector2(0f, -800f), TextAnchor.UpperCenter, false);
            achievementLine.color = UIKit.Gold;

            playButton = UIKit.Button(left, "PLAY AGAIN", new Vector2(0.5f, 0f), new Vector2(-270f, 70f), new Vector2(250f, 96f), UIKit.Orange, () => PlayAgain?.Invoke(), out playLabel, 34);
            nextButton = UIKit.Button(left, "NEXT LEVEL", new Vector2(0.5f, 0f), new Vector2(-270f, 70f), new Vector2(250f, 96f), UIKit.Green, () => NextLevel?.Invoke(), 34);
            shopButton = UIKit.Button(left, "UPGRADE TUNA", new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(260f, 96f), UIKit.Purple, () => OpenShop?.Invoke(), out shopLabel, 32);
            UIKit.Button(left, "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(270f, 70f), new Vector2(250f, 96f), new Color(0.45f, 0.5f, 0.95f), () => MainMenu?.Invoke(), 32);

            // Right: local leaderboard with name entry.
            rightPanel = UIKit.Box(t, "Board", new Vector2(0.5f, 0.5f), new Vector2(450f, 0f), new Vector2(860f, 1000f), UIKit.Panel);
            var rp = rightPanel.transform;
            boardTitle = UIKit.Text(rp, "LOCAL TOP 10", 52, new Vector2(0.5f, 1f), new Vector2(0f, -30f), TextAnchor.UpperCenter);
            boardTitle.color = UIKit.Gold;
            boardTable = UIKit.Node(rp, "Table");
            UIKit.Stretch((RectTransform)boardTable.transform);
            table = new LeaderboardTable(boardTable.transform, -120f, false);
            // Obstacle Mode shows level progress and personal bests instead of the race board.
            obstacleBoard = UIKit.Text(rp, "", 32, new Vector2(0.5f, 1f), new Vector2(0f, -120f), TextAnchor.UpperCenter, false, 760f);
            ((RectTransform)obstacleBoard.transform).sizeDelta = new Vector2(760f, 840f);
            obstacleBoard.lineSpacing = 1.15f;
            nameRow = UIKit.Node(rp, "NameRow");
            var nr = (RectTransform)nameRow.transform;
            nr.anchorMin = nr.anchorMax = new Vector2(0.5f, 0f);
            nr.anchoredPosition = new Vector2(0f, 80f);
            UIKit.Text(nameRow.transform, "Name for this run", 22, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), TextAnchor.MiddleCenter, false).color = UIKit.Muted;
            nameField = UIKit.InputField(nameRow.transform, new Vector2(-130f, 0f), new Vector2(380f, 74f));
            saveName = UIKit.Button(nameRow.transform, "SAVE NAME", new Vector2(0.5f, 0.5f), new Vector2(200f, 0f), new Vector2(220f, 74f), UIKit.Green, OnSaveName, out saveLabel, 28);
            nameField.onSubmit.AddListener(_ => OnSaveName());
        }

        public void Show(RunSummary s)
        {
            summary = s;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            bool obstacle = s.mode != GameMode.Race;

            if (s.disqualified) { title.text = "DISQUALIFIED!"; title.color = UIKit.Red; }
            else if (s.levelCleared) { title.text = "LEVEL " + s.level + " CLEAR!"; title.color = UIKit.Green; }
            else if (!obstacle && s.place == 1) { title.text = "LAST CAN STANDING!"; title.color = UIKit.Gold; }
            else { title.text = obstacle ? "WIPED OUT!" : "TUNA DOWN!"; title.color = UIKit.Red; }
            ScalePunch.On(title, 0.3f);

            CountUp.On(scoreText, 0f, s.score, 1.1f, "", "", false);
            highScore.gameObject.SetActive(s.newHighScore);
            if (s.newHighScore) { AudioFX.Play(Sfx.SuperGo, 0.8f, 1.2f); AudioFX.Play(Sfx.Cheer, 0.5f); }

            int secs = Mathf.FloorToInt(s.time);
            string time = (secs / 60) + ":" + (secs % 60).ToString("00");
            var lines = new List<string> { "SURVIVAL TIME  " + time };
            if (!obstacle)
            {
                lines.Add("BEST SCORE  " + s.bestScore.ToString("N0"));
                lines.Add("Orbs " + s.orbs + "   •   Accelerated " + s.accelTime.ToString("0.0") + "s   •   Place #" + s.place + " of " + s.racers);
            }
            else if (s.mode == GameMode.ObstacleLevels) lines.Add(s.levelCleared ? "Level " + s.level + " complete!" : "Level " + s.level + "  •  so close!");
            else lines.Add("BEST FREE PLAY  " + SaveSystem.Data.obstacleBestTime.ToString("0.0") + "s");
            stats.text = string.Join("\n", lines);

            // Rewards count up from what you had.
            coinsEarned.text = "";
            if (obstacle)
            {
                coinsEarned.color = UIKit.GearColor;
                CountUp.On(coinsEarned, 0f, s.gearsEarned, 1.4f, "GEARS  +", "");
                coinsTotal.text = "TOTAL  " + SaveSystem.Data.gears.ToString("N0") + " gears";
                gearsEarned.text = "+1 gear per 10s survived" + (s.levelCleared ? "  •  level bonus included" : "");
                gearsEarned.color = UIKit.Muted;
            }
            else
            {
                coinsEarned.color = UIKit.Gold;
                CountUp.On(coinsEarned, 0f, s.coinsEarned, 1.4f, "TUNA COINS  +", "");
                coinsTotal.text = "TOTAL  " + SaveSystem.Data.totalCoins.ToString("N0") + " coins";
                gearsEarned.text = s.gearsEarned > 0 ? "GEARS  +" + s.gearsEarned + "   (total " + SaveSystem.Data.gears + ")" : "";
                gearsEarned.color = UIKit.GearColor;
            }
            shopLabel.text = obstacle ? "UPGRADE GEAR" : "UPGRADE TUNA";
            doubleButton.gameObject.SetActive(!obstacle && s.coinsEarned > 0 && Ads.RewardedAvailable);
            doubleButton.interactable = true;

            RefreshGoal();
            achievementLine.text = s.achievements.Count > 0 ? "Unlocked: " + string.Join(", ", s.achievements.ConvertAll(a => a.name)) : "";

            bool hasNext = s.levelCleared && s.level < Levels.Count;
            nextButton.gameObject.SetActive(hasNext);
            playButton.gameObject.SetActive(!hasNext);
            playLabel.text = s.mode == GameMode.ObstacleLevels ? "RETRY" : "PLAY AGAIN";

            nameRow.SetActive(!obstacle && s.record != null);
            if (s.record != null) nameField.text = s.record.name;
            saveName.interactable = true;
            saveLabel.text = "SAVE NAME";
            boardTable.SetActive(!obstacle);
            obstacleBoard.gameObject.SetActive(obstacle);
            boardTitle.text = obstacle ? "OBSTACLE RECORDS" : "LOCAL TOP 10";
            if (obstacle) obstacleBoard.text = ObstacleRecords(s);
            else table.Fill(s.record != null ? s.record.id : null);
        }

        static string ObstacleRecords(RunSummary s)
        {
            var d = SaveSystem.Data;
            var sb = new System.Text.StringBuilder();
            sb.Append("<color=#9FE6FF>FREE PLAY BEST</color>\n");
            sb.Append(d.obstacleBestTime.ToString("0.0") + "s   •   " + d.obstacleHighScore.ToString("N0") + " pts\n\n");
            sb.Append("<color=#9FE6FF>LEVELS</color>\n");
            for (int i = 1; i <= Levels.Count; i++)
            {
                var lv = Levels.Get(i);
                bool cleared = i < d.obstacleLevel || (i == Levels.Count && s.levelCleared && s.level == i);
                bool current = !cleared && i == d.obstacleLevel;
                string mark = cleared ? "<color=#5BE38A>CLEAR</color>" : current ? "<color=#FFD24A>NEXT</color>" : "<color=#7C7F99>LOCKED</color>";
                string line = i + ".  " + lv.name + "   " + mark;
                if (s.mode == GameMode.ObstacleLevels && s.level == i) line = "<b>> " + line + " <</b>";
                sb.Append("<size=26>" + line + "</size>\n");
            }
            return sb.ToString();
        }

        /// <summary>Shows the nearest upgrade goal, or "UPGRADE AVAILABLE!" when one is affordable.</summary>
        public void RefreshGoal()
        {
            if (summary == null) return;
            var currency = summary.mode == GameMode.Race ? Currency.Coins : Currency.Gears;
            var pulse = goal.GetComponent<AttentionPulse>();
            if (Upgrades.AnyAffordable(Currency.Coins) || Upgrades.AnyAffordable(Currency.Gears))
            {
                goal.text = "UPGRADE AVAILABLE!";
                goal.color = UIKit.Green;
                if (!pulse) goal.gameObject.AddComponent<AttentionPulse>();
                if (!shopButton.GetComponent<AttentionPulse>()) shopButton.gameObject.AddComponent<AttentionPulse>();
                return;
            }
            if (pulse) Destroy(pulse);
            var sp = shopButton.GetComponent<AttentionPulse>();
            if (sp) Destroy(sp);
            var next = Upgrades.NextGoal(currency) ?? Upgrades.NextGoal(Currency.Coins);
            if (next == null) { goal.text = "Everything maxed - legend!"; goal.color = UIKit.Gold; return; }
            int need = Upgrades.NextCost(next) - SaveSystem.Balance(next.currency);
            string unit = next.currency == Currency.Coins ? "COINS" : "GEARS";
            goal.text = "NEXT: " + next.name + " - " + Upgrades.NextCost(next).ToString("N0") + " " + unit + "\n<size=26><color=#9FE6FF>ONLY " + need.ToString("N0") + " MORE " + unit + "!</color></size>";
            goal.color = Color.white;
        }

        void OnDoubleCoins()
        {
            if (summary == null) return;
            doubleButton.interactable = false;
            Ads.ShowRewarded(ok =>
            {
                if (!ok) { doubleButton.interactable = true; return; }
                SaveSystem.AddCoins(summary.coinsEarned);
                CountUp.On(coinsEarned, summary.coinsEarned, summary.coinsEarned * 2, 0.8f, "TUNA COINS  +", "");
                coinsTotal.text = "TOTAL  " + SaveSystem.Data.totalCoins.ToString("N0") + " coins";
                summary.coinsEarned *= 2;
                doubleButton.gameObject.SetActive(false);
                AudioFX.Play(Sfx.Coin, 0.8f);
                RefreshGoal();
            });
        }

        void OnSaveName()
        {
            if (summary?.record == null) return;
            SaveSystem.RenameRun(summary.record.id, nameField.text);
            saveName.interactable = false;
            saveLabel.text = "SAVED!";
            AudioFX.Play(Sfx.Pickup, 0.6f);
            table.Fill(summary.record.id);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
