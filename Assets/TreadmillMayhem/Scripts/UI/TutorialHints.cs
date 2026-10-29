using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// First-run coaching: a handful of timed tips during a brand-new player's first run, worded for the
    /// device they're on. Finishes (and never shows again) when that first run ends.
    /// </summary>
    public class TutorialHints : MonoBehaviour
    {
        (float at, string touch, string keys)[] steps;
        int next;
        GameManager gm;
        GameUI ui;

        public static bool ShouldRun
        {
            get
            {
                var d = SaveSystem.Data;
                return !d.tutorialDone && d.runsPlayed == 0 && d.highScore == 0 && !PlaytestRunner.Running;
            }
        }

        public static void Begin(GameManager manager, GameUI ui)
        {
            var t = manager.gameObject.AddComponent<TutorialHints>();
            t.gm = manager;
            t.ui = ui;
            t.steps = Session.IsObstacle
                ? new[]
                {
                    (0.5f, "Steer with your left thumb - dodge everything coming at you!", "Steer with WASD / arrows - dodge everything coming at you!"),
                    (7f, "Grab the GEARS - spend them on Obstacle upgrades.", "Grab the GEARS - spend them on Obstacle upgrades."),
                    (15f, "Warning circles show where crates will land. Move!", "Warning circles show where crates will land. Move!"),
                }
                : new[]
                {
                    (0.5f, "The belt pulls you back - push forward with your left thumb!", "The belt pulls you back - hold W (or Up) to run forward!"),
                    (6f, "Hold BOOST for a burst of speed. Blue orbs refill stamina.", "Hold SHIFT to boost. Blue orbs refill stamina."),
                    (14f, "Run straight for 3 seconds to ACCELERATE for bonus points.", "Run straight for 3 seconds to ACCELERATE for bonus points."),
                    (24f, "Last can standing wins. Bump rivals off the back!", "Last can standing wins. Bump rivals off the back!"),
                    (34f, "No hiding at the front - camp too long and you're out.", "No hiding at the front - camp too long and you're out."),
                };
        }

        void Update()
        {
            if (gm == null) return;
            if (gm.State == GameState.GameOver)
            {
                SaveSystem.Data.tutorialDone = true;
                SaveSystem.Save();
                Destroy(this);
                return;
            }
            if (gm.State != GameState.Racing || next >= steps.Length) return;
            if (gm.ElapsedTime >= steps[next].at)
            {
                if (ui != null) ui.Hint(TouchControls.Active ? steps[next].touch : steps[next].keys);
                next++;
            }
        }
    }
}
