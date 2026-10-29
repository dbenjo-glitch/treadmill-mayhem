using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    // Small self-contained UI tweens, added at runtime by code-built UI. All run on unscaled time so they
    // keep animating during slow-motion and on paused menus.

    /// <summary>Squishes a button on press and plays a click.</summary>
    public class PressBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        Vector3 baseScale = Vector3.one;
        float target = 1f, current = 1f;

        void Awake() => baseScale = transform.localScale;

        public void OnPointerDown(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b && !b.interactable) return;
            target = 0.92f;
            AudioFX.Play(Sfx.Click, 0.5f);
        }

        public void OnPointerUp(PointerEventData e) => target = 1f;

        void Update()
        {
            current = Mathf.Lerp(current, target, Time.unscaledDeltaTime * 18f);
            transform.localScale = baseScale * current;
        }

        public void Rebase(Vector3 scale) => baseScale = scale;
    }

    /// <summary>One-shot elastic scale punch.</summary>
    public class ScalePunch : MonoBehaviour
    {
        float t, amount;
        Vector3 baseScale;

        public static void On(Component c, float amount = 0.25f)
        {
            if (c == null) return;
            var existing = c.GetComponent<ScalePunch>();
            if (existing) { existing.t = 0f; return; }
            var p = c.gameObject.AddComponent<ScalePunch>();
            p.amount = amount;
            p.baseScale = c.transform.localScale;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime * 3f;
            float s = 1f + Mathf.Sin(t * Mathf.PI * 2.5f) * amount * Mathf.Max(0f, 1f - t);
            transform.localScale = baseScale * s;
            if (t >= 1f) { transform.localScale = baseScale; Destroy(this); }
        }
    }

    /// <summary>Counts a Text up (or down) to a target number with an optional prefix/suffix.</summary>
    public class CountUp : MonoBehaviour
    {
        Text text;
        float from, to, t, duration;
        string prefix, suffix;
        bool tick;

        public static void On(Text text, float from, float to, float duration, string prefix = "", string suffix = "", bool tickSound = true)
        {
            if (text == null) return;
            var c = text.GetComponent<CountUp>();
            if (!c) c = text.gameObject.AddComponent<CountUp>();
            c.text = text; c.from = from; c.to = to; c.duration = Mathf.Max(0.05f, duration); c.t = 0f;
            c.prefix = prefix; c.suffix = suffix; c.tick = tickSound;
            c.enabled = true;
        }

        float lastTick;

        void Update()
        {
            t += Time.unscaledDeltaTime / duration;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            text.text = prefix + Mathf.RoundToInt(Mathf.Lerp(from, to, e)).ToString("N0") + suffix;
            if (tick && Time.unscaledTime - lastTick > 0.07f && t < 1f) { lastTick = Time.unscaledTime; AudioFX.Play(Sfx.Coin, 0.15f, 1.3f); }
            if (t >= 1f) enabled = false;
        }
    }

    /// <summary>Smoothly fills an Image toward a target fill amount.</summary>
    public class BarFill : MonoBehaviour
    {
        Image image;
        float target;

        public static void To(Image image, float target)
        {
            if (image == null) return;
            var b = image.GetComponent<BarFill>();
            if (!b) b = image.gameObject.AddComponent<BarFill>();
            b.image = image;
            b.target = Mathf.Clamp01(target);
            b.enabled = true;
        }

        void Update()
        {
            image.fillAmount = Mathf.MoveTowards(image.fillAmount, target, Time.unscaledDeltaTime * 1.5f);
            if (Mathf.Approximately(image.fillAmount, target)) enabled = false;
        }
    }

    /// <summary>Briefly brightens a Graphic (card glow on purchase).</summary>
    public class Flash : MonoBehaviour
    {
        Graphic g;
        Color baseColor, flashColor;
        float t;

        public static void On(Graphic g, Color flashColor)
        {
            if (g == null) return;
            var f = g.GetComponent<Flash>();
            if (f) { f.t = 0f; return; }
            f = g.gameObject.AddComponent<Flash>();
            f.g = g;
            f.baseColor = g.color;
            f.flashColor = flashColor;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime * 2f;
            g.color = Color.Lerp(flashColor, baseColor, Mathf.Clamp01(t));
            if (t >= 1f) { g.color = baseColor; Destroy(this); }
        }
    }

    /// <summary>Floating text that rises and fades (purchase gains, rewards).</summary>
    public class FloatUp : MonoBehaviour
    {
        float t;
        Vector2 start;
        Text text;

        public static void Spawn(Transform parent, string message, Vector2 anchoredPos, Color color, int size = 44)
        {
            var txt = UIKit.Text(parent, message, size, new Vector2(0.5f, 0.5f), anchoredPos, TextAnchor.MiddleCenter, true, 900f);
            txt.color = color;
            var f = txt.gameObject.AddComponent<FloatUp>();
            f.text = txt;
            f.start = anchoredPos;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            ((RectTransform)transform).anchoredPosition = start + Vector2.up * (t * 120f);
            UIKit.SetAlpha(text, Mathf.Clamp01(1.4f - t));
            transform.localScale = Vector3.one * (t < 0.12f ? Mathf.Lerp(1.5f, 1f, t / 0.12f) : 1f);
            if (t > 1.4f) Destroy(gameObject);
        }
    }

    /// <summary>Gentle pulsing glow/scale to draw the eye (e.g. "UPGRADE AVAILABLE!").</summary>
    public class AttentionPulse : MonoBehaviour
    {
        public float speed = 4f, amount = 0.06f;
        Vector3 baseScale;
        void Awake() => baseScale = transform.localScale;
        void Update() => transform.localScale = baseScale * (1f + Mathf.Sin(Time.unscaledTime * speed) * amount);
    }
}
