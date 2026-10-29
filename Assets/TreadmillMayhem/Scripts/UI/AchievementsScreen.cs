using System;
using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>Overlay listing every achievement with progress, reward and unlocked state.</summary>
    public class AchievementsScreen : MonoBehaviour
    {
        public Action Closed;
        Transform list;
        Text header;

        public static AchievementsScreen Build(Transform canvas)
        {
            var root = UIKit.Backdrop(canvas, "Achievements", 0.97f);
            var s = root.AddComponent<AchievementsScreen>();
            s.Construct();
            root.SetActive(false);
            return s;
        }

        void Construct()
        {
            var t = transform;
            header = UIKit.Text(t, "ACHIEVEMENTS", 80, new Vector2(0.5f, 1f), new Vector2(0f, -28f), TextAnchor.UpperCenter);
            header.color = UIKit.Gold;
            UIKit.Button(t, "BACK", new Vector2(0f, 1f), new Vector2(150f, -75f), new Vector2(220f, 80f), new Color(0.75f, 0.75f, 0.85f), Close, 34);
            list = UIKit.Node(t, "List").transform;
            UIKit.Stretch((RectTransform)list);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            foreach (Transform c in list) Destroy(c.gameObject);
            var all = Achievements.All;
            int perCol = Mathf.CeilToInt(all.Count / 2f);
            for (int i = 0; i < all.Count; i++)
            {
                int col = i / perCol, row = i % perCol;
                Row(all[i], new Vector2(col == 0 ? -450f : 450f, 300f - row * 112f));
            }
            header.text = "ACHIEVEMENTS   " + Achievements.UnlockedCount + " / " + all.Count;
        }

        void Row(AchievementDef a, Vector2 pos)
        {
            bool done = Achievements.IsUnlocked(a.id);
            var card = UIKit.Box(list, a.id, new Vector2(0.5f, 0.5f), pos, new Vector2(860f, 100f), done ? new Color(0.2f, 0.32f, 0.25f, 0.95f) : UIKit.PanelLight);
            var t = card.transform;
            var badge = UIKit.Disc(t, new Vector2(0f, 0.5f), new Vector2(52f, 0f), 70f, done ? UIKit.Gold : new Color(0.35f, 0.37f, 0.46f));
            UIKit.Text(badge.transform, done ? "OK" : "?", 26, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, false, 70f).color = UIKit.Ink;
            UIKit.Text(t, a.name, 30, new Vector2(0f, 1f), new Vector2(102f, -14f), TextAnchor.UpperLeft, true, 500f).color = done ? UIKit.Gold : Color.white;
            UIKit.Text(t, a.description, 22, new Vector2(0f, 1f), new Vector2(102f, -54f), TextAnchor.UpperLeft, false, 520f).color = UIKit.Muted;
            var reward = UIKit.Text(t, done ? "CLAIMED" : "+" + a.reward + " COINS", 26, new Vector2(1f, 1f), new Vector2(-24f, -16f), TextAnchor.UpperRight, true, 220f);
            reward.color = done ? UIKit.Green : UIKit.Gold;

            if (!done && a.progress != null && a.target > 0f)
            {
                float p = Mathf.Clamp01(a.progress() / a.target);
                var bar = UIKit.Bar(t, new Vector2(1f, 0f), new Vector2(-24f, 22f), new Vector2(200f, 14f), UIKit.Cyan);
                ((RectTransform)bar.transform.parent).pivot = new Vector2(1f, 0f);
                bar.fillAmount = p;
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }

    /// <summary>Small slide-in banner when an achievement unlocks mid-run; never blocks input.</summary>
    public class AchievementToast : MonoBehaviour
    {
        RectTransform panel;
        Text title, body;
        float shownAt = -10f;
        readonly System.Collections.Generic.Queue<AchievementDef> queue = new System.Collections.Generic.Queue<AchievementDef>();

        public static AchievementToast Build(Transform canvas)
        {
            var go = UIKit.Box(canvas, "AchievementToast", new Vector2(0.5f, 1f), new Vector2(0f, 200f), new Vector2(720f, 110f), new Color(0.12f, 0.1f, 0.25f, 0.95f));
            var toast = go.AddComponent<AchievementToast>();
            toast.panel = (RectTransform)go.transform;
            UIKit.Disc(go.transform, new Vector2(0f, 0.5f), new Vector2(58f, 0f), 76f, UIKit.Gold);
            UIKit.Text(go.transform, "!", 44, new Vector2(0f, 0.5f), new Vector2(58f, 0f), TextAnchor.MiddleCenter, false, 76f).color = UIKit.Ink;
            toast.title = UIKit.Text(go.transform, "", 30, new Vector2(0f, 1f), new Vector2(110f, -16f), TextAnchor.UpperLeft, true, 580f);
            toast.title.color = UIKit.Gold;
            toast.body = UIKit.Text(go.transform, "", 24, new Vector2(0f, 1f), new Vector2(110f, -60f), TextAnchor.UpperLeft, false, 580f);
            Achievements.Unlocked += toast.Enqueue;
            return toast;
        }

        void OnDestroy() => Achievements.Unlocked -= Enqueue;

        void Enqueue(AchievementDef a) => queue.Enqueue(a);

        void Update()
        {
            float age = Time.unscaledTime - shownAt;
            if (age > 3.2f && queue.Count > 0)
            {
                var a = queue.Dequeue();
                title.text = "ACHIEVEMENT: " + a.name;
                body.text = a.description + "   <color=#FFD633>+" + a.reward + " coins</color>";
                shownAt = Time.unscaledTime;
                age = 0f;
                AudioFX.Play(Sfx.SuperReady, 0.6f, 1.3f);
            }
            // Slide down, hold, slide back up.
            float y = age < 0.3f ? Mathf.Lerp(200f, -24f, age / 0.3f) : age < 2.8f ? -24f : Mathf.Lerp(-24f, 200f, (age - 2.8f) / 0.4f);
            panel.anchoredPosition = new Vector2(0f, y);
            panel.SetAsLastSibling();
        }
    }
}
