using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Obstacle Mode spawner: sends waves of junk down the belt following the current level's patterns,
    /// always leaving a dodgeable gap, and awards dodge points for obstacles that get past the player cleanly.
    /// </summary>
    public class ObstacleDirector : MonoBehaviour
    {
        public LevelDef level;
        public int Dodged { get; private set; }
        public event System.Action<Vector3> DodgedOne;

        Treadmill treadmill;
        Racer player;
        float nextWave;
        int waveCount;
        readonly List<Obstacle> tracked = new List<Obstacle>();
        float elapsed;

        public void Begin(LevelDef def, Treadmill tm, Racer p)
        {
            level = def;
            treadmill = tm;
            player = p;
            nextWave = 2f;
            elapsed = 0f;
        }

        /// <summary>0..1 through the level's ramp (Free Play ramps over 120 s).</summary>
        public float Ramp(float t) => Mathf.Clamp01(t / (float.IsInfinity(level.duration) ? 120f : level.duration));

        /// <summary>Free Play only: 0..1 extra pressure after the two-minute ramp, so long runs keep getting harder.</summary>
        public float Overtime => float.IsInfinity(level.duration) ? Mathf.Clamp01((elapsed - 120f) / 180f) : 0f;

        public void Tick(float dt)
        {
            if (level == null || treadmill == null) return;
            elapsed += dt;
            if (elapsed >= nextWave)
            {
                float interval = Mathf.Lerp(level.spawnStart, level.spawnEnd, Ramp(elapsed)) * Mathf.Lerp(1f, 0.55f, Overtime);
                nextWave = elapsed + interval * Random.Range(0.85f, 1.15f);
                SpawnWave();
                // Deep into Free Play, waves start doubling up.
                if (Overtime > 0.25f && Random.value < Overtime * 0.6f) SpawnWave();
            }
            TrackDodges();
        }

        void SpawnWave()
        {
            waveCount++;
            var patterns = level.patterns;
            // Ease in: the first few waves are always singles.
            var p = waveCount <= 2 ? ObstaclePattern.Singles : patterns[Random.Range(0, patterns.Length)];
            switch (p)
            {
                case ObstaclePattern.Wall: Wall(); break;
                case ObstaclePattern.Rollers: Roller(); break;
                case ObstaclePattern.Rain: StartCoroutine(Rain()); break;
                case ObstaclePattern.Zigzag: Zigzag(); break;
                default: Singles(); break;
            }
        }

        float FrontZ => treadmill.FrontZ - 2.6f;
        float CenterX => treadmill.transform.position.x;
        float Half => treadmill.HalfWidth - 0.45f;
        float DropY => treadmill.TopY + 1.6f;

        void Track(Obstacle o) { if (o) tracked.Add(o); }

        void Singles()
        {
            int n = Random.Range(1, 3 + Mathf.RoundToInt(Ramp(elapsed) * 2f));
            var used = new List<float>();
            for (int i = 0; i < n; i++)
            {
                float x = 0f;
                for (int tries = 0; tries < 6; tries++)
                {
                    x = Random.Range(-Half, Half);
                    bool clash = false;
                    foreach (var u in used) if (Mathf.Abs(u - x) < 1.4f) clash = true;
                    if (!clash) break;
                }
                used.Add(x);
                Track(Obstacle.Spawn((ObstacleKind)Random.Range(0, 3), new Vector3(CenterX + x, DropY, FrontZ + Random.Range(-0.8f, 0.8f))));
            }
        }

        /// <summary>A row of blocks across the belt with one gap wide enough for the tuna.</summary>
        void Wall()
        {
            float gap = 2.4f;
            float gapCenter = Random.Range(-Half + gap * 0.5f, Half - gap * 0.5f);
            for (float x = -Half; x <= Half + 0.01f; x += 0.95f)
            {
                if (Mathf.Abs(x - gapCenter) < gap * 0.5f) continue;
                Track(Obstacle.Spawn(ObstacleKind.Block, new Vector3(CenterX + x, DropY, FrontZ)));
            }
        }

        /// <summary>A long bar rolling down one side; dodge to the open side.</summary>
        void Roller()
        {
            float length = (Half * 2f) * Random.Range(0.45f, 0.6f);
            bool left = Random.value < 0.5f;
            float x = left ? -Half + length * 0.5f : Half - length * 0.5f;
            Track(Obstacle.SpawnRollerBar(new Vector3(CenterX + x, DropY, FrontZ), length));
        }

        /// <summary>Crates dropped from above near the player, each telegraphed by a warning target.</summary>
        IEnumerator Rain()
        {
            int n = Random.Range(3, 5 + Mathf.RoundToInt(Ramp(elapsed) * 2f));
            var spots = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                var around = player && !player.IsEliminated ? player.Body.position : treadmill.transform.position;
                var spot = new Vector3(
                    Mathf.Clamp(around.x + Random.Range(-3f, 3f), CenterX - Half, CenterX + Half),
                    treadmill.TopY + 0.03f,
                    Mathf.Clamp(around.z + Random.Range(-1.5f, 3f), treadmill.RearZ + 2f, treadmill.FrontZ - 2f));
                spots.Add(spot);
                WarningMarker.Spawn(spot, 1f, treadmill);
            }
            yield return new WaitForSeconds(1f);
            foreach (var s in spots)
            {
                // Markers ride the belt, so drop where the marker has drifted to.
                var drifted = s + treadmill.BeltVelocity * 1f;
                Track(Obstacle.Spawn(ObstacleKind.Crate, drifted + Vector3.up * 7f));
            }
        }

        /// <summary>A diagonal line of blocks that forces a sidestep.</summary>
        void Zigzag()
        {
            bool leftToRight = Random.value < 0.5f;
            int n = 5;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float x = Mathf.Lerp(-Half, Half * 0.25f, t) * (leftToRight ? 1f : -1f);
                Track(Obstacle.Spawn(ObstacleKind.Block, new Vector3(CenterX + x, DropY, FrontZ - i * 1.1f)));
            }
        }

        /// <summary>An obstacle that slides past the player without hitting them counts as a dodge.</summary>
        void TrackDodges()
        {
            if (player == null || player.IsEliminated) return;
            float pz = player.Body.position.z;
            for (int i = tracked.Count - 1; i >= 0; i--)
            {
                var o = tracked[i];
                if (o == null) { tracked.RemoveAt(i); continue; }
                if (o.transform.position.z < pz - 1.2f)
                {
                    if (!o.HitPlayer)
                    {
                        Dodged++;
                        DodgedOne?.Invoke(o.transform.position);
                    }
                    tracked.RemoveAt(i);
                }
            }
        }
    }
}
