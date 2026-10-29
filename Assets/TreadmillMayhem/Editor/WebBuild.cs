using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TreadmillMayhem.EditorTools
{
    /// <summary>
    /// Builds a download-sized player without touching the editor's original look.
    /// The .glb models embed 4096px uncompressed textures (~3 GB build), so for the build only this
    /// swaps every model material for a copy whose textures are the same 4096px images re-saved as PNGs that
    /// Unity can compress, builds, then restores the prefabs and scene.
    /// </summary>
    public static class WebBuild
    {
        const string ModelDir = "Assets/TreadmillMayhem/Models";
        const string OutDir = "Assets/TreadmillMayhem/Models/WebTextures";
        const string ScenePath = "Assets/TreadmillMayhem/TreadmillMayhem.unity";
        const string LogPath = "Builds/webbuild_log.txt";
        const int FullSize = 4096; // keep Tripo's full resolution; only the storage format changes

        [MenuItem("Tools/Treadmill Mayhem/Build WebGL (web textures)")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Builds/WebGL");

        [MenuItem("Tools/Treadmill Mayhem/Build Windows (web textures)")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/TreadmillMayhem.exe");

        public static void Build(BuildTarget target, string location)
        {
            Directory.CreateDirectory("Builds");
            File.WriteAllText(LogPath, "started " + target + "\n");
            EditorSceneManager.SaveOpenScenes();

            var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/TreadmillMayhem" }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            var racerPaths = AssetDatabase.FindAssets("t:RacerDefinition").Select(AssetDatabase.GUIDToAssetPath).ToList();
            var backups = prefabPaths.Concat(racerPaths).Append(ScenePath).ToDictionary(p => p, File.ReadAllBytes);
            try
            {
                var map = MakeWebMaterials();
                var meshes = MakeWebMeshes();
                // visualModel is only read by the editor scene builder, but it would drag the .glb into the build.
                foreach (var rp in racerPaths)
                {
                    var def = AssetDatabase.LoadAssetAtPath<RacerDefinition>(rp);
                    def.visualModel = null;
                    EditorUtility.SetDirty(def);
                }
                foreach (var p in prefabPaths)
                {
                    var root = PrefabUtility.LoadPrefabContents(p);
                    if (Remap(root, map) | RemapMeshes(root, meshes)) PrefabUtility.SaveAsPrefabAsset(root, p);
                    PrefabUtility.UnloadPrefabContents(root);
                }
                var scene = EditorSceneManager.OpenScene(ScenePath);
                bool changed = false;
                foreach (var go in scene.GetRootGameObjects()) changed |= Remap(go, map) | RemapMeshes(go, meshes);
                if (changed) EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                var deps = AssetDatabase.GetDependencies(ScenePath, true);
                foreach (var d in deps.Where(d => d.EndsWith(".glb")))
                    File.AppendAllText(LogPath, "WARNING build still depends on " + d + "\n");

                if (target == BuildTarget.WebGL)
                {
                    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                    PlayerSettings.WebGL.decompressionFallback = true; // itch.io / GitHub Pages can't set Content-Encoding
                    PlayerSettings.WebGL.dataCaching = true;
                }
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = location,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                });
                var s = report.summary;
                var errors = report.steps.SelectMany(st => st.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content).Take(10);
                File.AppendAllText(LogPath, $"result {s.result} size {s.totalSize / 1048576} MB time {s.totalTime}\n" + string.Join("\n", errors) + "\n");
            }
            catch (System.Exception e)
            {
                File.AppendAllText(LogPath, "exception " + e + "\n");
            }
            finally
            {
                foreach (var kv in backups) File.WriteAllBytes(kv.Key, kv.Value);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                EditorSceneManager.OpenScene(ScenePath);
                File.AppendAllText(LogPath, "restored originals\nDONE\n");
            }
        }

        /// <summary>Creates (or reuses) the slim material copies and returns original → slim.</summary>
        public static Dictionary<Material, Material> MakeWebMaterials()
        {
            Directory.CreateDirectory(OutDir);
            var map = new Dictionary<Material, Material>();
            var pending = new List<(Material mat, string prop, string png, bool srgb)>();
            foreach (var file in Directory.GetFiles(ModelDir, "*.glb"))
            {
                string path = file.Replace('\\', '/');
                string model = Path.GetFileNameWithoutExtension(path);
                int i = 0;
                foreach (var src in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                {
                    string matPath = $"{OutDir}/{model}_{i}.mat";
                    var copy = new Material(src) { name = model + "_" + i };
                    foreach (var prop in src.GetTexturePropertyNames())
                    {
                        if (!(src.GetTexture(prop) is Texture2D tex)) continue;
                        string p = prop.ToLower();
                        bool srgb = p.Contains("basecolor") || p.Contains("maintex") || p.Contains("emissi") || p == "_basemap";
                        string png = $"{OutDir}/{model}_{i}{prop}.png";
                        if (!File.Exists(png)) Export(tex, png, FullSize, srgb);
                        pending.Add((copy, prop, png, srgb));
                    }
                    AssetDatabase.DeleteAsset(matPath);
                    AssetDatabase.CreateAsset(copy, matPath);
                    map[src] = copy;
                    i++;
                }
            }
            AssetDatabase.Refresh();
            foreach (var (mat, prop, png, srgb) in pending)
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(png);
                if (imp.maxTextureSize != FullSize || imp.sRGBTexture != srgb || imp.textureCompression != TextureImporterCompression.Compressed)
                {
                    // Default type even for normal maps: glTFast's shader expects plain RGB normals, as the .glb gives it.
                    imp.textureType = TextureImporterType.Default;
                    imp.sRGBTexture = srgb;
                    imp.maxTextureSize = FullSize;
                    imp.textureCompression = TextureImporterCompression.Compressed;
                    imp.SaveAndReimport();
                }
                mat.SetTexture(prop, AssetDatabase.LoadAssetAtPath<Texture2D>(png));
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            return map;
        }

        /// <summary>
        /// Copies every mesh out of the .glb files. Unity packs a referenced .glb whole, embedded
        /// uncompressed textures included, so the build must not reference the .glb at all.
        /// </summary>
        static Dictionary<Mesh, Mesh> MakeWebMeshes()
        {
            string dir = OutDir + "/Meshes";
            Directory.CreateDirectory(dir);
            var map = new Dictionary<Mesh, Mesh>();
            foreach (var file in Directory.GetFiles(ModelDir, "*.glb"))
            {
                string path = file.Replace('\\', '/');
                string model = Path.GetFileNameWithoutExtension(path);
                int i = 0;
                foreach (var src in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
                {
                    string meshPath = $"{dir}/{model}_{i++}.asset";
                    var copy = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (copy == null)
                    {
                        copy = Object.Instantiate(src);
                        copy.name = src.name;
                        AssetDatabase.CreateAsset(copy, meshPath);
                    }
                    map[src] = copy;
                }
            }
            AssetDatabase.SaveAssets();
            return map;
        }

        static bool RemapMeshes(GameObject root, Dictionary<Mesh, Mesh> map)
        {
            bool changed = false;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null && map.TryGetValue(mf.sharedMesh, out var m)) { mf.sharedMesh = m; changed = true; }
            foreach (var mc in root.GetComponentsInChildren<MeshCollider>(true))
                if (mc.sharedMesh != null && map.TryGetValue(mc.sharedMesh, out var m)) { mc.sharedMesh = m; changed = true; }
            foreach (var sk in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (sk.sharedMesh != null && map.TryGetValue(sk.sharedMesh, out var m)) { sk.sharedMesh = m; changed = true; }
            return changed;
        }

        static bool Remap(GameObject root, Dictionary<Material, Material> map)
        {
            bool changed = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                bool any = false;
                for (int k = 0; k < mats.Length; k++)
                    if (mats[k] != null && map.TryGetValue(mats[k], out var slim)) { mats[k] = slim; any = true; }
                if (any) { r.sharedMaterials = mats; changed = true; }
            }
            return changed;
        }

        static void Export(Texture2D tex, string png, int maxSize, bool srgb)
        {
            int w = Mathf.Min(maxSize, tex.width);
            int h = Mathf.Max(1, w * tex.height / tex.width);
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var read = new Texture2D(w, h, TextureFormat.RGBA32, false, !srgb);
            read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            read.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(png, read.EncodeToPNG());
            Object.DestroyImmediate(read);
        }
    }
}
