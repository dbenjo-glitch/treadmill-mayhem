#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace TreadmillMayhem.EditorTools
{
    /// <summary>Info.plist entries App Review checks for, written into every iOS build.</summary>
    public static class IOSPostBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            var root = plist.root;
            // App Tracking Transparency prompt text (needed once an ad SDK asks for tracking permission).
            root.SetString("NSUserTrackingUsageDescription",
                "Your data is used to show you more relevant ads, which keeps Treadmill Mayhem free.");
            // The game uses only standard HTTPS encryption, so no export compliance paperwork is needed.
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            root.SetBoolean("UIRequiresFullScreen", true);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
