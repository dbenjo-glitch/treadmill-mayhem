using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>Daily login calendar: seven day cards, today's highlighted, one big CLAIM button.</summary>
    public class DailyRewardPopup : MonoBehaviour
    {
        public Action Closed;

        readonly List<(Image card, Text day, Text reward)> days = new List<(Image, Text, Text)>();
        Text streakText;
        Button claimButton;

        public static DailyRewardPopup Build(Transform canvas)
        {
            var root = UIKit.Backdrop(canvas, "DailyReward", 0.8f);
            var p = root.AddComponent<DailyRewardPopup>();
            p.Construct();
            root.SetActive(false);
            return p;
        }

        void Construct()
        {
            var card = UIKit.Box(transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 640f), UIKit.Panel).transform;
            UIKit.Text(card, "DAILY REWARD", 80, new Vector2(0.5f, 1f), new Vector2(0f, -34f), TextAnchor.UpperCenter).color = UIKit.Gold;
            streakText = UIKit.Text(card, "", 28, new Vector2(0.5f, 1f), new Vector2(0f, -135f), TextAnchor.UpperCenter, false);
            streakText.color = UIKit.Muted;

            var cal = DailyRewards.Calendar;
            for (int i = 0; i < cal.Length; i++)
            {
                bool big = i == cal.Length - 1;
                float w = big ? 230f : 170f;
                float x = -600f + i * 190f + (big ? 30f : 0f);
                var c = UIKit.Box(card, "Day" + (i + 1), new Vector2(0.5f, 0.5f), new Vector2(x, 10f), new Vector2(w, 230f), UIKit.PanelLight);
                var dayText = UIKit.Text(c.transform, "DAY " + (i + 1), 26, new Vector2(0.5f, 1f), new Vector2(0f, -14f), TextAnchor.UpperCenter, false, w);
                var r = cal[i];
                string label = r.coins + "\ncoins" + (r.gears > 0 ? "\n+" + r.gears + " gears" : "");
                var rewardText = UIKit.Text(c.transform, label, big ? 34 : 30, new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), TextAnchor.MiddleCenter, true, w);
                rewardText.color = UIKit.Gold;
                days.Add((c.GetComponent<Image>(), dayText, rewardText));
            }

            claimButton = UIKit.Button(card, "CLAIM!", new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(420f, 100f), UIKit.Green, Claim, 48);
        }

        /// <summary>Shows the calendar if today's reward hasn't been claimed. Returns false otherwise.</summary>
        public bool TryShow()
        {
            if (!DailyRewards.Available) return false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            int today = DailyRewards.TodayIndex;
            for (int i = 0; i < days.Count; i++)
            {
                var (img, day, _) = days[i];
                bool past = i < today, now = i == today;
                img.color = now ? new Color(0.35f, 0.3f, 0.12f) : past ? new Color(0.18f, 0.32f, 0.22f) : UIKit.PanelLight;
                day.text = past ? "CLAIMED" : now ? "TODAY" : "DAY " + (i + 1);
                day.color = now ? UIKit.Gold : past ? UIKit.Green : UIKit.Muted;
                img.transform.localScale = Vector3.one * (now ? 1.08f : 1f);
            }
            streakText.text = today == 0 ? "Come back every day - day 7 pays 1,000 coins!" : "Streak: day " + (today + 1) + " of 7  •  don't break it!";
            claimButton.interactable = true;
            AudioFX.Play(Sfx.SuperReady, 0.7f);
            return true;
        }

        void Claim()
        {
            int index = DailyRewards.TodayIndex;
            var r = DailyRewards.Claim();
            claimButton.interactable = false;
            AudioFX.Play(Sfx.Coin, 1f);
            AudioFX.Play(Sfx.Cheer, 0.4f);
            if (index < days.Count)
            {
                var (img, day, _) = days[index];
                Flash.On(img, UIKit.Gold);
                ScalePunch.On(img.transform, 0.2f);
                day.text = "CLAIMED";
                day.color = UIKit.Green;
            }
            FloatUp.Spawn(transform, "+" + r.coins + " COINS" + (r.gears > 0 ? "  +" + r.gears + " GEARS" : ""), new Vector2(0f, 120f), UIKit.Gold, 56);
            Invoke(nameof(Close), 1.1f);
        }

        void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }
}
