using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TreadmillMayhem.EditorTools
{
    /// <summary>
    /// Builds the Treadmill Mayhem scene and racer prefabs from the Tripo models.
    /// Re-run from Tools ▸ Treadmill Mayhem ▸ Build Scene after changing models or tuning values.
    /// </summary>
    public static class TreadmillMayhemSetup
    {
        const string Root = "Assets/TreadmillMayhem";
        const string ModelDir = Root + "/Models";
        const string PrefabDir = Root + "/Prefabs";
        const string ScenePath = Root + "/TreadmillMayhem.unity";

        // Treadmill model is ~1 unit long along +X with the console at +X; we turn it so the console faces +Z.
        // Height keeps the original scale; length and width are stretched 1.5x for more room on the belt.
        const float TreadmillScale = 14f;
        const float FootprintStretch = 1.5f;
        // Belt surface measured from the model (model units, before scaling).
        const float BeltTop = 0.164f;
        const float BeltRearX = -0.47f;
        const float BeltFrontX = 0.26f;
        const float BeltHalfWidth = 0.165f;

        const string ConfigDir = Root + "/Config";

        /// <summary>Default physics profiles from the design spec. Only used to create missing definition assets.</summary>
        struct RacerSeed
        {
            public string name, model;
            public bool player;
            public RacerShape shape;
            public float size, mass, friction, bounce, accel, steer, maxRel, stability, wobble, instability, bump, knock;
            public Color color;
            public float laneBias, aggressionBias, greedBias;
            public Vector3 visualRotation;
        }

        // Model alignment measured from the meshes: long axis onto X, lying flat (fish: head toward +Z).
        static readonly Vector3 RollingPinRotation = (Quaternion.Euler(0f, 0f, 28f) * Quaternion.Euler(0f, 50f, 0f)).eulerAngles;
        static readonly Vector3 WaterBottleRotation = (Quaternion.Euler(0f, 0f, 5.5f) * Quaternion.Euler(0f, 45.5f, 0f)).eulerAngles;

        static readonly RacerSeed[] Racers =
        {
            // Tuna: the all-rounder.
            new RacerSeed { name = "Tuna", model = "tuna", player = true, shape = RacerShape.Can, size = 1.15f, mass = 1f, friction = 0.6f, bounce = 0.25f, accel = 24f, steer = 18f, maxRel = 7f, stability = 0.7f, wobble = 1f, bump = 4.5f, knock = 1f, color = new Color(0.15f, 0.45f, 1f) },
            // Orange: medium weight, steady roll, medium bounce.
            new RacerSeed { name = "Orange", model = "orange", shape = RacerShape.Ball, size = 0.95f, mass = 0.9f, friction = 0.6f, bounce = 0.4f, accel = 21f, steer = 18f, maxRel = 6.5f, stability = 0.6f, wobble = 0.8f, bump = 4f, knock = 1f, color = new Color(1f, 0.55f, 0.1f), laneBias = -0.6f, greedBias = 0.05f },
            // Apple: lumpy, slightly unstable roll.
            new RacerSeed { name = "Apple", model = "apple", shape = RacerShape.Ball, size = 0.95f, mass = 0.85f, friction = 0.65f, bounce = 0.3f, accel = 20f, steer = 17f, maxRel = 6.5f, stability = 0.5f, wobble = 1.1f, instability = 0.35f, bump = 4f, knock = 1.05f, color = new Color(0.95f, 0.15f, 0.2f), laneBias = 0.6f },
            // Beans: big, heavy, slow to get going, hits hard.
            new RacerSeed { name = "Beans", model = "beans", shape = RacerShape.Can, size = 1.2f, mass = 1.45f, friction = 0.7f, bounce = 0.15f, accel = 17f, steer = 14f, maxRel = 6f, stability = 0.9f, wobble = 0.6f, bump = 6f, knock = 0.7f, color = new Color(0.95f, 0.5f, 0.2f), laneBias = -0.2f, aggressionBias = 0.1f },
            // Bouncy ball: light, elastic, fast and erratic.
            new RacerSeed { name = "Ball", model = "ball", shape = RacerShape.Ball, size = 0.95f, mass = 0.5f, friction = 0.5f, bounce = 0.8f, accel = 24f, steer = 22f, maxRel = 7.5f, stability = 0.3f, wobble = 1f, instability = 0.15f, bump = 3.5f, knock = 1.3f, color = new Color(1f, 0.85f, 0.2f), laneBias = 0.3f, greedBias = 0.1f },
            // Cola: long, narrow, fast roller that wobbles under side hits.
            new RacerSeed { name = "Cola", model = "cola", shape = RacerShape.Can, size = 1.1f, mass = 0.75f, friction = 0.5f, bounce = 0.3f, accel = 26f, steer = 16f, maxRel = 7.5f, stability = 0.4f, wobble = 1.5f, bump = 4f, knock = 1.1f, color = new Color(0.85f, 0.1f, 0.15f), laneBias = 0.7f, aggressionBias = 0.1f },
            // Fish: slippery and agile, skids and flops instead of rolling.
            new RacerSeed { name = "Fish", model = "fish", shape = RacerShape.Slider, size = 1.2f, mass = 0.7f, friction = 0.25f, bounce = 0.35f, accel = 22f, steer = 24f, maxRel = 7f, stability = 0.6f, wobble = 1.3f, instability = 0.5f, bump = 3.5f, knock = 1.2f, color = new Color(0.35f, 0.55f, 0.95f), laneBias = -0.4f, greedBias = 0.1f, visualRotation = new Vector3(0f, 74f, 0f) },
            // Bread can: grippy, sturdy, steady.
            new RacerSeed { name = "Bread Can", model = "breadcan", shape = RacerShape.Can, size = 1.15f, mass = 1.2f, friction = 0.8f, bounce = 0.15f, accel = 20f, steer = 16f, maxRel = 6.5f, stability = 0.85f, wobble = 0.8f, bump = 5f, knock = 0.85f, color = new Color(1f, 0.75f, 0.25f), laneBias = 0.4f },
            // Rolling pin: long, fast roller that barely turns but bulldozes everything.
            new RacerSeed { name = "Rolling Pin", model = "rollingpin", shape = RacerShape.Can, size = 1f, mass = 1.6f, friction = 0.6f, bounce = 0.2f, accel = 25f, steer = 10f, maxRel = 8f, stability = 0.95f, wobble = 0.5f, bump = 6.5f, knock = 0.6f, color = new Color(0.85f, 0.6f, 0.95f), laneBias = 0f, aggressionBias = 0.15f, visualRotation = RollingPinRotation },
            // Water bottle: light, tippy and sloshy.
            // Onion: lumpy ball with a sprout; rolls a little off-line, medium weight.
            new RacerSeed { name = "Onion", model = "onion", shape = RacerShape.Ball, size = 0.95f, mass = 0.8f, friction = 0.65f, bounce = 0.25f, accel = 21f, steer = 19f, maxRel = 6.8f, stability = 0.55f, wobble = 1f, instability = 0.3f, bump = 4f, knock = 1.05f, color = new Color(0.95f, 0.75f, 0.3f), laneBias = -0.1f, greedBias = 0.05f },
            // Paper towel: light, grippy and soft - absorbs bumps but gets shoved around.
            new RacerSeed { name = "Paper Towel", model = "papertowel", shape = RacerShape.Can, size = 1.05f, mass = 0.55f, friction = 0.75f, bounce = 0.1f, accel = 22f, steer = 17f, maxRel = 6.8f, stability = 0.6f, wobble = 1.1f, bump = 3.5f, knock = 1.25f, color = new Color(0.95f, 0.95f, 0.97f), laneBias = 0.2f },
            new RacerSeed { name = "Water Bottle", model = "waterbottle", shape = RacerShape.Can, size = 1f, mass = 0.45f, friction = 0.5f, bounce = 0.45f, accel = 21f, steer = 18f, maxRel = 7f, stability = 0.35f, wobble = 1.6f, instability = 0.25f, bump = 3f, knock = 1.35f, color = new Color(0.4f, 0.8f, 1f), laneBias = -0.7f, visualRotation = WaterBottleRotation },
        };

        [MenuItem("Tools/Treadmill Mayhem/Build Scene")]
        public static void BuildScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            System.IO.Directory.CreateDirectory(PrefabDir);

            var config = EnsureGameConfig();
            var difficulties = EnsureDifficulties();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildEnvironment();
            var treadmill = BuildTreadmill();
            PlaceStartButton(GameObject.Find("Treadmill/Model"));
            BuildRacers(treadmill);
            BuildCamera(treadmill);
            BuildCrowd();
            BuildShopDecor(treadmill);

            var gmObj = new GameObject("GameManager").AddComponent<GameManager>();
            gmObj.config = config;
            gmObj.difficulties = difficulties;
            gmObj.playerPrefab = playerPrefab;
            gmObj.opponentPool = opponentPrefabs.ToArray();
            gmObj.spawnPoints = lineupSlots;
            new GameObject("GameUI", typeof(RectTransform)).AddComponent<GameUI>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild();
            Debug.Log("Treadmill Mayhem scene built at " + ScenePath);
        }

        static void BuildEnvironment()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.6f;
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.9f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.9f, 0.8f, 0.85f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.5f, 0.6f);
            var horizon = new Color(1f, 0.84f, 0.9f);
            // No fog: it would wash out the gradient sky dome, which already gives the depth cue.
            RenderSettings.fog = false;

            var env = new GameObject("Environment").transform;

            var sky = new GameObject("SkyDome").AddComponent<TreadmillMayhem.SkyDome>();
            sky.transform.SetParent(env);
            sky.horizon = horizon;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(env);
            ground.transform.position = new Vector3(0f, -0.45f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            ground.GetComponent<Renderer>().sharedMaterial = SaveMat("Ground", new Color(0.78f, 0.74f, 1f), 0.05f);

            // Round stage the treadmill sits on, with a candy-pink rim.
            Prim(PrimitiveType.Cylinder, env, "StageRim", new Vector3(0f, -0.3f, -2f), new Vector3(37f, 0.15f, 37f), SaveMat("StageRim", new Color(1f, 0.55f, 0.72f), 0.3f));
            Prim(PrimitiveType.Cylinder, env, "Stage", new Vector3(0f, -0.2f, -2f), new Vector3(36f, 0.2f, 36f), SaveMat("Stage", new Color(1f, 0.9f, 0.8f), 0.35f));

            // Crash mat behind the belt: where losers land.
            var mat = Prim(PrimitiveType.Cube, env, "CrashMat", new Vector3(0f, 0.25f, -14.5f), new Vector3(10.5f, 0.5f, 6f), SaveMat("CrashMat", new Color(0.3f, 0.7f, 1f), 0.15f));
            Prim(PrimitiveType.Cube, env, "CrashMatStripe", new Vector3(0f, 0.505f, -14.5f), new Vector3(10.6f, 0.02f, 0.7f), SaveMat("White", Color.white, 0.2f));
            mat.GetComponent<Collider>().sharedMaterial = SavePhysMat("CrashMat", 0.9f, 0.5f);

            var pastel = new[]
            {
                new Color(1f, 0.72f, 0.8f), new Color(1f, 0.88f, 0.5f), new Color(0.65f, 0.93f, 0.75f),
                new Color(0.72f, 0.7f, 1f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.7f, 0.55f),
            };
            var rng = new System.Random(7);
            float Rand() => (float)rng.NextDouble();

            // A ring of chunky pillars topped with balls frames the arena.
            var ring = new GameObject("Pillars").transform;
            ring.SetParent(env);
            for (int i = 0; i < 18; i++)
            {
                float ang = i / 18f * Mathf.PI * 2f;
                float r = 29f + Rand() * 5f;
                var p = new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r - 1f);
                float h = 1.5f + Rand() * 4f;
                var col = pastel[i % pastel.Length];
                Prim(PrimitiveType.Cylinder, ring, "Pillar", p + Vector3.up * h * 0.5f, new Vector3(1.6f, h * 0.5f, 1.6f), SaveMat("Pastel" + (i % pastel.Length), col, 0.25f));
                Prim(PrimitiveType.Sphere, ring, "PillarBall", p + Vector3.up * (h + 0.9f), Vector3.one * 1.8f, SaveMat("Pastel" + ((i + 2) % pastel.Length), pastel[(i + 2) % pastel.Length], 0.5f));
            }

            // Floating, slowly spinning shapes in the sky.
            var floaters = new GameObject("Floaters").transform;
            floaters.SetParent(env);
            for (int i = 0; i < 12; i++)
            {
                float ang = Rand() * Mathf.PI * 2f;
                float r = 28f + Rand() * 25f;
                var p = new Vector3(Mathf.Cos(ang) * r, 8f + Rand() * 14f, Mathf.Sin(ang) * r + 10f);
                var shape = i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube;
                var f = Prim(shape, floaters, "Floater", p, Vector3.one * (1.5f + Rand() * 2.5f), SaveMat("Pastel" + (i % pastel.Length), pastel[i % pastel.Length], 0.25f));
                f.transform.rotation = Quaternion.Euler(Rand() * 360f, Rand() * 360f, 0f);
                Object.DestroyImmediate(f.GetComponent<Collider>());
                var spin = f.AddComponent<TreadmillMayhem.Spinner>();
                spin.axis = new Vector3(Rand(), 1f, Rand()).normalized;
                spin.degreesPerSecond = 8f + Rand() * 20f;
            }

            // Puffy clouds drifting across the sky.
            var clouds = new GameObject("Clouds").transform;
            clouds.SetParent(env);
            var cloudMat = SaveMat("Cloud", Color.white, 0f);
            for (int i = 0; i < 14; i++)
            {
                var cloud = new GameObject("Cloud").transform;
                cloud.SetParent(clouds);
                cloud.position = new Vector3(-70f + Rand() * 140f, 18f + Rand() * 18f, 35f + Rand() * 60f);
                int puffs = 3 + rng.Next(3);
                for (int k = 0; k < puffs; k++)
                {
                    var puff = Prim(PrimitiveType.Sphere, cloud, "Puff", Vector3.zero, Vector3.one * (3f + Rand() * 3f), cloudMat);
                    puff.transform.localPosition = new Vector3((k - puffs * 0.5f) * 2.4f, Rand() * 1.2f, Rand() * 1.5f);
                    Object.DestroyImmediate(puff.GetComponent<Collider>());
                    puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                cloud.gameObject.AddComponent<TreadmillMayhem.CloudDrift>().speed = 0.4f + Rand() * 0.8f;
            }

            BuildDetails(env);
            BuildPostProcessing(env);
        }

        // ---------------------------------------------------------------------------------------------
        // Background detail: a TV-studio / gym-party set dressed around the stage. Everything here is
        // decor only (no colliders) and sits where the gameplay camera actually looks.
        // Treadmill footprint for reference: x ±5.8, z -10.3..10.3, console up to ~7.5 m around z 4–6.
        // ---------------------------------------------------------------------------------------------

        static void BuildDetails(Transform env)
        {
            var root = new GameObject("Details").transform;
            root.SetParent(env);

            BuildStudioFloor(env);
            BuildStageTrim(root);
            BuildBillboards(root);
            BuildGymProps(root);
            BuildSpeakers(root);
            BuildRingLights(root);
            BuildBalloons(root);
            BuildTruss(root);
            BuildBunting(root);
            BuildTvCameras(root);
            BuildFloorStickers(root);
            BuildBleachers(root);
            BuildTrees(root);
            BuildConfetti(root);

            // Static-batch everything that never moves; leave animated decor (anything under a Bob) dynamic.
            MarkStatic(env);
        }

        static void MarkStatic(Transform t, bool insideAnimated = false)
        {
            bool animated = insideAnimated || t.GetComponent<Bob>() != null || t.GetComponent<Spinner>() != null ||
                            t.GetComponent<CloudDrift>() != null || t.GetComponent<ParticleSystem>() != null || t.GetComponent<SkyDome>() != null ||
                            t.GetComponent<TextMesh>() != null;
            if (!animated) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            foreach (Transform child in t) MarkStatic(child, animated);
        }

        /// <summary>Primitive with no collider, for decor.</summary>
        static GameObject Deco(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Material SaveEmissive(string name, Color c, float intensity)
        {
            var m = SaveMat(name, c, 0.8f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }

        static readonly Color[] Candy =
        {
            new Color(1f, 0.45f, 0.62f), new Color(1f, 0.8f, 0.25f), new Color(0.45f, 0.85f, 0.55f),
            new Color(0.5f, 0.7f, 1f), new Color(0.75f, 0.55f, 1f), new Color(1f, 0.6f, 0.35f),
        };

        /// <summary>Soft two-tone tiled studio floor beyond the stage.</summary>
        static void BuildStudioFloor(Transform env)
        {
            var path = Root + "/Materials/FloorTiles.png";
            if (!System.IO.File.Exists(path))
            {
                const int n = 128;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var a = new Color(0.8f, 0.76f, 1f);
                var b = new Color(0.86f, 0.83f, 1f);
                var grout = new Color(0.72f, 0.68f, 0.92f);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool line = x % 64 < 2 || y % 64 < 2;
                    bool alt = ((x / 64) + (y / 64)) % 2 == 0;
                    tex.SetPixel(x, y, line ? grout : alt ? a : b);
                }
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.SaveAndReimport();
            }
            var ground = env.Find("Ground");
            if (ground == null) return;
            var mat = SaveMat("Ground", Color.white, 0.15f);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            mat.SetTextureScale("_BaseMap", new Vector2(100f, 100f));
            EditorUtility.SetDirty(mat);
            ground.GetComponent<Renderer>().sharedMaterial = mat;
        }

        /// <summary>Painted rings on the stage plus a ring of chasing marquee bulbs around the rim.</summary>
        static void BuildStageTrim(Transform root)
        {
            var trim = new GameObject("StageTrim").transform;
            trim.SetParent(root);
            var c = new Vector3(0f, 0f, -2f);
            // Concentric painted rings (stacked flat discs, each slightly higher and smaller).
            Deco(PrimitiveType.Cylinder, trim, "RingWhite", c + Vector3.up * 0.005f, new Vector3(34.4f, 0.005f, 34.4f), SaveMat("StageRingWhite", Color.white, 0.4f));
            Deco(PrimitiveType.Cylinder, trim, "RingFill", c + Vector3.up * 0.01f, new Vector3(33.4f, 0.005f, 33.4f), SaveMat("StageRingPeach", new Color(1f, 0.72f, 0.6f), 0.25f));
            Deco(PrimitiveType.Cylinder, trim, "RingInnerLine", c + Vector3.up * 0.015f, new Vector3(27f, 0.005f, 27f), SaveMat("StageRingPink", new Color(1f, 0.55f, 0.7f), 0.3f));
            Deco(PrimitiveType.Cylinder, trim, "RingInner", c + Vector3.up * 0.02f, new Vector3(26.2f, 0.005f, 26.2f), SaveMat("StageInner", new Color(1f, 0.82f, 0.68f), 0.25f));
            // Sunburst stripes radiating from under the treadmill, like a TV game-show floor.
            var rayMat = SaveMat("StageRay", new Color(1f, 0.76f, 0.62f), 0.25f);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * 360f;
                var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                Deco(PrimitiveType.Cube, trim, "Ray", c + dir * 8.5f + Vector3.up * 0.03f, new Vector3(1.6f, 0.01f, 9f), rayMat, Quaternion.Euler(0f, a, 0f));
            }

            // Marquee bulbs around the rim.
            var bulbs = new GameObject("MarqueeBulbs").transform;
            bulbs.SetParent(trim);
            var bulbMat = SaveMat("BulbDim", new Color(1f, 0.95f, 0.85f), 0.8f);
            const int count = 64;
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                var p = c + new Vector3(Mathf.Cos(a) * 18.3f, 0.05f, Mathf.Sin(a) * 18.3f);
                Deco(PrimitiveType.Sphere, bulbs, "Bulb", p, Vector3.one * 0.32f, bulbMat).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            bulbs.gameObject.AddComponent<MarqueeLights>();
        }

        /// <summary>
        /// The show's logo, split into two signs flanking the console so it stays visible behind the belt,
        /// plus a blinking ON AIR light.
        /// </summary>
        static void BuildBillboards(Transform root)
        {
            var signs = new GameObject("Billboards").transform;
            signs.SetParent(root);
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var frameMat = SaveMat("SignFrame", new Color(0.18f, 0.16f, 0.32f), 0.5f);
            var faceMat = SaveMat("SignFace", new Color(0.32f, 0.2f, 0.62f), 0.3f);
            var postMat = SaveMat("SignPost", new Color(0.85f, 0.85f, 0.92f), 0.6f);
            var words = new[] { "TREADMILL", "MAYHEM" };
            for (int s = 0; s < 2; s++)
            {
                float x = s == 0 ? -9.5f : 9.5f;
                var sign = new GameObject("Sign_" + words[s]).transform;
                sign.SetParent(signs);
                sign.position = new Vector3(x, 0f, 13f);
                sign.rotation = Quaternion.Euler(0f, s == 0 ? -12f : 12f, 0f);

                foreach (float px in new[] { -3.2f, 3.2f })
                    Deco(PrimitiveType.Cylinder, sign, "Post", new Vector3(px, 1.6f, 0.3f), new Vector3(0.35f, 1.6f, 0.35f), postMat);
                Deco(PrimitiveType.Cube, sign, "Frame", new Vector3(0f, 4.4f, 0f), new Vector3(8.4f, 3f, 0.5f), frameMat);
                Deco(PrimitiveType.Cube, sign, "Face", new Vector3(0f, 4.4f, -0.27f), new Vector3(7.8f, 2.4f, 0.05f), faceMat);

                // Bulb border around each sign.
                var border = new GameObject("Border").transform;
                border.SetParent(sign, false);
                var bulbMat = SaveMat("BulbDim", new Color(1f, 0.95f, 0.85f), 0.8f);
                for (int i = 0; i < 26; i++)
                {
                    float t = i / 26f;
                    Vector3 p;
                    if (t < 0.35f) p = new Vector3(Mathf.Lerp(-4f, 4f, t / 0.35f), 5.85f, -0.3f);
                    else if (t < 0.5f) p = new Vector3(4f, Mathf.Lerp(5.85f, 2.95f, (t - 0.35f) / 0.15f), -0.3f);
                    else if (t < 0.85f) p = new Vector3(Mathf.Lerp(4f, -4f, (t - 0.5f) / 0.35f), 2.95f, -0.3f);
                    else p = new Vector3(-4f, Mathf.Lerp(2.95f, 5.85f, (t - 0.85f) / 0.15f), -0.3f);
                    Deco(PrimitiveType.Sphere, border, "Bulb", p, Vector3.one * 0.22f, bulbMat).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                var marquee = border.gameObject.AddComponent<MarqueeLights>();
                marquee.palette = new[] { new Color(1f, 0.85f, 0.3f) };
                marquee.groupSize = 2;
                marquee.speed = 5f;

                // Word, with a drop shadow for that chunky arcade look.
                for (int layer = 0; layer < 2; layer++)
                {
                    var tgo = new GameObject(layer == 0 ? "TextShadow" : "Text");
                    tgo.transform.SetParent(sign, false);
                    tgo.transform.localPosition = new Vector3(layer == 0 ? 0.08f : 0f, layer == 0 ? 4.32f : 4.4f, layer == 0 ? -0.31f : -0.33f);
                    var tm = tgo.AddComponent<TextMesh>();
                    tm.text = words[s];
                    tm.font = font;
                    tm.fontSize = 120;
                    tm.characterSize = 0.075f;
                    tm.fontStyle = FontStyle.Bold;
                    tm.anchor = TextAnchor.MiddleCenter;
                    tm.alignment = TextAlignment.Center;
                    tm.color = layer == 0 ? new Color(0.1f, 0.05f, 0.2f) : new Color(1f, 0.82f, 0.2f);
                    tgo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }
            }

            // ON AIR light on the right sign.
            var onAir = Deco(PrimitiveType.Cube, signs, "OnAirBox", new Vector3(9.5f + 2.2f, 6.6f, 13f), new Vector3(1.8f, 0.6f, 0.3f), SaveMat("OnAirBox", new Color(0.2f, 0.05f, 0.08f), 0.5f));
            var lamp = Deco(PrimitiveType.Sphere, signs, "OnAirLamp", new Vector3(9.5f + 1.55f, 6.6f, 12.82f), Vector3.one * 0.35f, SaveMat("OnAirLamp", Color.red, 0.8f));
            lamp.AddComponent<Blink>();
            var label = new GameObject("OnAirText");
            label.transform.SetParent(signs, false);
            label.transform.position = new Vector3(9.5f + 2.35f, 6.6f, 12.83f);
            var ltm = label.AddComponent<TextMesh>();
            ltm.text = "ON AIR";
            ltm.font = font;
            ltm.fontSize = 60;
            ltm.characterSize = 0.035f;
            ltm.anchor = TextAnchor.MiddleCenter;
            ltm.color = new Color(1f, 0.4f, 0.4f);
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            onAir.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Gym clutter on the stage either side of the treadmill: yoga balls, dumbbells, kettlebells, mats, bottles.</summary>
        static void BuildGymProps(Transform root)
        {
            var gym = new GameObject("GymProps").transform;
            gym.SetParent(root);
            var rng = new System.Random(21);
            float R() => (float)rng.NextDouble();
            var metal = SaveMat("GymMetal", new Color(0.7f, 0.72f, 0.78f), 0.85f);
            var dark = SaveMat("GymDark", new Color(0.22f, 0.22f, 0.28f), 0.4f);

            for (int side = -1; side <= 1; side += 2)
            {
                // Yoga balls.
                for (int i = 0; i < 2; i++)
                {
                    float d = 1.5f + R() * 0.5f;
                    var p = new Vector3(side * (8f + R() * 4f), d * 0.5f, -8f + i * 9f + R() * 2f);
                    Deco(PrimitiveType.Sphere, gym, "YogaBall", p, Vector3.one * d, SaveMat("Yoga" + i + side, Candy[(i + (side > 0 ? 3 : 0)) % Candy.Length], 0.55f));
                }

                // Stacked exercise mats.
                var matBase = new Vector3(side * 13f, 0f, 3f + R() * 2f);
                for (int i = 0; i < 4; i++)
                    Deco(PrimitiveType.Cube, gym, "Mat", matBase + Vector3.up * (0.12f + i * 0.22f), new Vector3(2.4f, 0.2f, 1.2f), SaveMat("GymMat" + i, Candy[(i + 1) % Candy.Length], 0.2f),
                        Quaternion.Euler(0f, (R() - 0.5f) * 14f, 0f));

                // Dumbbell pairs.
                for (int i = 0; i < 2; i++)
                {
                    var p = new Vector3(side * (7.2f + R() * 1.5f), 0.3f, -3f + i * 1.3f + R());
                    var bell = new GameObject("Dumbbell").transform;
                    bell.SetParent(gym);
                    bell.position = p;
                    bell.rotation = Quaternion.Euler(0f, R() * 180f, 0f);
                    Deco(PrimitiveType.Cylinder, bell, "Bar", Vector3.zero, new Vector3(0.12f, 0.55f, 0.12f), metal, Quaternion.Euler(0f, 0f, 90f));
                    var plate = SaveMat("Plate" + i, Candy[(i * 2 + (side > 0 ? 1 : 0)) % Candy.Length], 0.4f);
                    foreach (float e in new[] { -0.45f, 0.45f })
                        Deco(PrimitiveType.Cylinder, bell, "Weight", new Vector3(e, 0f, 0f), new Vector3(0.6f, 0.12f, 0.6f), plate, Quaternion.Euler(0f, 0f, 90f));
                }

                // Kettlebells.
                for (int i = 0; i < 2; i++)
                {
                    var p = new Vector3(side * (11f + R() * 2f), 0f, -6f + i * 2f);
                    var kb = new GameObject("Kettlebell").transform;
                    kb.SetParent(gym);
                    kb.position = p;
                    var body = SaveMat("Kettle" + i, i == 0 ? dark.color : Candy[4], 0.5f);
                    Deco(PrimitiveType.Sphere, kb, "Body", new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.85f, 0.9f), body);
                    Deco(PrimitiveType.Cylinder, kb, "HandleL", new Vector3(-0.22f, 0.95f, 0f), new Vector3(0.1f, 0.18f, 0.1f), body);
                    Deco(PrimitiveType.Cylinder, kb, "HandleR", new Vector3(0.22f, 0.95f, 0f), new Vector3(0.1f, 0.18f, 0.1f), body);
                    Deco(PrimitiveType.Cylinder, kb, "HandleTop", new Vector3(0f, 1.12f, 0f), new Vector3(0.1f, 0.24f, 0.1f), body, Quaternion.Euler(0f, 0f, 90f));
                }

                // Water bottles and a towel.
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3(side * (6.8f + i * 0.35f), 0f, 8f + R());
                    Deco(PrimitiveType.Cylinder, gym, "Bottle", p + Vector3.up * 0.35f, new Vector3(0.28f, 0.35f, 0.28f), SaveMat("Bottle", new Color(0.55f, 0.85f, 1f), 0.9f));
                    Deco(PrimitiveType.Cylinder, gym, "Cap", p + Vector3.up * 0.75f, new Vector3(0.18f, 0.06f, 0.18f), SaveMat("BottleCap", Candy[i % Candy.Length], 0.5f));
                }
                Deco(PrimitiveType.Cube, gym, "Towel", new Vector3(side * 7.6f, 0.05f, 9.6f), new Vector3(1.2f, 0.06f, 0.7f), SaveMat("Towel", Color.white, 0.1f), Quaternion.Euler(0f, side * 20f, 0f));

                // Weight-plate tree.
                var tree = new GameObject("PlateTree").transform;
                tree.SetParent(gym);
                tree.position = new Vector3(side * 14.5f, 0f, -2f);
                Deco(PrimitiveType.Cylinder, tree, "Base", new Vector3(0f, 0.1f, 0f), new Vector3(1.4f, 0.1f, 1.4f), dark);
                Deco(PrimitiveType.Cylinder, tree, "Pole", new Vector3(0f, 1.1f, 0f), new Vector3(0.15f, 1f, 0.15f), metal);
                for (int i = 0; i < 3; i++)
                {
                    float y = 0.6f + i * 0.55f;
                    Deco(PrimitiveType.Cylinder, tree, "Peg", new Vector3(0f, y, 0f), new Vector3(0.08f, 0.45f, 0.08f), metal, Quaternion.Euler(0f, 0f, 90f));
                    foreach (float e in new[] { -0.38f, 0.38f })
                        Deco(PrimitiveType.Cylinder, tree, "Plate", new Vector3(e, y, 0f), new Vector3(0.7f - i * 0.1f, 0.05f, 0.7f - i * 0.1f), SaveMat("Plate" + i, Candy[(i * 2) % Candy.Length], 0.4f), Quaternion.Euler(0f, 0f, 90f));
                }
            }
        }

        /// <summary>Tall party speakers beside the console with pulsing cones.</summary>
        static void BuildSpeakers(Transform root)
        {
            var speakers = new GameObject("Speakers").transform;
            speakers.SetParent(root);
            var cab = SaveMat("SpeakerCab", new Color(0.15f, 0.14f, 0.2f), 0.35f);
            var cone = SaveMat("SpeakerCone", new Color(0.3f, 0.3f, 0.38f), 0.6f);
            var rim = SaveEmissive("SpeakerRim", new Color(1f, 0.45f, 0.75f), 1.8f);
            foreach (float x in new[] { -8.2f, 8.2f })
            {
                var sp = new GameObject("Speaker").transform;
                sp.SetParent(speakers);
                sp.position = new Vector3(x, 0f, 9f);
                sp.rotation = Quaternion.Euler(0f, x < 0 ? 25f : -25f, 0f);
                Deco(PrimitiveType.Cube, sp, "Cabinet", new Vector3(0f, 2.2f, 0f), new Vector3(1.8f, 4.4f, 1.4f), cab);
                for (int i = 0; i < 2; i++)
                {
                    float y = 1.3f + i * 2f;
                    float size = i == 0 ? 1.3f : 0.8f;
                    Deco(PrimitiveType.Cylinder, sp, "Rim", new Vector3(0f, y, -0.71f), new Vector3(size + 0.15f, 0.02f, size + 0.15f), rim, Quaternion.Euler(90f, 0f, 0f));
                    var c = Deco(PrimitiveType.Cylinder, sp, "Cone", new Vector3(0f, y, -0.73f), new Vector3(size, 0.03f, size), cone, Quaternion.Euler(90f, 0f, 0f));
                    var bob = c.AddComponent<Bob>();
                    bob.amplitude = Vector3.zero;
                    bob.swayDegrees = 0f;
                    bob.frequency = 2f;
                    bob.scalePulse = 0.08f;
                }
            }
        }

        /// <summary>Ring lights on tripods: the influencer-filming vibe of the original videos.</summary>
        static void BuildRingLights(Transform root)
        {
            var rigs = new GameObject("RingLights").transform;
            rigs.SetParent(root);
            var metal = SaveMat("TripodMetal", new Color(0.2f, 0.2f, 0.25f), 0.6f);
            var glow = SaveEmissive("RingGlow", new Color(1f, 0.97f, 0.9f), 2.5f);
            var spots = new[] { new Vector3(-15.5f, 0f, -7f), new Vector3(15.5f, 0f, -7f), new Vector3(-15f, 0f, 7.5f), new Vector3(15f, 0f, 7.5f) };
            foreach (var p in spots)
            {
                var rig = new GameObject("RingLight").transform;
                rig.SetParent(rigs);
                rig.position = p;
                var toCenter = new Vector3(0f, 0f, -1f) - p;
                toCenter.y = 0f;
                rig.rotation = Quaternion.LookRotation(toCenter);
                for (int i = 0; i < 3; i++)
                {
                    float a = i / 3f * Mathf.PI * 2f;
                    var foot = new Vector3(Mathf.Cos(a) * 0.9f, 0f, Mathf.Sin(a) * 0.9f);
                    var mid = (foot + new Vector3(0f, 2.2f, 0f)) * 0.5f;
                    Deco(PrimitiveType.Cylinder, rig, "Leg", mid, new Vector3(0.08f, 1.2f, 0.08f), metal, Quaternion.FromToRotation(Vector3.up, new Vector3(0f, 2.2f, 0f) - foot));
                }
                Deco(PrimitiveType.Cylinder, rig, "Pole", new Vector3(0f, 3.2f, 0f), new Vector3(0.1f, 1.1f, 0.1f), metal);
                // The ring itself: a circle of glowing beads facing the stage.
                var ring = new GameObject("Ring").transform;
                ring.SetParent(rig, false);
                ring.localPosition = new Vector3(0f, 4.9f, 0.1f);
                for (int i = 0; i < 20; i++)
                {
                    float a = i / 20f * Mathf.PI * 2f;
                    Deco(PrimitiveType.Sphere, ring, "Bead", new Vector3(Mathf.Cos(a) * 0.75f, Mathf.Sin(a) * 0.75f, 0f), Vector3.one * 0.26f, glow).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                // A phone clipped in the middle, pointed at the action.
                Deco(PrimitiveType.Cube, ring, "Phone", Vector3.zero, new Vector3(0.3f, 0.55f, 0.05f), SaveMat("PhoneBody", new Color(0.1f, 0.1f, 0.12f), 0.8f));
                var light = ring.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 6f;
                light.intensity = 0.6f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.shadows = LightShadows.None;
            }
        }

        /// <summary>Bunches of balloons tied along the barrier.</summary>
        static void BuildBalloons(Transform root)
        {
            var group = new GameObject("Balloons").transform;
            group.SetParent(root);
            var rng = new System.Random(5);
            float R() => (float)rng.NextDouble();
            var stringMat = SaveMat("BalloonString", new Color(0.95f, 0.95f, 1f), 0.2f);
            var anchors = new[] { new Vector3(-16.5f, 0f, -12f), new Vector3(16.5f, 0f, -12f), new Vector3(-17.2f, 0f, 1f), new Vector3(17.2f, 0f, 1f), new Vector3(-12.5f, 0f, 11.5f), new Vector3(12.5f, 0f, 11.5f) };
            foreach (var anchor in anchors)
            {
                var bunch = new GameObject("Bunch").transform;
                bunch.SetParent(group);
                bunch.position = anchor;
                int n = 4 + rng.Next(3);
                for (int i = 0; i < n; i++)
                {
                    var top = new Vector3((R() - 0.5f) * 1.6f, 4.5f + R() * 1.6f, (R() - 0.5f) * 1.6f);
                    var balloon = new GameObject("Balloon").transform;
                    balloon.SetParent(bunch, false);
                    balloon.localPosition = top;
                    Deco(PrimitiveType.Sphere, balloon, "Body", Vector3.zero, new Vector3(0.9f, 1.08f, 0.9f), SaveMat("Balloon" + (i % Candy.Length), Candy[(i + rng.Next(Candy.Length)) % Candy.Length], 0.85f));
                    Deco(PrimitiveType.Sphere, balloon, "Knot", new Vector3(0f, -0.56f, 0f), Vector3.one * 0.12f, SaveMat("Balloon" + (i % Candy.Length), Candy[i % Candy.Length], 0.85f));
                    Deco(PrimitiveType.Cylinder, bunch, "String", top * 0.5f + Vector3.up * -0.3f, new Vector3(0.02f, top.magnitude * 0.5f, 0.02f), stringMat, Quaternion.FromToRotation(Vector3.up, top));
                    var bob = balloon.gameObject.AddComponent<Bob>();
                    bob.amplitude = new Vector3(0.1f, 0.12f, 0.1f);
                    bob.frequency = 0.35f + R() * 0.2f;
                    bob.swayDegrees = 6f;
                }
            }
        }

        /// <summary>Overhead lighting truss with coloured spotlights pooling light on the stage.</summary>
        static void BuildTruss(Transform root)
        {
            var truss = new GameObject("Truss").transform;
            truss.SetParent(root);
            var metal = SaveMat("TrussMetal", new Color(0.82f, 0.82f, 0.88f), 0.85f);
            const float y = 17f, half = 15f;
            var c = new Vector3(0f, 0f, -2f);
            for (int side = 0; side < 4; side++)
            {
                bool alongX = side % 2 == 0;
                float off = side < 2 ? half : -half;
                var mid = c + (alongX ? new Vector3(0f, y, off) : new Vector3(off, y, 0f));
                var len = new Vector3(alongX ? half * 2f : 0.45f, 0.45f, alongX ? 0.45f : half * 2f);
                Deco(PrimitiveType.Cube, truss, "Beam", mid, len, metal);
                Deco(PrimitiveType.Cube, truss, "BeamLow", mid + Vector3.down * 0.8f, len, metal);
            }
            // Corner towers.
            foreach (var corner in new[] { new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half), new Vector3(-half, 0f, half), new Vector3(half, 0f, half) })
                Deco(PrimitiveType.Cube, truss, "Tower", c + corner + Vector3.up * y * 0.5f, new Vector3(0.5f, y, 0.5f), metal);

            // Spot cans hanging from the front and back beams.
            var canMat = SaveMat("SpotCan", new Color(0.18f, 0.18f, 0.22f), 0.6f);
            var colors = new[] { new Color(1f, 0.6f, 0.8f), new Color(0.6f, 0.8f, 1f), new Color(1f, 0.9f, 0.6f) };
            int k = 0;
            foreach (float z in new[] { -half, half })
                foreach (float x in new[] { -8f, 0f, 8f })
                {
                    var pos = c + new Vector3(x, y - 1.4f, z);
                    var target = new Vector3(x * 0.4f, 0f, -2f);
                    var rot = Quaternion.LookRotation(target - pos);
                    var can = Deco(PrimitiveType.Cylinder, truss, "SpotCan", pos, new Vector3(0.7f, 0.5f, 0.7f), canMat, rot * Quaternion.Euler(90f, 0f, 0f));
                    var lgo = new GameObject("Spot");
                    lgo.transform.SetParent(can.transform, false);
                    lgo.transform.position = pos;
                    lgo.transform.rotation = rot;
                    var l = lgo.AddComponent<Light>();
                    l.type = LightType.Spot;
                    l.spotAngle = 32f;
                    l.range = 30f;
                    l.intensity = 1.1f;
                    l.color = colors[k++ % colors.Length];
                    l.shadows = LightShadows.None;
                }
        }

        /// <summary>Strings of triangular pennants sagging between the truss towers and the sign posts.</summary>
        static void BuildBunting(Transform root)
        {
            var bunting = new GameObject("Bunting").transform;
            bunting.SetParent(root);
            var c = new Vector3(0f, 0f, -2f);
            var lineMat = SaveMat("BuntingLine", Color.white, 0.2f);
            var runs = new[]
            {
                (c + new Vector3(-15f, 14f, 15f), c + new Vector3(15f, 14f, 15f)),
                (c + new Vector3(-15f, 14f, -15f), c + new Vector3(-15f, 14f, 15f)),
                (c + new Vector3(15f, 14f, -15f), c + new Vector3(15f, 14f, 15f)),
                (new Vector3(-12.7f, 6.2f, 13f), new Vector3(-6.3f, 6.2f, 13f)),
                (new Vector3(6.3f, 6.2f, 13f), new Vector3(12.7f, 6.2f, 13f)),
            };
            int flag = 0;
            foreach (var (a, b) in runs)
            {
                float len = Vector3.Distance(a, b);
                int count = Mathf.Max(4, Mathf.RoundToInt(len / 1.1f));
                float sag = len * 0.08f;
                Vector3 prev = a;
                for (int i = 1; i <= count; i++)
                {
                    float t = i / (float)count;
                    var p = Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * sag;
                    var seg = p - prev;
                    Deco(PrimitiveType.Cube, bunting, "Line", (p + prev) * 0.5f, new Vector3(0.04f, 0.04f, seg.magnitude), lineMat, Quaternion.LookRotation(seg));
                    // Pennant: a thin square turned 45° and half-buried in the line reads as a triangle.
                    var pennant = Deco(PrimitiveType.Cube, bunting, "Pennant", p + Vector3.down * 0.05f, new Vector3(0.55f, 0.55f, 0.02f),
                        SaveMat("Pennant" + (flag % Candy.Length), Candy[flag % Candy.Length], 0.2f), Quaternion.LookRotation(Vector3.Cross(seg, Vector3.up)) * Quaternion.Euler(0f, 0f, 45f));
                    pennant.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    var bob = pennant.AddComponent<Bob>();
                    bob.amplitude = Vector3.zero;
                    bob.swayDegrees = 8f;
                    bob.frequency = 0.5f;
                    flag++;
                    prev = p;
                }
            }
        }

        /// <summary>Two big studio TV cameras on wheeled tripods, aimed at the belt from beside the crash mat.</summary>
        static void BuildTvCameras(Transform root)
        {
            var group = new GameObject("TvCameras").transform;
            group.SetParent(root);
            var body = SaveMat("TvCamBody", new Color(0.25f, 0.25f, 0.3f), 0.5f);
            var accent = SaveMat("TvCamAccent", new Color(1f, 0.45f, 0.6f), 0.5f);
            var lens = SaveMat("TvCamLens", new Color(0.08f, 0.1f, 0.2f), 0.95f);
            var tally = SaveEmissive("TvCamTally", new Color(1f, 0.15f, 0.2f), 3f);
            foreach (float x in new[] { -9.5f, 9.5f })
            {
                var rig = new GameObject("TvCamera").transform;
                rig.SetParent(group);
                rig.position = new Vector3(x, 0f, -13f);
                var aim = new Vector3(0f, 2.5f, -2f) - rig.position;
                aim.y = 0f;
                rig.rotation = Quaternion.LookRotation(aim);
                // Wheeled base and column.
                for (int i = 0; i < 3; i++)
                {
                    float a = i / 3f * Mathf.PI * 2f;
                    var foot = new Vector3(Mathf.Cos(a) * 1f, 0.2f, Mathf.Sin(a) * 1f);
                    Deco(PrimitiveType.Cube, rig, "Arm", foot * 0.5f + Vector3.up * 0.1f, new Vector3(0.18f, 0.12f, 1f), body, Quaternion.LookRotation(foot));
                    Deco(PrimitiveType.Sphere, rig, "Wheel", foot, Vector3.one * 0.3f, accent);
                }
                Deco(PrimitiveType.Cylinder, rig, "Column", new Vector3(0f, 1.3f, 0f), new Vector3(0.3f, 1.1f, 0.3f), body);
                // Camera head, tilted down at the belt.
                var head = new GameObject("Head").transform;
                head.SetParent(rig, false);
                head.localPosition = new Vector3(0f, 2.7f, 0f);
                head.localRotation = Quaternion.Euler(8f, 0f, 0f);
                Deco(PrimitiveType.Cube, head, "Body", new Vector3(0f, 0f, -0.2f), new Vector3(0.9f, 0.8f, 1.5f), body);
                Deco(PrimitiveType.Cube, head, "Stripe", new Vector3(0f, 0.41f, -0.2f), new Vector3(0.92f, 0.04f, 1.52f), accent);
                Deco(PrimitiveType.Cylinder, head, "Lens", new Vector3(0f, -0.05f, 0.75f), new Vector3(0.55f, 0.35f, 0.55f), lens, Quaternion.Euler(90f, 0f, 0f));
                Deco(PrimitiveType.Cylinder, head, "Hood", new Vector3(0f, -0.05f, 1.05f), new Vector3(0.7f, 0.08f, 0.7f), body, Quaternion.Euler(90f, 0f, 0f));
                Deco(PrimitiveType.Cube, head, "Viewfinder", new Vector3(0.55f, 0.25f, -0.7f), new Vector3(0.25f, 0.35f, 0.4f), body);
                Deco(PrimitiveType.Sphere, head, "Tally", new Vector3(0f, 0.5f, 0.3f), Vector3.one * 0.18f, tally);
                Deco(PrimitiveType.Cylinder, head, "Handle", new Vector3(-0.3f, -0.2f, -1.2f), new Vector3(0.07f, 0.5f, 0.07f), accent, Quaternion.Euler(70f, 0f, 0f));
            }
        }

        /// <summary>Game-show floor stickers on the stage beside the belt: chunky stars and a painted logo badge.</summary>
        static void BuildFloorStickers(Transform root)
        {
            var stickers = new GameObject("FloorStickers").transform;
            stickers.SetParent(root);
            var rng = new System.Random(13);
            float R() => (float)rng.NextDouble();

            // Five-point stars: five slim diamonds fanned at 72°, flat on the floor.
            var starSpots = new[] { new Vector3(-9f, 0f, -9f), new Vector3(9.5f, 0f, -8.5f), new Vector3(-12f, 0f, 1.5f), new Vector3(12.5f, 0f, 2.5f), new Vector3(-7.5f, 0f, 5f), new Vector3(7.8f, 0f, 5.5f) };
            for (int s = 0; s < starSpots.Length; s++)
            {
                var star = new GameObject("Star").transform;
                star.SetParent(stickers);
                star.position = starSpots[s] + Vector3.up * 0.04f;
                star.rotation = Quaternion.Euler(0f, R() * 72f, 0f);
                float size = 0.8f + R() * 0.5f;
                var mat = SaveMat("StarSticker" + (s % 3), s % 3 == 0 ? new Color(1f, 0.85f, 0.3f) : s % 3 == 1 ? new Color(1f, 0.55f, 0.75f) : new Color(0.55f, 0.8f, 1f), 0.35f);
                for (int i = 0; i < 5; i++)
                {
                    var arm = Deco(PrimitiveType.Cube, star, "Arm", Vector3.zero, new Vector3(0.38f * size, 0.01f, 1.7f * size), mat, Quaternion.Euler(0f, i * 72f, 0f) * Quaternion.Euler(0f, 45f, 0f));
                    arm.transform.localScale = new Vector3(0.5f * size, 0.01f, 0.5f * size);
                    arm.transform.localPosition = Quaternion.Euler(0f, i * 72f, 0f) * Vector3.forward * 0.42f * size;
                    arm.transform.localRotation = Quaternion.Euler(0f, i * 72f + 45f, 0f);
                }
                Deco(PrimitiveType.Cylinder, star, "Core", Vector3.zero, new Vector3(0.75f * size, 0.006f, 0.75f * size), mat);
            }

            // A round logo badge painted on each side of the belt.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (float x in new[] { -10.5f, 10.5f })
            {
                var badge = new GameObject("FloorLogo").transform;
                badge.SetParent(stickers);
                badge.position = new Vector3(x, 0.035f, -3.5f);
                Deco(PrimitiveType.Cylinder, badge, "Outer", Vector3.zero, new Vector3(4.2f, 0.005f, 4.2f), SaveMat("LogoOuter", new Color(0.45f, 0.3f, 0.85f), 0.3f));
                Deco(PrimitiveType.Cylinder, badge, "Inner", Vector3.up * 0.006f, new Vector3(3.6f, 0.005f, 3.6f), SaveMat("LogoInner", new Color(1f, 0.85f, 0.3f), 0.3f));
                var tgo = new GameObject("LogoText");
                tgo.transform.SetParent(badge, false);
                tgo.transform.localPosition = Vector3.up * 0.02f;
                tgo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var tm = tgo.AddComponent<TextMesh>();
                tm.text = "TM";
                tm.font = font;
                tm.fontSize = 140;
                tm.characterSize = 0.1f;
                tm.fontStyle = FontStyle.Bold;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = new Color(0.4f, 0.25f, 0.8f);
                tgo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }

        /// <summary>Three-tier pastel bleachers in an arc behind the back half of the crowd.</summary>
        static void BuildBleachers(Transform root)
        {
            var bleachers = new GameObject("Bleachers").transform;
            bleachers.SetParent(root);
            var c = new Vector3(0f, -0.45f, -2f);
            const int segments = 14;
            for (int s = 0; s < segments; s++)
            {
                // Arc across the back (angles 25°..155°, 90° = straight behind the console).
                float a = Mathf.Lerp(25f, 155f, (s + 0.5f) / segments) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var rot = Quaternion.LookRotation(-dir);
                for (int tier = 0; tier < 3; tier++)
                {
                    float r = 22.5f + tier * 1.4f;
                    float h = 0.7f + tier * 0.7f;
                    var p = c + dir * r + Vector3.up * h * 0.5f;
                    Deco(PrimitiveType.Cube, bleachers, "Tier", p, new Vector3(r * 2f * Mathf.PI / (segments * 2.9f), h, 1.4f), SaveMat("Bleacher" + (s + tier) % 3, Candy[((s + tier) % 3) * 2 % Candy.Length] * 0.85f + Color.white * 0.15f, 0.25f), rot);
                    // White seat edge on each tier.
                    Deco(PrimitiveType.Cube, bleachers, "Edge", p + Vector3.up * (h * 0.5f + 0.03f) - dir * 0.55f, new Vector3(r * 2f * Mathf.PI / (segments * 2.9f), 0.06f, 0.25f), SaveMat("BleacherEdge", Color.white, 0.3f), rot);

                    // Seated fans: simple blobs (body + head) that bounce along. Kept to two per tier for mobile.
                    for (int f = 0; f < 2; f++)
                    {
                        float side = (f - 0.5f) * r * 2f * Mathf.PI / (segments * 2.9f) * 0.55f;
                        var lateral = Vector3.Cross(Vector3.up, dir).normalized;
                        var seat = c + dir * r + Vector3.up * h + lateral * side;
                        var fan = new GameObject("Fan").transform;
                        fan.SetParent(bleachers);
                        fan.position = seat;
                        fan.rotation = rot;
                        int pick = (s * 3 + tier * 2 + f) % Candy.Length;
                        Deco(PrimitiveType.Capsule, fan, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.7f, 0.55f, 0.55f), SaveMat("FanShirt" + pick, Candy[pick], 0.3f));
                        var skin = new[] { new Color(1f, 0.85f, 0.72f), new Color(0.85f, 0.65f, 0.48f), new Color(0.55f, 0.38f, 0.25f) }[(s + f + tier) % 3];
                        Deco(PrimitiveType.Sphere, fan, "Head", new Vector3(0f, 1.3f, 0f), Vector3.one * 0.5f, SaveMat("FanSkin" + (s + f + tier) % 3, skin, 0.3f));
                        var bob = fan.gameObject.AddComponent<Bob>();
                        bob.amplitude = new Vector3(0f, 0.12f, 0f);
                        bob.frequency = 0.8f + ((s + f) % 3) * 0.25f;
                        bob.swayDegrees = 3f;
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------------------
        // Purchasable arena decor (DecorCatalog). Each item is a hidden child "Decor_<id>" of ShopDecor;
        // DecorManager switches on the ones the player owns.
        // ---------------------------------------------------------------------------------------------

        static void BuildShopDecor(Treadmill tm)
        {
            var root = new GameObject("ShopDecor").AddComponent<DecorManager>().transform;
            GameObject Item(string id) { var g = new GameObject("Decor_" + id); g.transform.SetParent(root); return g; }

            // Potted palms.
            var plants = Item("plants").transform;
            var pot = SaveMat("Pot", new Color(0.85f, 0.5f, 0.35f), 0.3f);
            var trunk = SaveMat("PalmTrunk", new Color(0.6f, 0.45f, 0.3f), 0.2f);
            var leaf = SaveMat("PalmLeaf", new Color(0.35f, 0.8f, 0.4f), 0.35f);
            foreach (var p in new[] { new Vector3(-15f, 0f, -4f), new Vector3(15f, 0f, -4f), new Vector3(-12.5f, 0f, 9.5f), new Vector3(12.5f, 0f, 9.5f) })
            {
                var plant = new GameObject("Palm").transform;
                plant.SetParent(plants);
                plant.position = p;
                Deco(PrimitiveType.Cylinder, plant, "Pot", new Vector3(0f, 0.5f, 0f), new Vector3(1.3f, 0.5f, 1.3f), pot);
                Deco(PrimitiveType.Cylinder, plant, "Trunk", new Vector3(0f, 2f, 0f), new Vector3(0.25f, 1.3f, 0.25f), trunk);
                for (int i = 0; i < 6; i++)
                {
                    var l = Deco(PrimitiveType.Sphere, plant, "Leaf", Vector3.zero, new Vector3(0.5f, 0.12f, 1.8f), leaf, Quaternion.Euler(25f, i * 60f, 0f));
                    l.transform.localPosition = new Vector3(0f, 3.3f, 0f) + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 0.8f;
                }
            }

            // Floor lamps with warm light.
            var lamps = Item("lamps").transform;
            var lampMetal = SaveMat("LampMetal", new Color(0.2f, 0.2f, 0.25f), 0.7f);
            var shade = SaveEmissive("LampShade", new Color(1f, 0.9f, 0.65f), 1.6f);
            foreach (var p in new[] { new Vector3(-16.5f, 0f, -9f), new Vector3(16.5f, 0f, -9f), new Vector3(-16f, 0f, 5f), new Vector3(16f, 0f, 5f) })
            {
                var lamp = new GameObject("Lamp").transform;
                lamp.SetParent(lamps);
                lamp.position = p;
                Deco(PrimitiveType.Cylinder, lamp, "Base", new Vector3(0f, 0.08f, 0f), new Vector3(1f, 0.08f, 1f), lampMetal);
                Deco(PrimitiveType.Cylinder, lamp, "Pole", new Vector3(0f, 2.2f, 0f), new Vector3(0.12f, 2.2f, 0.12f), lampMetal);
                Deco(PrimitiveType.Cylinder, lamp, "Shade", new Vector3(0f, 4.5f, 0f), new Vector3(1.4f, 0.55f, 1.4f), shade);
                var lg = new GameObject("Light");
                lg.transform.SetParent(lamp, false);
                lg.transform.localPosition = new Vector3(0f, 4f, 0f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point; l.range = 9f; l.intensity = 1.4f; l.color = new Color(1f, 0.85f, 0.6f); l.shadows = LightShadows.None;
            }

            // Neon sign.
            var neon = Item("neon").transform;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var side in new[] { -1f, 1f })
            {
                var sign = new GameObject("NeonSign").transform;
                sign.SetParent(neon);
                sign.position = new Vector3(side * 11.5f, 0f, 7f);
                // TextMesh and the glow face local -Z, so point +Z away from the arena centre.
                sign.rotation = Quaternion.LookRotation(sign.position - new Vector3(0f, 0f, -4f));
                Deco(PrimitiveType.Cube, sign, "Post", new Vector3(0f, 1.8f, 0.2f), new Vector3(0.25f, 3.6f, 0.25f), lampMetal);
                Deco(PrimitiveType.Cube, sign, "Board", new Vector3(0f, 4f, 0f), new Vector3(5.2f, 1.8f, 0.2f), SaveMat("NeonBoard", new Color(0.1f, 0.08f, 0.15f), 0.6f));
                Deco(PrimitiveType.Cube, sign, "Glow", new Vector3(0f, 4f, -0.11f), new Vector3(4.8f, 1.4f, 0.02f), SaveEmissive("NeonGlow", new Color(1f, 0.25f, 0.65f), 0.6f));
                var tgo = new GameObject("Text");
                tgo.transform.SetParent(sign, false);
                tgo.transform.localPosition = new Vector3(0f, 4f, -0.14f);
                var t = tgo.AddComponent<TextMesh>();
                t.text = side < 0 ? "GO TUNA!" : "NO CAMPING!";
                t.font = font; t.fontSize = 120; t.characterSize = 0.06f; t.fontStyle = FontStyle.Bold;
                t.anchor = TextAnchor.MiddleCenter; t.color = new Color(1f, 0.9f, 1f);
                tgo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                var lg = new GameObject("Light");
                lg.transform.SetParent(sign, false);
                lg.transform.localPosition = new Vector3(0f, 4f, -1f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point; l.range = 6f; l.intensity = 1.5f; l.color = new Color(1f, 0.3f, 0.7f); l.shadows = LightShadows.None;
            }

            // LED strips along both sides of the treadmill, colour-chasing.
            var led = Item("led").transform;
            var bulb = SaveMat("BulbDim", new Color(1f, 0.95f, 0.85f), 0.8f);
            foreach (var side in new[] { -1f, 1f })
            {
                var strip = new GameObject("Strip").transform;
                strip.SetParent(led);
                for (int i = 0; i < 24; i++)
                {
                    float z = Mathf.Lerp(tm.RearZ - 0.5f, tm.FrontZ, i / 23f);
                    Deco(PrimitiveType.Cube, strip, "LED", new Vector3(side * (tm.HalfWidth + 0.95f), tm.TopY + 0.3f, z), new Vector3(0.18f, 0.12f, 0.5f), bulb).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                var chase = strip.gameObject.AddComponent<MarqueeLights>();
                chase.palette = new[] { new Color(1f, 0.3f, 0.6f), new Color(0.3f, 0.8f, 1f), new Color(0.5f, 1f, 0.4f), new Color(1f, 0.85f, 0.3f) };
                chase.groupSize = 4;
                chase.speed = 10f;
            }

            // Camera drone.
            var droneRoot = Item("drone").transform;
            var drone = new GameObject("Drone").transform;
            drone.SetParent(droneRoot);
            drone.position = new Vector3(7f, 6.5f, -5f);
            var droneBody = SaveMat("DroneBody", new Color(0.92f, 0.92f, 0.96f), 0.7f);
            var droneDark = SaveMat("DroneDark", new Color(0.15f, 0.15f, 0.2f), 0.6f);
            Deco(PrimitiveType.Cube, drone, "Body", Vector3.zero, new Vector3(0.8f, 0.25f, 0.8f), droneBody);
            Deco(PrimitiveType.Sphere, drone, "Lens", new Vector3(0f, -0.2f, 0.35f), Vector3.one * 0.28f, droneDark);
            var rotors = new List<Transform>();
            foreach (var c in new[] { new Vector3(-0.6f, 0.1f, -0.6f), new Vector3(0.6f, 0.1f, -0.6f), new Vector3(-0.6f, 0.1f, 0.6f), new Vector3(0.6f, 0.1f, 0.6f) })
            {
                Deco(PrimitiveType.Cylinder, drone, "Arm", c * 0.5f, new Vector3(0.08f, 0.45f, 0.08f), droneDark, Quaternion.FromToRotation(Vector3.up, c));
                rotors.Add(Deco(PrimitiveType.Cube, drone, "Rotor", c + Vector3.up * 0.08f, new Vector3(0.7f, 0.02f, 0.08f), droneDark).transform);
            }
            Deco(PrimitiveType.Sphere, drone, "Tally", new Vector3(0f, 0.15f, 0.3f), Vector3.one * 0.1f, SaveEmissive("TvCamTally", new Color(1f, 0.15f, 0.2f), 3f));
            drone.gameObject.AddComponent<DroneCamera>().rotors = rotors.ToArray();

            // Confetti cannons beside the crash mat.
            var cannons = Item("cannons").transform;
            foreach (var side in new[] { -1f, 1f })
            {
                var cannon = new GameObject("Cannon").transform;
                cannon.SetParent(cannons);
                cannon.position = new Vector3(side * 11f, 0f, -12f);
                Deco(PrimitiveType.Cylinder, cannon, "Base", new Vector3(0f, 0.4f, 0f), new Vector3(1.2f, 0.4f, 1.2f), SaveMat("CannonBase", new Color(0.3f, 0.3f, 0.4f), 0.5f));
                var aim = Quaternion.LookRotation(new Vector3(-side * 4f, 10f, 8f));
                var barrel = Deco(PrimitiveType.Cylinder, cannon, "Barrel", new Vector3(0f, 1.2f, 0f), new Vector3(0.6f, 0.9f, 0.6f), SaveMat("CannonBarrel", new Color(1f, 0.4f, 0.6f), 0.6f), aim * Quaternion.Euler(90f, 0f, 0f));
                var psGo = new GameObject("Burst");
                psGo.transform.SetParent(cannon, false);
                psGo.transform.localPosition = new Vector3(0f, 1.2f, 0f) + aim * Vector3.forward * 0.9f;
                psGo.transform.localRotation = aim;
                var ps = psGo.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = false; main.loop = false; main.startLifetime = 3.5f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.28f);
                main.gravityModifier = 0.6f;
                main.startRotation3D = true;
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.7f), new Color(0.4f, 0.9f, 1f));
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = 0f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 18f; sh.radius = 0.2f;
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
                rot.x = new ParticleSystem.MinMaxCurve(-4f, 4f); rot.y = new ParticleSystem.MinMaxCurve(-4f, 4f); rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
                var r = psGo.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Mesh;
                r.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Confetti.mat");
                cannon.gameObject.AddComponent<ConfettiCannon>().burst = ps;
            }

            // Disco ball over the stage with swinging coloured spots.
            var discoRoot = Item("disco").transform;
            var cable = SaveMat("Cable", new Color(0.2f, 0.2f, 0.22f), 0.5f);
            Deco(PrimitiveType.Cylinder, discoRoot, "Cable", new Vector3(0f, 14.6f, -1f), new Vector3(0.05f, 1.8f, 0.05f), cable);
            var ballMat = SaveMat("DiscoBall", new Color(0.85f, 0.88f, 0.95f), 0.95f);
            ballMat.SetFloat("_Metallic", 1f);
            ballMat.EnableKeyword("_EMISSION");
            ballMat.SetColor("_EmissionColor", new Color(0.25f, 0.25f, 0.3f));
            var ball = Deco(PrimitiveType.Sphere, discoRoot, "Ball", new Vector3(0f, 12f, -1f), Vector3.one * 1.8f, ballMat);
            // Mirror tiles: little facets on the surface.
            var tile = SaveEmissive("MirrorTile", new Color(1f, 1f, 1f), 0.6f);
            for (int i = 0; i < 60; i++)
            {
                var dir = Random.onUnitSphere;
                Deco(PrimitiveType.Cube, ball.transform, "Tile", dir * 0.5f, Vector3.one * 0.12f, tile, Quaternion.LookRotation(dir));
            }
            var beams = new List<Light>();
            for (int i = 0; i < 4; i++)
            {
                var bgo = new GameObject("Beam" + i);
                bgo.transform.SetParent(discoRoot, false);
                bgo.transform.position = new Vector3(0f, 12f, -1f);
                var l = bgo.AddComponent<Light>();
                l.type = LightType.Spot; l.spotAngle = 22f; l.range = 30f; l.intensity = 3f; l.shadows = LightShadows.None;
                beams.Add(l);
            }
            var disco = ball.AddComponent<DiscoBall>();
            disco.beams = beams.ToArray();

            // Trophy shelf.
            var trophies = Item("trophies").transform;
            var shelf = new GameObject("Shelf").transform;
            shelf.SetParent(trophies);
            shelf.position = new Vector3(-14.5f, 0f, 0f);
            shelf.rotation = Quaternion.Euler(0f, 90f, 0f);
            var wood = SaveMat("ShelfWood", new Color(0.55f, 0.35f, 0.25f), 0.35f);
            var gold = SaveMat("TrophyGold", new Color(1f, 0.78f, 0.25f), 0.92f);
            gold.SetFloat("_Metallic", 1f);
            Deco(PrimitiveType.Cube, shelf, "Cabinet", new Vector3(0f, 1.2f, 0.3f), new Vector3(4.5f, 2.4f, 0.9f), wood);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 1.4f;
                float h = i == 1 ? 1.2f : 0.9f;
                Deco(PrimitiveType.Cube, shelf, "TrophyBase", new Vector3(x, 2.55f, 0.2f), new Vector3(0.6f, 0.25f, 0.6f), wood);
                Deco(PrimitiveType.Cylinder, shelf, "Stem", new Vector3(x, 2.65f + h * 0.3f, 0.2f), new Vector3(0.15f, h * 0.3f, 0.15f), gold);
                Deco(PrimitiveType.Sphere, shelf, "Cup", new Vector3(x, 2.7f + h * 0.75f, 0.2f), new Vector3(0.75f, 0.6f, 0.75f), gold);
                Deco(PrimitiveType.Cylinder, shelf, "Rim", new Vector3(x, 2.7f + h * 0.95f, 0.2f), new Vector3(0.8f, 0.04f, 0.8f), gold);
            }

            // Laser show from the truss corners.
            var lasers = Item("lasers").transform;
            var laserColors = new[] { new Color(0.3f, 1f, 0.4f), new Color(1f, 0.3f, 0.5f), new Color(0.3f, 0.7f, 1f), new Color(1f, 0.9f, 0.3f) };
            var corners = new[] { new Vector3(-14.5f, 16f, -16.5f), new Vector3(14.5f, 16f, -16.5f), new Vector3(-14.5f, 16f, 12.5f), new Vector3(14.5f, 16f, 12.5f) };
            for (int i = 0; i < corners.Length; i++)
            {
                var emitter = new GameObject("Laser" + i).transform;
                emitter.SetParent(lasers);
                emitter.position = corners[i];
                emitter.rotation = Quaternion.LookRotation(new Vector3(0f, 0f, -2f) - corners[i]);
                Deco(PrimitiveType.Cube, emitter, "Box", Vector3.zero, new Vector3(0.6f, 0.6f, 0.8f), droneDark);
                var beam = Deco(PrimitiveType.Cylinder, emitter, "Beam", new Vector3(0f, 0f, 14f), new Vector3(0.06f, 14f, 0.06f), SaveEmissive("Laser" + i, laserColors[i], 4f), Quaternion.Euler(90f, 0f, 0f));
                beam.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var sweep = emitter.gameObject.AddComponent<LaserSweep>();
                sweep.phase = i * 1.3f;
            }

            foreach (Transform child in root) child.gameObject.SetActive(false);
        }

        /// <summary>Lollipop trees out past the pillars for the wide shots.</summary>
        static void BuildTrees(Transform root)
        {
            var trees = new GameObject("Trees").transform;
            trees.SetParent(root);
            var rng = new System.Random(9);
            float R() => (float)rng.NextDouble();
            var trunk = SaveMat("TreeTrunk", new Color(0.75f, 0.55f, 0.42f), 0.2f);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f + R() * 0.2f;
                float r = 36f + R() * 10f;
                var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r - 2f);
                float h = 3f + R() * 3f;
                Deco(PrimitiveType.Cylinder, trees, "Trunk", p + Vector3.up * h * 0.5f, new Vector3(0.5f, h * 0.5f, 0.5f), trunk);
                float crown = 3f + R() * 2.5f;
                Deco(PrimitiveType.Sphere, trees, "Crown", p + Vector3.up * (h + crown * 0.35f), Vector3.one * crown, SaveMat("TreeCrown" + (i % 3), Candy[(i % 3) * 2 % Candy.Length] * 0.9f + Color.white * 0.1f, 0.3f));
            }
        }

        /// <summary>A light, continuous drift of colourful confetti over the stage.</summary>
        static void BuildConfetti(Transform root)
        {
            var go = new GameObject("Confetti");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(0f, 16f, -2f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 12f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Candy[0], 0f), new GradientColorKey(Candy[1], 0.25f), new GradientColorKey(Candy[2], 0.5f), new GradientColorKey(Candy[3], 0.75f), new GradientColorKey(Candy[4], 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };
            main.gravityModifier = 0.02f;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 14f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 34f, 0.5f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.separateAxes = true;
            spin.x = new ParticleSystem.MinMaxCurve(-3f, 3f);
            spin.y = new ParticleSystem.MinMaxCurve(-3f, 3f);
            spin.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            renderer.alignment = ParticleSystemRenderSpace.Local;
            // Particle shader so each piece takes its random start colour; double-sided so flips stay visible.
            var matPath = Root + "/Materials/Confetti.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Simple Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = Shader.Find("Universal Render Pipeline/Particles/Simple Lit");
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(mat);
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (type == PrimitiveType.Cylinder)
            {
                // The cylinder primitive ships with a capsule collider, which balloons into a giant sphere
                // when the cylinder is scaled flat and wide. Use the real mesh instead.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            return go;
        }

        static void BuildPostProcessing(Transform parent)
        {
            var path = Root + "/Materials/PostFX.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);

                var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
                bloom.intensity.Override(0.6f);
                bloom.threshold.Override(1.05f);
                bloom.scatter.Override(0.6f);

                var color = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
                color.saturation.Override(18f);
                color.contrast.Override(8f);
                color.postExposure.Override(0.15f);

                var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
                vignette.intensity.Override(0.22f);
                vignette.smoothness.Override(0.5f);

                foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            var vol = new GameObject("PostFX").AddComponent<Volume>();
            vol.transform.SetParent(parent);
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        static Treadmill BuildTreadmill()
        {
            var root = new GameObject("Treadmill");

            // treadmill.glb carries the painted textures; treadmill_parts.glb (same layout) was only used to measure the belt.
            var model = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel("treadmill"));
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            float s = TreadmillScale * FootprintStretch;
            model.transform.localScale = new Vector3(s, TreadmillScale, s);

            float beltLen = (BeltFrontX - BeltRearX) * s;
            float beltWidth = BeltHalfWidth * 2f * s;
            float beltCenterZ = (BeltFrontX + BeltRearX) * 0.5f * s;
            float top = BeltTop * TreadmillScale;

            // The physics belt is a separate kinematic box sitting exactly on the model's belt.
            var beltGo = new GameObject("Belt");
            beltGo.transform.SetParent(root.transform, false);
            beltGo.transform.localPosition = new Vector3(0f, top, beltCenterZ);
            var box = beltGo.AddComponent<BoxCollider>();
            box.size = new Vector3(beltWidth, 0.6f, beltLen);
            box.center = new Vector3(0f, -0.3f, 0f);
            box.sharedMaterial = SavePhysMat("Belt", 0.9f, 0f);
            beltGo.AddComponent<Rigidbody>();
            var tm = beltGo.AddComponent<Treadmill>();
            tm.beltWidth = beltWidth;
            tm.beltLength = beltLen;
            tm.beltTopY = 0f;

            // A real belt loop: over the top, round the rear roller, back underneath and round the front roller.
            // The stripe texture scrolls along the loop, so the markings visibly travel over the edge and under.
            var stripes = new GameObject("BeltLoop");
            stripes.transform.SetParent(beltGo.transform, false);
            stripes.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            stripes.AddComponent<MeshFilter>().sharedMesh = SaveBeltLoopMesh(beltWidth, beltLen, 0.7f, 2f);
            var stripeMat = SaveMat("BeltStripes", Color.white, 0.25f);
            stripeMat.SetTexture("_BaseMap", SaveStripeTexture());
            stripeMat.SetTextureScale("_BaseMap", Vector2.one);
            var loopRenderer = stripes.AddComponent<MeshRenderer>();
            loopRenderer.sharedMaterial = stripeMat;
            loopRenderer.shadowCastingMode = ShadowCastingMode.Off;
            tm.beltRenderer = loopRenderer;
            tm.textureTilesPerMeter = 0.5f;

            // Side walls and the motor hood at the front keep racers on the belt; only the back is open.
            var walls = new GameObject("Walls");
            walls.transform.SetParent(root.transform, false);
            var wallMat = SavePhysMat("Wall", 0.2f, 0.35f);
            AddWall(walls.transform, "LeftWall", new Vector3(-beltWidth * 0.5f - 0.3f, top + 0.5f, beltCenterZ), new Vector3(0.6f, 1.6f, beltLen), wallMat);
            AddWall(walls.transform, "RightWall", new Vector3(beltWidth * 0.5f + 0.3f, top + 0.5f, beltCenterZ), new Vector3(0.6f, 1.6f, beltLen), wallMat);
            AddWall(walls.transform, "Hood", new Vector3(0f, top + 0.5f, BeltFrontX * s + 2f), new Vector3(beltWidth + 1.2f, 2.5f, 4f), wallMat);

            return tm;
        }

        /// <summary>
        /// Finds the yellow button baked into the console mesh (the bump standing proud of the sloped top face)
        /// and drops the interactive StartButton exactly on top of it.
        /// </summary>
        static void PlaceStartButton(GameObject model)
        {
            var mf = model.GetComponentInChildren<MeshFilter>();
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            Physics.SyncTransforms();
            try
            {
                // Average the sloped console face along the centre line.
                Vector3 n = Vector3.zero, c = Vector3.zero;
                int k = 0;
                for (float z = 0f; z < 12f; z += 0.1f)
                {
                    if (!Physics.Raycast(new Vector3(0f, 20f, z), Vector3.down, out var h, 30f) || h.collider != mc || h.point.y < 5f) continue;
                    if (h.normal.y > 0.4f && h.normal.y < 0.9f && h.normal.z < -0.3f) { n += h.normal; c += h.point; k++; }
                }
                if (k == 0) { Debug.LogWarning("Treadmill Mayhem: console face not found; start button skipped."); return; }
                n.Normalize();
                c /= k;

                // Sample heights above that face; the button is the cluster that sticks up the most.
                var right = Vector3.right;
                var up = Vector3.Cross(right, n).normalized;
                var samples = new List<(Vector3 p, float h)>();
                for (float a = -3f; a <= 3f; a += 0.05f)
                for (float b = -2.5f; b <= 2.5f; b += 0.05f)
                    if (Physics.Raycast(c + right * a + up * b + n * 3f, -n, out var hit, 6f) && hit.collider == mc)
                        samples.Add((hit.point, Vector3.Dot(hit.point - c, n)));
                var heights = samples.ConvertAll(s => s.h);
                heights.Sort();
                float baseline = heights[heights.Count / 2], top = heights[heights.Count - 1];

                Vector3 centre = Vector3.zero;
                int count = 0;
                float extent = 0f;
                foreach (var s in samples)
                    if (s.h - baseline >= (top - baseline) * 0.5f) { centre += s.p; count++; }
                centre /= Mathf.Max(1, count);
                foreach (var s in samples)
                    if (s.h - baseline >= (top - baseline) * 0.5f) extent = Mathf.Max(extent, Vector3.ProjectOnPlane(s.p - centre, n).magnitude);

                var onFace = centre - n * (Vector3.Dot(centre - c, n) - baseline);
                var go = new GameObject("StartButton");
                go.transform.SetParent(model.transform.parent, true);
                go.transform.position = onFace;
                go.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, n), n);
                // The half-height contour sits inside the bump's footprint; pad so the baked button is fully covered.
                go.AddComponent<StartButton>().radius = extent * 1.15f;
            }
            finally
            {
                Object.DestroyImmediate(mc);
            }
        }

        /// <summary>
        /// Stadium-shaped ribbon mesh in belt space (top surface at y = 0, length along Z). UV.v runs opposite to
        /// the belt's motion in tiles of <paramref name="tileLength"/> metres, so an increasing texture offset makes
        /// the stripes travel with the belt: backward on top, down over the rear roller, forward underneath.
        /// </summary>
        static Mesh SaveBeltLoopMesh(float width, float length, float radius, float tileLength)
        {
            const int arcSegments = 14;
            float half = length * 0.5f, hw = width * 0.5f;
            var points = new List<(Vector3 pos, Vector3 normal)>();

            // Path in the direction of belt motion: top (front → rear), rear arc, bottom (rear → front), front arc.
            points.Add((new Vector3(0f, 0f, half), Vector3.up));
            points.Add((new Vector3(0f, 0f, -half), Vector3.up));
            for (int i = 1; i <= arcSegments; i++)
            {
                float a = Mathf.Lerp(90f, 270f, i / (float)arcSegments) * Mathf.Deg2Rad;
                var n = new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a));
                points.Add((new Vector3(0f, -radius, -half) + n * radius, n));
            }
            points.Add((new Vector3(0f, -2f * radius, half), Vector3.down));
            for (int i = 1; i <= arcSegments; i++)
            {
                float a = Mathf.Lerp(-90f, 90f, i / (float)arcSegments) * Mathf.Deg2Rad;
                var n = new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a));
                points.Add((new Vector3(0f, -radius, half) + n * radius, n));
            }

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            float s = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0) s += Vector3.Distance(points[i].pos, points[i - 1].pos);
                float v = -s / tileLength;
                verts.Add(points[i].pos + Vector3.left * hw); normals.Add(points[i].normal); uvs.Add(new Vector2(0f, v));
                verts.Add(points[i].pos + Vector3.right * hw); normals.Add(points[i].normal); uvs.Add(new Vector2(1f, v));
            }

            var tris = new List<int>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                // Wind each quad so its face points along the outward normal.
                var faceNormal = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                if (Vector3.Dot(faceNormal, normals[a] + normals[c]) >= 0f) tris.AddRange(new[] { a, b, c, b, d, c });
                else tris.AddRange(new[] { a, c, b, b, c, d });
            }

            var mesh = new Mesh { name = "BeltLoop" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var path = Root + "/Materials/BeltLoop.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { existing.Clear(); EditorUtility.CopySerialized(mesh, existing); return existing; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void AddWall(Transform parent, string name, Vector3 pos, Vector3 size, PhysicsMaterial mat)
        {
            var w = new GameObject(name);
            w.transform.SetParent(parent, false);
            w.transform.localPosition = pos;
            var c = w.AddComponent<BoxCollider>();
            c.size = size;
            c.sharedMaterial = mat;
        }

        static void BuildRacers(Treadmill tm)
        {
            var parent = new GameObject("Racers").transform;
            float top = tm.transform.position.y;
            float z0 = tm.transform.position.z;
            var slots = new List<Vector3>
            {
                // Grid sits in the front half so a new player has several seconds before the rear edge.
                new Vector3(0f, 0f, 2.5f),   // player: middle of the pack
                new Vector3(-2.2f, 0f, 4.6f), new Vector3(2.2f, 0f, 4.6f), new Vector3(0f, 0f, 5.2f),
                new Vector3(-2.3f, 0f, 1.6f), new Vector3(2.3f, 0f, 1.6f),
            };

            // Every racer becomes a prefab; the GameManager spawns the tuna plus a random lineup at runtime.
            lineupSlots = slots.ConvertAll(s => new Vector3(s.x, top + 0.75f, z0 + s.z)).ToArray();
            opponentPrefabs = new List<GameObject>();
            for (int i = 0; i < Racers.Length; i++)
            {
                var seed = Racers[i];
                var def = EnsureRacerDefinition(seed);
                var prefab = BuildRacerPrefab(def, seed);
                if (seed.player) playerPrefab = prefab;
                else opponentPrefabs.Add(prefab);
                RenderPortrait(prefab, def);
            }
        }

        const string PortraitDir = Root + "/Resources/Portraits";

        /// <summary>Character-select portrait: the racer's model on a soft tint of its colour, saved as a sprite.</summary>
        static void RenderPortrait(GameObject prefab, RacerDefinition def)
        {
            const int size = 256;
            var stage = new GameObject("PortraitStage") { hideFlags = HideFlags.HideAndDontSave };
            var origin = new Vector3(0f, -800f, 0f);
            stage.transform.position = origin;
            try
            {
                var modelSrc = prefab.transform.Find("Model");
                if (modelSrc == null) return;
                var model = Object.Instantiate(modelSrc.gameObject, stage.transform);
                model.transform.localPosition = Vector3.zero;
                // Sliders (the fish) read best side-on; everything else at a friendly three-quarter angle.
                var pose = def.shape == RacerShape.Slider ? Quaternion.Euler(-10f, 100f, 0f) : Quaternion.Euler(-15f, 210f, 10f);
                model.transform.localRotation = pose * modelSrc.localRotation;
                var b = new Bounds(model.transform.position, Vector3.zero);
                bool first = true;
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
                model.transform.position += origin - b.center;
                float extent = Mathf.Max(b.size.x, b.size.y) * 0.62f;

                var lightGo = new GameObject("Light");
                lightGo.transform.SetParent(stage.transform, false);
                lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;

                var cam = new GameObject("Cam").AddComponent<Camera>();
                cam.transform.SetParent(stage.transform, false);
                cam.transform.position = origin + new Vector3(0f, 0f, -10f);
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(0.2f, extent);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.Lerp(def.color, Color.white, 0.55f);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 25f;

                var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);

                System.IO.Directory.CreateDirectory(PortraitDir);
                var path = PortraitDir + "/" + def.displayName + ".png";
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                Object.DestroyImmediate(stage);
            }
        }

        static Vector3[] lineupSlots;
        static GameObject playerPrefab;
        static List<GameObject> opponentPrefabs;

        /// <summary>Loads (or creates from the spec defaults) the racer's definition asset. Existing tuning is kept.</summary>
        static RacerDefinition EnsureRacerDefinition(RacerSeed s)
        {
            var dir = ConfigDir + "/Racers";
            System.IO.Directory.CreateDirectory(dir);
            var path = dir + "/" + s.name + ".asset";
            var def = AssetDatabase.LoadAssetAtPath<RacerDefinition>(path);
            if (def != null)
            {
                if (def.visualModel == null) { def.visualModel = LoadModel(s.model); EditorUtility.SetDirty(def); }
                return def;
            }
            def = ScriptableObject.CreateInstance<RacerDefinition>();
            def.displayName = s.name;
            def.color = s.color;
            def.shape = s.shape;
            def.visualModel = LoadModel(s.model);
            def.visualSize = s.size;
            def.mass = s.mass;
            def.friction = s.friction;
            def.bounciness = s.bounce;
            def.acceleration = s.accel;
            def.steering = s.steer;
            def.maxRelativeSpeed = s.maxRel;
            def.stability = s.stability;
            def.wobbleSusceptibility = s.wobble;
            def.rollingInstability = s.instability;
            def.bumpStrength = s.bump;
            def.knockbackTaken = s.knock;
            def.visualRotation = s.visualRotation;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        /// <summary>
        /// Physics root (Rigidbody, simple collider, Racer + systems) with the visual model as a separate child,
        /// so a replacement model never changes physics. The collider is fitted to the model's bounds.
        /// </summary>
        static GameObject BuildRacerPrefab(RacerDefinition def, RacerSeed seed)
        {
            var root = new GameObject(def.displayName);
            GameObject model;
            if (def.visualModel != null)
            {
                model = (GameObject)PrefabUtility.InstantiatePrefab(def.visualModel);
            }
            else
            {
                // Placeholder primitive if no model has been assigned yet.
                model = GameObject.CreatePrimitive(def.shape == RacerShape.Can ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
                Object.DestroyImmediate(model.GetComponent<Collider>());
                if (def.shape == RacerShape.Can) model.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                model.GetComponent<Renderer>().sharedMaterial = SaveMat("Placeholder_" + def.displayName, def.color, 0.6f);
            }
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localRotation = model.transform.localRotation * Quaternion.Euler(def.visualRotation);
            model.transform.localScale = model.transform.localScale * def.visualSize;

            // Model pivots sit at the base; centre the mesh on the rigidbody so it rolls true.
            var b = MeshBounds(model);
            model.transform.localPosition -= b.center;

            var phys = SavePhysMat("Racer_" + def.displayName, def.friction, def.bounciness);
            if (def.shape == RacerShape.Can)
            {
                var cap = root.AddComponent<CapsuleCollider>();
                cap.direction = 0;
                cap.radius = (b.size.y + b.size.z) * 0.25f;
                cap.height = b.size.x;
                cap.sharedMaterial = phys;
            }
            else if (def.shape == RacerShape.Slider)
            {
                // Capsule along the body's long axis (nose to tail), sized to its thickness.
                var cap = root.AddComponent<CapsuleCollider>();
                bool alongZ = b.size.z >= b.size.x;
                cap.direction = alongZ ? 2 : 0;
                cap.height = alongZ ? b.size.z : b.size.x;
                cap.radius = Mathf.Min(b.size.y, alongZ ? b.size.x : b.size.z) * 0.5f;
                cap.sharedMaterial = phys;
            }
            else
            {
                var sph = root.AddComponent<SphereCollider>();
                sph.radius = Mathf.Max(b.size.x, b.size.y, b.size.z) * 0.5f * 0.97f;
                sph.sharedMaterial = phys;
            }

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = def.mass;
            rb.linearDamping = def.linearDamping;
            rb.angularDamping = def.angularDamping;

            // Racer first: the systems below [RequireComponent(Racer)] and would otherwise auto-add a second one.
            var racer = root.AddComponent<Racer>();
            racer.definition = def;
            racer.isPlayer = seed.player;
            root.AddComponent<StaminaSystem>();
            root.AddComponent<AccelerationSystem>();

            if (seed.player)
            {
                root.AddComponent<SuperSpeedSystem>();
                root.AddComponent<PlayerController>();
            }
            else
            {
                var ai = root.AddComponent<AIController>();
                ai.laneBias = seed.laneBias;
                ai.aggressionBias = seed.aggressionBias;
                ai.greedBias = seed.greedBias;
            }

            var path = PrefabDir + "/" + def.displayName + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameConfig EnsureGameConfig()
        {
            System.IO.Directory.CreateDirectory(ConfigDir);
            var path = ConfigDir + "/GameConfig.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(cfg, path);
            }
            return cfg;
        }

        /// <summary>Five AI levels that differ in decisions, latency, prediction and risk — not speed.</summary>
        static AIDifficultyProfile[] EnsureDifficulties()
        {
            var dir = ConfigDir + "/AI";
            System.IO.Directory.CreateDirectory(dir);
            var specs = new (string name, Color color, string blurb, float reaction, float look, float predict, float aware, float noise, float mistake, float recover, float correct, float rear, float aggro, float greed, float boost, float risk)[]
            {
                ("EASY", new Color(0.35f, 0.9f, 0.45f), "Slow reactions, weak corrections, lots of mistakes. Barely notices obstacles.", 0.45f, 0.3f, 0.2f, 0.3f, 0.5f, 0.25f, 0.3f, 0.35f, 0.18f, 0.03f, 0.15f, 0.05f, 0.2f),
                ("NORMAL", new Color(0.4f, 0.75f, 1f), "Basic survival. Sometimes dodges, sometimes wanders into trouble.", 0.3f, 0.6f, 0.45f, 0.55f, 0.35f, 0.12f, 0.5f, 0.55f, 0.28f, 0.15f, 0.35f, 0.25f, 0.3f),
                ("HARD", new Color(1f, 0.8f, 0.2f), "Holds good position, avoids obstacles, recovers well and steps around rivals.", 0.2f, 0.9f, 0.65f, 0.75f, 0.22f, 0.06f, 0.7f, 0.75f, 0.36f, 0.35f, 0.55f, 0.5f, 0.4f),
                ("EXPERT", new Color(1f, 0.5f, 0.2f), "Fast and tidy. Collects stamina efficiently and picks its fights.", 0.12f, 1.2f, 0.85f, 0.9f, 0.12f, 0.03f, 0.85f, 0.9f, 0.42f, 0.6f, 0.75f, 0.75f, 0.55f),
                ("INSANE", new Color(1f, 0.25f, 0.35f), "Ruthless. Near-perfect positioning, steals pickups and shoves you toward the edge.", 0.07f, 1.5f, 1f, 1f, 0.05f, 0.01f, 1f, 1f, 0.46f, 0.85f, 0.95f, 0.9f, 0.7f),
            };
            var result = new AIDifficultyProfile[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                var path = dir + "/AI_" + (i + 1) + "_" + s.name + ".asset";
                var p = AssetDatabase.LoadAssetAtPath<AIDifficultyProfile>(path);
                if (p == null)
                {
                    p = ScriptableObject.CreateInstance<AIDifficultyProfile>();
                    p.displayName = s.name; p.color = s.color; p.blurb = s.blurb;
                    p.reactionTime = s.reaction; p.lookahead = s.look; p.predictionAccuracy = s.predict; p.awareness = s.aware;
                    p.steeringNoise = s.noise; p.mistakeChance = s.mistake; p.recovery = s.recover; p.correctionStrength = s.correct;
                    p.rearSafety = s.rear; p.aggression = s.aggro; p.pickupGreed = s.greed; p.boostUsage = s.boost; p.riskTolerance = s.risk;
                    AssetDatabase.CreateAsset(p, path);
                }
                result[i] = p;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        /// <summary>Spectators (spawned at runtime by CrowdManager) plus low barriers keeping them off the stage.</summary>
        static void BuildCrowd()
        {
            var crowd = new GameObject("Crowd").AddComponent<CrowdManager>();
            crowd.radius = 19f;
            crowd.center = new Vector3(0f, -0.45f, -2f);

            var barriers = new GameObject("Barriers").transform;
            var postMat = SaveMat("BarrierPost", new Color(1f, 1f, 1f), 0.4f);
            var ropeMat = SaveMat("BarrierRope", new Color(1f, 0.35f, 0.55f), 0.3f);
            const float r = 17.4f;
            Vector3 prev = Vector3.zero;
            int n = 0;
            for (float a = crowd.cameraGap.y; a <= crowd.cameraGap.x + 360f + 0.01f; a += 12f)
            {
                float rad = a * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r - 2f);
                var post = Prim(PrimitiveType.Cylinder, barriers, "Post", p + Vector3.up * 0.6f, new Vector3(0.25f, 0.6f, 0.25f), postMat);
                Object.DestroyImmediate(post.GetComponent<Collider>());
                if (n++ > 0)
                {
                    var mid = (prev + p) * 0.5f + Vector3.up * 1f;
                    var rope = Prim(PrimitiveType.Cube, barriers, "Rope", mid, new Vector3(0.08f, 0.08f, Vector3.Distance(prev, p)), ropeMat);
                    rope.transform.rotation = Quaternion.LookRotation(p - prev);
                    Object.DestroyImmediate(rope.GetComponent<Collider>());
                }
                prev = p;
            }
        }

        static void BuildCamera(Treadmill tm)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(1f, 0.84f, 0.9f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<TreadmillMayhem.AudioFX>();
            var urpCam = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            urpCam.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            // Tight framing: the belt fills the screen, with the rear edge just inside the bottom of the frame.
            // Pitch ~36°: the rear roller sits ~7° inside the bottom of the frame, the console top just inside the top.
            var look = new Vector3(0f, tm.TopY, tm.transform.position.z - 1.8f);
            camGo.transform.position = look + new Vector3(0f, 9.7f, -13.5f);
            camGo.transform.LookAt(look);

            var rig = camGo.AddComponent<CameraRig>();
            rig.lookPoint = look;
            rig.baseFov = 47f;
            rig.fovPerSpeed = 0.45f;
            cam.fieldOfView = 47f;
            foreach (var r in Object.FindObjectsByType<Racer>()) if (r.isPlayer) rig.target = r.transform;
        }

        static GameObject LoadModel(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(ModelDir + "/" + name + ".glb");

        /// <summary>
        /// Tight world-space bounds from the actual vertices. Renderer.bounds would box the model's original
        /// bounding box, which balloons for models that were rotated into alignment (rolling pin, bottle).
        /// </summary>
        static Bounds MeshBounds(GameObject go)
        {
            bool any = false;
            var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var m = mf.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var p = m.MultiplyPoint3x4(v);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            if (!any)
            {
                var rs = go.GetComponentsInChildren<Renderer>();
                b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
            }
            return b;
        }

        static Material SaveMat(string name, Color c, float smoothness)
        {
            var path = Root + "/Materials/" + name + ".mat";
            System.IO.Directory.CreateDirectory(Root + "/Materials");
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        static PhysicsMaterial SavePhysMat(string name, float friction, float bounce)
        {
            var path = Root + "/Materials/" + name + ".physicMaterial";
            System.IO.Directory.CreateDirectory(Root + "/Materials");
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (m == null)
            {
                m = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(m, path);
            }
            m.dynamicFriction = friction;
            m.staticFriction = friction;
            m.bounciness = bounce;
            m.frictionCombine = PhysicsMaterialCombine.Maximum;
            m.bounceCombine = PhysicsMaterialCombine.Maximum;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Texture2D SaveStripeTexture()
        {
            var path = Root + "/Materials/BeltStripes.png";
            if (!System.IO.File.Exists(path))
            {
                const int w = 128, h = 128;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                var belt = new Color(0.16f, 0.16f, 0.19f);
                var dash = new Color(0.62f, 0.62f, 0.66f);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool isDash = y >= 52 && y < 64 && ((x > 14 && x < 34) || (x > 40 && x < 60) || (x > 94 && x < 114));
                    float grain = (Mathf.PerlinNoise(x * 0.3f, y * 0.3f) - 0.5f) * 0.03f;
                    var c = isDash ? dash : belt;
                    tex.SetPixel(x, y, new Color(c.r + grain, c.g + grain, c.b + grain));
                }
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void AddSceneToBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
