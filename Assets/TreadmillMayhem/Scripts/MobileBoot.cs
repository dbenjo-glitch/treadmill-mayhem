using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TreadmillMayhem
{
    /// <summary>
    /// One-time runtime setup that makes the phone build feel right: 60 fps, no screen dimming mid-run,
    /// landscape only, and a lighter render load on handhelds.
    /// </summary>
    public static class MobileBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            if (!Application.isMobilePlatform) return;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;

            // Handhelds: shorter shadow range and a touch less resolution keep it smooth and cool.
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = Mathf.Min(urp.shadowDistance, 35f);
                urp.renderScale = SystemInfo.systemMemorySize < 3000 ? 0.8f : 0.9f;
                urp.msaaSampleCount = 2;
            }
        }
    }
}
