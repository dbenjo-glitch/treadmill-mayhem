using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>
    /// Keeps a RectTransform inside the device safe area (notches, rounded corners, home indicator).
    /// With <see cref="bleed"/>, the object's own background is redrawn edge-to-edge by a child so dark
    /// overlays still cover the whole screen while their content stays safe.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        public bool bleed;

        RectTransform rt, bleedRt;
        Image source, bleedImage;
        Rect lastArea;
        Vector2Int lastSize;

        public static SafeArea Add(GameObject go, bool bleed = false)
        {
            var sa = go.AddComponent<SafeArea>();
            sa.bleed = bleed;
            sa.Setup();
            return sa;
        }

        void Setup()
        {
            rt = (RectTransform)transform;
            if (bleed && (source = GetComponent<Image>()) != null)
            {
                var go = new GameObject("Bleed", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                go.transform.SetAsFirstSibling();
                bleedRt = (RectTransform)go.transform;
                bleedImage = go.GetComponent<Image>();
                bleedImage.raycastTarget = source.raycastTarget;
                bleedImage.color = source.color;
                source.canvasRenderer.SetAlpha(0f);
            }
            Apply();
        }

        void OnEnable()
        {
            if (rt != null) Apply();
        }

        void LateUpdate()
        {
            if (rt == null) return;
            if (Screen.safeArea != lastArea || Screen.width != lastSize.x || Screen.height != lastSize.y) Apply();
            if (bleedImage != null)
            {
                // Mirror the original background (screens fade or recolour it) and keep the original hidden.
                if (bleedImage.color != source.color) bleedImage.color = source.color;
                source.canvasRenderer.SetAlpha(0f);
            }
        }

        void Apply()
        {
            lastArea = Screen.safeArea;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = lastArea.position, max = lastArea.position + lastArea.size;
            min.x /= Screen.width; max.x /= Screen.width;
            min.y /= Screen.height; max.y /= Screen.height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            if (bleedRt != null)
            {
                // Anchors relative to this (safe) rect that reach the real screen edges.
                float w = Mathf.Max(0.0001f, max.x - min.x), h = Mathf.Max(0.0001f, max.y - min.y);
                bleedRt.anchorMin = new Vector2(-min.x / w, -min.y / h);
                bleedRt.anchorMax = new Vector2(1f + (1f - max.x) / w, 1f + (1f - max.y) / h);
                bleedRt.offsetMin = bleedRt.offsetMax = Vector2.zero;
            }
        }
    }
}
