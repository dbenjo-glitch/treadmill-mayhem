using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Phone vibration for the big moments only (knocked out, Super Speed, level clear), so it stays special.
    /// Can be switched off in Settings. Does nothing on desktop.
    /// </summary>
    public static class Haptics
    {
        static float lastBuzz = -10f;

        public static bool Enabled
        {
            get => SaveSystem.Data.haptics;
            set { SaveSystem.Data.haptics = value; SaveSystem.Save(); }
        }

        public static void Buzz()
        {
            if (!Enabled || Time.unscaledTime - lastBuzz < 0.4f) return;
            lastBuzz = Time.unscaledTime;
#if UNITY_IOS || UNITY_ANDROID
            if (!PlaytestRunner.Running) Handheld.Vibrate();
#endif
        }
    }
}
