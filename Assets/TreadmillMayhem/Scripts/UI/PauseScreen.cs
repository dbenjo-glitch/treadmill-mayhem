using System;
using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>
    /// Mid-run pause overlay: resume, restart, quit, and quick sound toggles.
    /// Opened by the HUD pause button, Esc / P, or automatically when a phone app goes to the background.
    /// </summary>
    public class PauseScreen : MonoBehaviour
    {
        public Action Resume, Restart, Quit;

        Text modeLine, soundLabel, musicLabel;

        public static PauseScreen Build(Transform canvas)
        {
            var root = UIKit.Backdrop(canvas, "Pause", 0.82f);
            var p = root.AddComponent<PauseScreen>();
            p.Construct();
            root.SetActive(false);
            return p;
        }

        void Construct()
        {
            var t = transform;
            var card = UIKit.Box(t, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 800f), UIKit.Panel).transform;
            UIKit.Text(card, "PAUSED", 96, new Vector2(0.5f, 1f), new Vector2(0f, -40f), TextAnchor.UpperCenter).color = UIKit.Gold;
            modeLine = UIKit.Text(card, "", 28, new Vector2(0.5f, 1f), new Vector2(0f, -150f), TextAnchor.UpperCenter, false);
            modeLine.color = UIKit.Muted;

            UIKit.Button(card, "RESUME", new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(480f, 100f), UIKit.Green, () => Resume?.Invoke(), 44);
            UIKit.Button(card, "RESTART", new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(480f, 90f), UIKit.Orange, () => Restart?.Invoke(), 38);
            UIKit.Button(card, "QUIT RUN", new Vector2(0.5f, 1f), new Vector2(0f, -480f), new Vector2(480f, 90f), new Color(0.45f, 0.5f, 0.95f), () => Quit?.Invoke(), 38);

            UIKit.Button(card, "", new Vector2(0.5f, 0f), new Vector2(-125f, 110f), new Vector2(230f, 70f), new Color(0.3f, 0.32f, 0.45f), ToggleSound, out soundLabel, 26);
            UIKit.Button(card, "", new Vector2(0.5f, 0f), new Vector2(125f, 110f), new Vector2(230f, 70f), new Color(0.3f, 0.32f, 0.45f), ToggleMusic, out musicLabel, 26);
            var hint = UIKit.Text(card, "Quitting ends the run without rewards.", 22, new Vector2(0.5f, 0f), new Vector2(0f, 40f), TextAnchor.MiddleCenter, false);
            hint.color = UIKit.Muted;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            modeLine.text = Session.ModeLabel;
            RefreshLabels();
        }

        public void Hide() => gameObject.SetActive(false);

        public bool IsOpen => gameObject.activeSelf;

        void ToggleSound()
        {
            var d = SaveSystem.Data;
            if (d.sfxVolume > 0.01f) { PlayerPrefs.SetFloat("TM.PrevSfx", d.sfxVolume); d.sfxVolume = 0f; }
            else d.sfxVolume = PlayerPrefs.GetFloat("TM.PrevSfx", 1f);
            AudioFX.ApplyVolumes();
            SaveSystem.Save();
            RefreshLabels();
        }

        void ToggleMusic()
        {
            var d = SaveSystem.Data;
            if (d.musicVolume > 0.01f) { PlayerPrefs.SetFloat("TM.PrevMusic", d.musicVolume); d.musicVolume = 0f; }
            else d.musicVolume = PlayerPrefs.GetFloat("TM.PrevMusic", 0.6f);
            AudioFX.ApplyVolumes();
            SaveSystem.Save();
            RefreshLabels();
        }

        void RefreshLabels()
        {
            var d = SaveSystem.Data;
            soundLabel.text = "SOUND: " + (d.sfxVolume > 0.01f ? "ON" : "OFF");
            musicLabel.text = "MUSIC: " + (d.musicVolume > 0.01f ? "ON" : "OFF");
        }
    }
}
