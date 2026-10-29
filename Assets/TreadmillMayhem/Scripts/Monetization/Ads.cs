using System;

namespace TreadmillMayhem
{
    /// <summary>
    /// Single entry point for ads. Gameplay code only talks to this class, so the ad network can be swapped
    /// (or removed) without touching game logic. Until an SDK adapter registers itself, no ads are available
    /// and every reward callback reports false, so the game is fully playable ad-free.
    ///
    /// Policy (player-friendly by design):
    ///  - Rewarded ads are always opt-in (double offline earnings, double run coins) and stay available
    ///    after Remove Ads, because players choose them.
    ///  - Interstitials only between runs, at most every 4th run, never in the first 3 runs, never mid-run,
    ///    and never for players who bought Remove Ads.
    /// </summary>
    public static class Ads
    {
        /// <summary>Set by the ad provider adapter once an SDK is integrated and a rewarded ad is loaded.</summary>
        public static Func<bool> RewardedReady = () => false;
        public static Action<Action<bool>> RewardedShow;
        public static Action InterstitialShow;

        public static bool RewardedAvailable => RewardedShow != null && RewardedReady != null && RewardedReady();

        /// <summary>Shows a rewarded ad; the callback gets true only if the player earned the reward.</summary>
        public static void ShowRewarded(Action<bool> onDone)
        {
            if (!RewardedAvailable) { onDone?.Invoke(false); return; }
            RewardedShow(onDone);
        }

        /// <summary>Optional between-run interstitial; never shown to players who bought Remove Ads.</summary>
        public static void MaybeShowInterstitial(int runsPlayed)
        {
            if (Purchases.HasRemoveAds || InterstitialShow == null) return;
            if (runsPlayed > 3 && runsPlayed % 4 == 0) InterstitialShow();
        }
    }
}
