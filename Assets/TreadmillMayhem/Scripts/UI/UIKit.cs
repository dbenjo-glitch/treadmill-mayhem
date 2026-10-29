using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>Shared builders and palette for all code-built UI, so every screen looks like one game.</summary>
    public static class UIKit
    {
        public static readonly Color Panel = new Color(0.11f, 0.13f, 0.2f, 0.94f);
        public static readonly Color PanelLight = new Color(0.18f, 0.2f, 0.3f, 0.95f);
        public static readonly Color Track = new Color(0.22f, 0.25f, 0.34f);
        public static readonly Color Muted = new Color(0.62f, 0.66f, 0.75f);
        public static readonly Color Cyan = new Color(0.3f, 0.8f, 1f);
        public static readonly Color Purple = new Color(0.62f, 0.45f, 1f);
        public static readonly Color Gold = new Color(1f, 0.8f, 0.2f);
        public static readonly Color Green = new Color(0.3f, 0.85f, 0.5f);
        public static readonly Color Red = new Color(1f, 0.35f, 0.35f);
        public static readonly Color Orange = new Color(1f, 0.55f, 0.15f);
        public static readonly Color Ink = new Color(0.08f, 0.08f, 0.15f);
        public static readonly Color GearColor = new Color(0.55f, 0.9f, 1f);

        static Font font;
        static Sprite rounded, circle;

        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeRoundedSprite();
        public static Sprite Circle => circle != null ? circle : circle = MakeCircleSprite();

        public static RectTransform Rect(GameObject go) => (RectTransform)go.transform;

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        public static GameObject Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject Box(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = Rect(go);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x == 0f ? 0f : anchor.x == 1f ? 1f : 0.5f, anchor.y == 1f ? 1f : anchor.y == 0f ? 0f : 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = raycast;
            return go;
        }

        /// <summary>Full-screen dimmed backdrop that swallows clicks behind a modal.</summary>
        public static GameObject Backdrop(Transform parent, string name, float alpha = 0.7f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(Rect(go));
            var img = go.GetComponent<Image>();
            img.color = new Color(0.05f, 0.04f, 0.14f, alpha);
            img.raycastTarget = true;
            SafeArea.Add(go, true);
            return go;
        }

        public static Text Text(Transform parent, string text, int size, Vector2 anchor, Vector2 pos, TextAnchor align, bool outlined = true, float width = 1600f)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = Rect(go);
            rt.anchorMin = rt.anchorMax = anchor;
            float px = align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft ? 0f
                     : align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight ? 1f : 0.5f;
            float py = align == TextAnchor.UpperLeft || align == TextAnchor.UpperCenter || align == TextAnchor.UpperRight ? 1f
                     : align == TextAnchor.LowerLeft || align == TextAnchor.LowerCenter || align == TextAnchor.LowerRight ? 0f : 0.5f;
            rt.pivot = new Vector2(px, py);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, size * 1.4f);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.font = Font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = Color.white;
            t.text = text;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outlined)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = Ink;
                outline.effectDistance = new Vector2(3f, -3f);
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
                shadow.effectDistance = new Vector2(0f, -6f);
            }
            return t;
        }

        public static Button Button(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, UnityAction onClick, out UnityEngine.UI.Text labelText, int fontSize = 40)
        {
            var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = Rect(go);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -8f);
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.75f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            go.AddComponent<PressBounce>();
            labelText = Text(go.transform, label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, true);
            Rect(labelText.gameObject).sizeDelta = size;
            return btn;
        }

        public static Button Button(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color, UnityAction onClick, int fontSize = 40)
            => Button(parent, label, anchor, pos, size, color, onClick, out _, fontSize);

        /// <summary>Horizontal progress bar; returns the fill image (set fillAmount 0..1).</summary>
        public static Image Bar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fillColor)
        {
            var track = Box(parent, "Track", anchor, pos, size, Track);
            var fillGo = Node(track.transform, "Fill");
            Stretch(Rect(fillGo));
            var fill = fillGo.AddComponent<Image>();
            fill.sprite = Rounded;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = fillColor;
            fill.raycastTarget = false;
            return fill;
        }

        public static Image Disc(Transform parent, Vector2 anchor, Vector2 pos, float size, Color color)
        {
            var go = Node(parent, "Disc");
            var rt = Rect(go);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.sprite = Circle;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A rounded "pill" showing a currency balance, e.g. the coin counter in menus.</summary>
        public static UnityEngine.UI.Text CurrencyPill(Transform parent, Vector2 anchor, Vector2 pos, Currency currency, out RectTransform pill)
        {
            var go = Box(parent, currency + "Pill", anchor, pos, new Vector2(240f, 66f), new Color(0f, 0f, 0f, 0.45f));
            pill = Rect(go);
            var c = currency == Currency.Coins ? Gold : GearColor;
            Disc(go.transform, new Vector2(0f, 0.5f), new Vector2(38f, 0f), 50f, c);
            Text(go.transform, currency == Currency.Coins ? "$" : "G", 30, new Vector2(0f, 0.5f), new Vector2(38f, 0f), TextAnchor.MiddleCenter, false, 50f).color = Ink;
            var t = Text(go.transform, "0", 38, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), TextAnchor.MiddleRight, true, 180f);
            t.color = c;
            return t;
        }

        public static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        public static InputField InputField(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("NameField", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.InputField));
            go.transform.SetParent(parent, false);
            var rt = Rect(go);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            UnityEngine.UI.Text Inner(string name, Color c, FontStyle style)
            {
                var tgo = Node(go.transform, name);
                var trt = Rect(tgo);
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(20f, 6f);
                trt.offsetMax = new Vector2(-20f, -6f);
                var t = tgo.AddComponent<UnityEngine.UI.Text>();
                t.font = Font;
                t.fontSize = 36;
                t.fontStyle = style;
                t.color = c;
                t.alignment = TextAnchor.MiddleCenter;
                t.supportRichText = false;
                return t;
            }

            var field = go.GetComponent<UnityEngine.UI.InputField>();
            field.textComponent = Inner("Text", Ink, FontStyle.Bold);
            var ph = Inner("Placeholder", new Color(0.5f, 0.5f, 0.6f), FontStyle.Italic);
            ph.text = "Enter name...";
            field.placeholder = ph;
            field.characterLimit = 14;
            field.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            return field;
        }

        static Sprite MakeRoundedSprite()
        {
            const int n = 64, r = 24;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(r - x, x - (n - 1 - r)));
                float dy = Mathf.Max(0f, Mathf.Max(r - y, y - (n - 1 - r)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f)));
            }
            tex.Apply();
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        static Sprite MakeCircleSprite()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                byte a = (byte)(Mathf.Clamp01((1f - d) * 30f) * 255);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
