using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TreadmillMayhem.EditorTools
{
    /// <summary>
    /// One-click App Store / Google Play preparation: identity, versioning, orientation, iOS and Android
    /// player settings, and an app icon rendered from the tuna can model.
    /// Run from Tools ▸ Treadmill Mayhem ▸ Apply Store Settings. Safe to re-run.
    /// </summary>
    public static class StoreSetup
    {
        // Change these to your own before submitting: the bundle id must be registered to your Apple Developer account.
        public const string Company = "Tuna Can Studios";
        public const string Product = "Treadmill Mayhem";
        public const string BundleId = "com.tunacanstudios.treadmillmayhem";
        public const string Version = "1.0.0";
        const string IconPath = "Assets/TreadmillMayhem/Branding/AppIcon.png";

        [MenuItem("Tools/Treadmill Mayhem/Apply Store Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);

            // Landscape only, either way up.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.runInBackground = false;

            // iOS
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.hideHomeButton = false;
            PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);

            // Android
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            ApplyIcon();
            AssetDatabase.SaveAssets();
            Debug.Log("Treadmill Mayhem: store settings applied (" + BundleId + " v" + Version + ").");
        }

        static void ApplyIcon()
        {
            if (!File.Exists(IconPath)) RenderIcon();
            AssetDatabase.ImportAsset(IconPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        /// <summary>Renders the tuna can on a sunny gradient into a 1024x1024 opaque PNG (App Store requires no alpha).</summary>
        [MenuItem("Tools/Treadmill Mayhem/Render App Icon")]
        public static void RenderIcon()
        {
            const int size = 1024;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TreadmillMayhem/Prefabs/Tuna.prefab");
            if (prefab == null)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab Tuna", new[] { "Assets/TreadmillMayhem/Prefabs" }))
                {
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (prefab != null) break;
                }
            }

            var stageRoot = new GameObject("IconStage") { hideFlags = HideFlags.HideAndDontSave };
            var stagePos = new Vector3(0f, -500f, 0f);
            stageRoot.transform.position = stagePos;
            try
            {
                // Background card: warm yellow-to-orange, matching the in-game palette.
                var bgTex = new Texture2D(2, 2) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                bgTex.SetPixels(new[] { new Color(1f, 0.55f, 0.3f), new Color(1f, 0.55f, 0.3f), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.85f, 0.3f) });
                bgTex.Apply();
                var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
                bg.transform.SetParent(stageRoot.transform, false);
                bg.transform.localPosition = new Vector3(0f, 0f, 6f);
                bg.transform.localScale = new Vector3(14f, 14f, 1f);
                var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { mainTexture = bgTex };
                bgMat.SetTexture("_BaseMap", bgTex);
                bg.GetComponent<Renderer>().sharedMaterial = bgMat;

                GameObject can = null;
                if (prefab != null)
                {
                    can = (GameObject)Object.Instantiate(prefab, stageRoot.transform);
                    foreach (var mb in can.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
                    foreach (var rb in can.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
                    can.transform.localPosition = Vector3.zero;
                    can.transform.localRotation = Quaternion.Euler(-12f, 205f, 14f);
                    var b = new Bounds(can.transform.position, Vector3.zero);
                    foreach (var r in can.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                    float fit = 5.2f / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.y));
                    can.transform.localScale *= fit;
                    b = new Bounds(can.transform.position, Vector3.zero);
                    foreach (var r in can.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                    can.transform.position += stagePos - b.center;
                }

                var lightGo = new GameObject("IconLight");
                lightGo.transform.SetParent(stageRoot.transform, false);
                lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.6f;

                var camGo = new GameObject("IconCam");
                camGo.transform.SetParent(stageRoot.transform, false);
                camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 3.6f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(1f, 0.75f, 0.3f);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 30f;

                var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);

                Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
                File.WriteAllBytes(IconPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(IconPath);
            }
            finally
            {
                Object.DestroyImmediate(stageRoot);
            }
        }
    }
}
