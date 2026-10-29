using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Huge inverted sphere with a vertical colour gradient: a Ketchapp-style pastel sky.</summary>
    [ExecuteAlways]
    public class SkyDome : MonoBehaviour
    {
        public Color top = new Color(0.45f, 0.62f, 1f);
        public Color horizon = new Color(1f, 0.82f, 0.9f);
        public Color bottom = new Color(0.78f, 0.74f, 1f);
        public float radius = 400f;

        void OnEnable() => Build();

        void Build()
        {
            var mf = GetComponent<MeshFilter>();
            if (!mf) mf = gameObject.AddComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            if (!mr) mr = gameObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            const int lon = 32, lat = 24;
            var verts = new Vector3[(lon + 1) * (lat + 1)];
            var uvs = new Vector2[verts.Length];
            for (int y = 0; y <= lat; y++)
            for (int x = 0; x <= lon; x++)
            {
                float v = (float)y / lat;
                float theta = Mathf.Lerp(-Mathf.PI / 2f, Mathf.PI / 2f, v);
                float phi = (float)x / lon * Mathf.PI * 2f;
                verts[y * (lon + 1) + x] = new Vector3(Mathf.Cos(theta) * Mathf.Cos(phi), Mathf.Sin(theta), Mathf.Cos(theta) * Mathf.Sin(phi)) * radius;
                uvs[y * (lon + 1) + x] = new Vector2((float)x / lon, v);
            }
            var tris = new int[lon * lat * 6];
            int t = 0;
            for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int a = y * (lon + 1) + x, b = a + lon + 1;
                // Wound inward so the inside of the sphere is visible.
                tris[t++] = a; tris[t++] = a + 1; tris[t++] = b;
                tris[t++] = a + 1; tris[t++] = b + 1; tris[t++] = b;
            }
            var mesh = new Mesh { name = "SkyDome", hideFlags = HideFlags.DontSave, vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            var tex = new Texture2D(2, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int i = 0; i < 256; i++)
            {
                float v = i / 255f;
                var c = v < 0.5f
                    ? Color.Lerp(bottom, horizon, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.5f, v)))
                    : Color.Lerp(horizon, top, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.85f, v)));
                tex.SetPixel(0, i, c);
                tex.SetPixel(1, i, c);
            }
            tex.Apply();

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { hideFlags = HideFlags.DontSave };
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mr.sharedMaterial = mat;
        }
    }
}
