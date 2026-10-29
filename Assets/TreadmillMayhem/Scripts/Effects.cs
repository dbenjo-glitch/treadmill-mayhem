using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>Runtime-built juice: particle bursts, shield bubbles, and URP materials.</summary>
    public static class Effects
    {
        static ParticleSystem burstPrefab;
        static readonly Dictionary<Color, Material> matCache = new Dictionary<Color, Material>();

        public static Material MakeMat(Color c, float smoothness = 0.6f, bool transparent = false)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader);
            m.SetColor("_BaseColor", c);
            m.color = c;
            m.SetFloat("_Smoothness", smoothness);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return m;
        }

        public static Material SharedMat(Color c)
        {
            if (!matCache.TryGetValue(c, out var m) || m == null) matCache[c] = m = MakeMat(c);
            return m;
        }

        public static void Burst(Vector3 pos, Color color, int count)
        {
            if (burstPrefab == null) burstPrefab = BuildBurst();
            var ps = Object.Instantiate(burstPrefab, pos, Quaternion.identity);
            ps.gameObject.SetActive(true);
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            ps.Emit(count);
            Object.Destroy(ps.gameObject, 1.5f);
        }

        static ParticleSystem BuildBurst()
        {
            var go = new GameObject("BurstFX");
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            var unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            r.sharedMaterial = new Material(unlit);
            r.renderMode = ParticleSystemRenderMode.Mesh;
            r.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            return ps;
        }

        public static TrailRenderer CreateTrail(Transform parent, Color color, float width)
        {
            var go = new GameObject("BoostTrail");
            go.transform.SetParent(parent, false);
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.35f;
            tr.minVertexDistance = 0.05f;
            tr.widthCurve = new AnimationCurve(new Keyframe(0f, width), new Keyframe(1f, 0f));
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.4f), 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            tr.colorGradient = grad;
            var unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            var m = new Material(unlit);
            m.SetFloat("_Surface", 1f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            tr.sharedMaterial = m;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.emitting = false;
            return tr;
        }

        public static GameObject CreateShieldBubble(Transform parent, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ShieldBubble";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * radius * 2.2f / Mathf.Max(0.01f, parent.lossyScale.x);
            go.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.3f, 0.95f, 1f, 0.28f), 0.9f, true);
            return go;
        }
    }
}
