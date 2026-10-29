using System;
using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>"Welcome back!" modal that pays out coins earned while the player was away.</summary>
    public class OfflinePopup : MonoBehaviour
    {
        public Action Closed;
        Text away, amount, rate;
        Button doubleButton;
        int pending;

        public static OfflinePopup Build(Transform canvas)
        {
            var root = UIKit.Backdrop(canvas, "OfflinePopup", 0.75f);
            var p = root.AddComponent<OfflinePopup>();
            p.Construct();
            root.SetActive(false);
            return p;
        }

        void Construct()
        {
            var card = UIKit.Box(transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 620f), UIKit.Panel).transform;
            UIKit.Text(card, "WELCOME BACK!", 80, new Vector2(0.5f, 1f), new Vector2(0f, -36f), TextAnchor.UpperCenter).color = UIKit.Gold;
            away = UIKit.Text(card, "", 32, new Vector2(0.5f, 1f), new Vector2(0f, -150f), TextAnchor.UpperCenter, false);
            UIKit.Text(card, "Your treadmill kept rolling and earned", 30, new Vector2(0.5f, 1f), new Vector2(0f, -200f), TextAnchor.UpperCenter, false).color = UIKit.Muted;
            UIKit.Disc(card, new Vector2(0.5f, 1f), new Vector2(-230f, -300f), 90f, UIKit.Gold);
            UIKit.Text(card, "$", 52, new Vector2(0.5f, 1f), new Vector2(-230f, -300f), TextAnchor.MiddleCenter, false, 90f).color = UIKit.Ink;
            amount = UIKit.Text(card, "", 84, new Vector2(0.5f, 1f), new Vector2(40f, -300f), TextAnchor.MiddleCenter);
            amount.color = UIKit.Gold;
            rate = UIKit.Text(card, "", 24, new Vector2(0.5f, 1f), new Vector2(0f, -385f), TextAnchor.UpperCenter, false);
            rate.color = UIKit.Muted;
            UIKit.Button(card, "COLLECT", new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(380f, 100f), UIKit.Green, () => Collect(1), 44);
            // Rewarded-ad doubling is wired in by the monetization layer when available.
            doubleButton = UIKit.Button(card, "WATCH AD  x2", new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(380f, 80f), UIKit.Purple, () => Ads.ShowRewarded(ok => { if (ok) Collect(2); }), 32);
        }

        /// <summary>Shows the popup if there's anything to collect. Returns false if nothing was pending.</summary>
        public bool TryShow()
        {
            pending = OfflineRewards.Pending(out var span);
            if (pending <= 0) return false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            away.text = "You were away " + OfflineRewards.Describe(span);
            CountUp.On(amount, 0f, pending, 1.2f, "+", "");
            rate.text = Mathf.RoundToInt(OfflineRewards.RatePerHour) + " coins / hour  (max " + OfflineRewards.MaxHours + "h)  •  boost it with OFFLINE EARNINGS";
            doubleButton.gameObject.SetActive(Ads.RewardedAvailable);
            AudioFX.Play(Sfx.SuperReady, 0.7f);
            return true;
        }

        void Collect(int multiplier)
        {
            OfflineRewards.Claim(pending * multiplier);
            AudioFX.Play(Sfx.Pickup, 0.9f);
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }
}
