using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>
    /// Phone controls: a floating thumbstick anywhere on the left half of the screen, and big BOOST (hold)
    /// and SUPER (tap) buttons on the right. Multi-touch, so you can steer and boost at the same time.
    /// Switches itself on the first time a touchscreen is used (or always on mobile builds).
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static Vector2 Move { get; private set; }
        public static bool BoostHeld { get; private set; }
        /// <summary>Editor/testing: show the phone controls without a touchscreen.</summary>
        public static bool ForceOn;

        const float StickRadius = 120f;

        static bool superQueued;

        RectTransform canvasRect, stickBase, stickKnob, boostBtn, superBtn;
        Image boostImg, superImg, superRing;
        Text superLabel;
        GameObject root;
        int stickTouch = -1;
        Vector2 stickOrigin;

        /// <summary>True once per tap of the SUPER button.</summary>
        public static bool ConsumeSuper()
        {
            bool s = superQueued;
            superQueued = false;
            return s;
        }

        public static TouchControls Build(RectTransform canvas)
        {
            var go = UIKit.Node(canvas, "TouchControls");
            UIKit.Stretch((RectTransform)go.transform);
            var tc = go.AddComponent<TouchControls>();
            tc.canvasRect = canvas;
            tc.Construct();
            return tc;
        }

        void Construct()
        {
            root = gameObject;
            var t = transform;
            stickBase = (RectTransform)UIKit.Disc(t, Vector2.zero, Vector2.zero, StickRadius * 2.1f, new Color(1f, 1f, 1f, 0.16f)).transform;
            stickKnob = (RectTransform)UIKit.Disc(t, Vector2.zero, Vector2.zero, 110f, new Color(1f, 1f, 1f, 0.55f)).transform;
            stickBase.gameObject.SetActive(false);
            stickKnob.gameObject.SetActive(false);

            boostImg = UIKit.Disc(t, new Vector2(1f, 0f), new Vector2(-330f, 150f), 190f, new Color(UIKit.Cyan.r, UIKit.Cyan.g, UIKit.Cyan.b, 0.55f));
            boostBtn = (RectTransform)boostImg.transform;
            UIKit.Text(boostBtn, "BOOST", 34, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, true, 190f);

            superRing = UIKit.Disc(t, new Vector2(1f, 0f), new Vector2(-130f, 300f), 170f, new Color(1f, 1f, 1f, 0.25f));
            superImg = UIKit.Disc(superRing.transform, new Vector2(0.5f, 0.5f), Vector2.zero, 150f, new Color(UIKit.Purple.r, UIKit.Purple.g, UIKit.Purple.b, 0.55f));
            superBtn = (RectTransform)superRing.transform;
            superLabel = UIKit.Text(superImg.transform, "SUPER", 30, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, true, 160f);

            Active = Application.isMobilePlatform;
            root.SetActive(false);
        }

        void OnDisable()
        {
            Move = Vector2.zero;
            BoostHeld = false;
            stickTouch = -1;
        }

        void OnDestroy()
        {
            Move = Vector2.zero;
            BoostHeld = false;
            superQueued = false;
        }

        /// <summary>Called by GameUI every frame; shows the controls only while racing.</summary>
        public void Tick(bool racing)
        {
            var ts = Touchscreen.current;
            if (!Active && (ForceOn || (ts != null && ts.primaryTouch.press.isPressed))) Active = true;
            bool show = Active && racing;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show || ts == null) { Move = Vector2.zero; BoostHeld = false; return; }

            bool boost = false, stickSeen = false;
            Vector2 move = Vector2.zero;
            foreach (TouchControl touch in ts.touches)
            {
                if (!touch.press.isPressed) continue;
                int id = touch.touchId.ReadValue();
                Vector2 pos = touch.position.ReadValue();
                bool began = touch.press.wasPressedThisFrame;

                if (id == stickTouch)
                {
                    stickSeen = true;
                    move = Vector2.ClampMagnitude((pos - stickOrigin) / (StickRadius * Scale), 1f);
                    PlaceStick(pos);
                    continue;
                }
                if (Hit(boostBtn, pos)) { boost = true; continue; }
                if (began && Hit(superBtn, pos)) { superQueued = true; ScalePunch.On(superBtn, 0.2f); continue; }
                // The top strip belongs to the HUD (pause button), not the stick.
                if (began && stickTouch < 0 && pos.x < Screen.width * 0.55f && pos.y < Screen.height * 0.8f)
                {
                    stickTouch = id;
                    stickOrigin = pos;
                    stickSeen = true;
                    PlaceStick(pos);
                }
            }
            if (!stickSeen) stickTouch = -1;

            Move = move;
            BoostHeld = boost;
            stickBase.gameObject.SetActive(stickTouch >= 0);
            stickKnob.gameObject.SetActive(stickTouch >= 0);

            // Button feedback: boost brightens while held and dims when out of stamina; super glows when charged.
            var p = GameManager.Instance != null ? GameManager.Instance.Player : null;
            float stamina = p != null && p.Stamina != null ? p.Stamina.Normalized : 1f;
            boostImg.color = new Color(UIKit.Cyan.r, UIKit.Cyan.g, UIKit.Cyan.b, boost ? 0.9f : stamina > 0.05f ? 0.55f : 0.2f);
            boostBtn.localScale = Vector3.one * (boost ? 0.92f : 1f);
            bool ready = p != null && p.Super != null && p.Super.CanActivate;
            float pulse = ready ? 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f) : 0.3f;
            superImg.color = new Color(UIKit.Purple.r, UIKit.Purple.g, UIKit.Purple.b, pulse);
            superRing.color = ready ? new Color(1f, 0.9f, 0.4f, 0.5f + 0.4f * Mathf.Sin(Time.unscaledTime * 8f)) : new Color(1f, 1f, 1f, 0.2f);
            superLabel.text = p != null && p.Super != null && p.Super.Charges > 1 ? "SUPER x" + p.Super.Charges : "SUPER";
        }

        float Scale => canvasRect.lossyScale.x;

        void PlaceStick(Vector2 pos)
        {
            stickBase.position = stickOrigin;
            stickKnob.position = stickOrigin + Vector2.ClampMagnitude(pos - stickOrigin, StickRadius * Scale);
        }

        static bool Hit(RectTransform rt, Vector2 screenPos)
        {
            // Generous circular hit area (thumbs are imprecise).
            Vector2 c = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            float r = rt.rect.width * 0.5f * rt.lossyScale.x * 1.25f;
            return (screenPos - c).sqrMagnitude <= r * r;
        }
    }
}
