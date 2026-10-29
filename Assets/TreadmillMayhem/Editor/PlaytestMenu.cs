using UnityEditor;

namespace TreadmillMayhem.EditorTools
{
    /// <summary>
    /// Menu entry for the automated bot playtest; enters Play mode first if needed. Also guarantees the
    /// player's save is restored if a batch is cut short by a script reload or by leaving Play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class PlaytestMenu
    {
        const string PendingKey = "TM.PlaytestPending";

        static PlaytestMenu()
        {
            EditorApplication.update += TryStartPending;
            AssemblyReloadEvents.beforeAssemblyReload += PlaytestRunner.AbortAndRestore;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) PlaytestRunner.AbortAndRestore();
            };
        }

        [MenuItem("Tools/Treadmill Mayhem/Run 50-Bot Playtest")]
        static void Run50() => Run(50);

        [MenuItem("Tools/Treadmill Mayhem/Run 10-Bot Smoke Test")]
        static void Run10() => Run(10);

        static void Run(int count)
        {
            SessionState.SetInt(PendingKey, count);
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }

        static void TryStartPending()
        {
            int count = SessionState.GetInt(PendingKey, 0);
            if (count <= 0 || !EditorApplication.isPlaying || GameManager.Instance == null || PlaytestRunner.Running) return;
            SessionState.SetInt(PendingKey, 0);
            PlaytestRunner.Begin(count);
        }
    }
}
