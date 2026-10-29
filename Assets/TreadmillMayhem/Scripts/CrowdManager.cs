using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Spawns a ring of spectators around the treadmill (leaving the camera's side open) and makes them
    /// react together when someone falls off.
    /// </summary>
    public class CrowdManager : MonoBehaviour
    {
        [Range(4, 20)] public int count = 12;
        public float radius = 16.5f;
        public Vector3 center = new Vector3(0f, 0f, -2f);
        [Tooltip("Spectators are this many times human size so they read next to the giant treadmill.")]
        public float spectatorScale = 2.4f;
        [Tooltip("Angle range (degrees, 0 = +X) kept clear so spectators never block the game camera.")]
        public Vector2 cameraGap = new Vector2(232f, 308f);
        [Tooltip("Optional replacement model (e.g. from Tripo). Leave empty for the built-in low-poly people.")]
        public GameObject spectatorModel;
        public int seed = 11;

        readonly List<Spectator> spectators = new List<Spectator>();

        static readonly Color[] Shirts =
        {
            new Color(1f, 0.35f, 0.4f), new Color(0.3f, 0.7f, 1f), new Color(1f, 0.8f, 0.2f), new Color(0.45f, 0.85f, 0.45f),
            new Color(0.75f, 0.45f, 1f), new Color(1f, 0.55f, 0.2f), new Color(0.2f, 0.85f, 0.8f), new Color(1f, 0.5f, 0.8f),
        };
        static readonly Color[] Skins =
        {
            new Color(1f, 0.85f, 0.72f), new Color(0.92f, 0.72f, 0.55f), new Color(0.75f, 0.55f, 0.38f), new Color(0.5f, 0.35f, 0.22f),
        };
        static readonly Color[] Pants =
        {
            new Color(0.2f, 0.25f, 0.45f), new Color(0.25f, 0.25f, 0.28f), new Color(0.85f, 0.8f, 0.7f), new Color(0.3f, 0.4f, 0.3f),
        };

        void Awake()
        {
            var rng = new System.Random(seed);
            float span = 360f - (cameraGap.y - cameraGap.x);
            for (int i = 0; i < count; i++)
            {
                float a = cameraGap.y + span * (i + 0.5f) / count + (float)(rng.NextDouble() - 0.5) * 8f;
                float rad = a * Mathf.Deg2Rad;
                float r = radius + (float)rng.NextDouble() * 1.5f;
                var pos = center + new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r);

                GameObject go;
                if (spectatorModel != null)
                {
                    go = Instantiate(spectatorModel, transform);
                }
                else
                {
                    go = new GameObject("Spectator_" + i);
                    go.transform.SetParent(transform, false);
                    var s = go.AddComponent<Spectator>();
                    s.shirt = Shirts[rng.Next(Shirts.Length)];
                    s.skin = Skins[rng.Next(Skins.Length)];
                    s.pants = Pants[rng.Next(Pants.Length)];
                    s.hair = Color.Lerp(new Color(0.15f, 0.08f, 0.04f), new Color(0.9f, 0.75f, 0.4f), (float)rng.NextDouble() * 0.6f);
                    s.wearsCap = rng.NextDouble() < 0.35;
                    spectators.Add(s);
                }
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * spectatorScale * (0.92f + (float)rng.NextDouble() * 0.16f);
                var toCenter = center - pos;
                toCenter.y = 0f;
                go.transform.rotation = Quaternion.LookRotation(toCenter);
            }
        }

        void OnEnable() => Racer.Eliminated += OnEliminated;
        void OnDisable() => Racer.Eliminated -= OnEliminated;

        void OnEliminated(Racer r)
        {
            AudioFX.Play(Sfx.Cheer, r.isPlayer ? 0.6f : 0.45f);
            foreach (var s in spectators) if (s) s.Cheer(Random.Range(0f, 0.35f));
        }
    }
}
