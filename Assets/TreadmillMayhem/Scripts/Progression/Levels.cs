using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    public enum ObstaclePattern { Singles, Wall, Rollers, Rain, Zigzag }

    /// <summary>One Obstacle Mode stage: survive for the duration while the belt and the junk ramp up.</summary>
    public class LevelDef
    {
        public int number;
        public string name;
        public float duration;
        public float beltStart, beltEnd;
        public float spawnStart, spawnEnd;  // seconds between waves
        public ObstaclePattern[] patterns;
        public int gearReward;
    }

    /// <summary>The Obstacle Mode level ladder. Difficulty climbs in belt speed, wave rate and pattern variety.</summary>
    public static class Levels
    {
        static readonly ObstaclePattern[] Basic = { ObstaclePattern.Singles };
        static readonly ObstaclePattern[] Walls = { ObstaclePattern.Singles, ObstaclePattern.Wall };
        static readonly ObstaclePattern[] Rolling = { ObstaclePattern.Singles, ObstaclePattern.Rollers, ObstaclePattern.Wall };
        static readonly ObstaclePattern[] Stormy = { ObstaclePattern.Singles, ObstaclePattern.Rain, ObstaclePattern.Rollers, ObstaclePattern.Wall };
        static readonly ObstaclePattern[] Everything = { ObstaclePattern.Singles, ObstaclePattern.Rain, ObstaclePattern.Rollers, ObstaclePattern.Wall, ObstaclePattern.Zigzag };

        public static readonly List<LevelDef> All = new List<LevelDef>
        {
            new LevelDef { number = 1,  name = "Warm Up",          duration = 25, beltStart = 1.4f, beltEnd = 2.4f, spawnStart = 2.6f, spawnEnd = 2.0f, patterns = Basic,      gearReward = 15 },
            new LevelDef { number = 2,  name = "Junk Drawer",      duration = 30, beltStart = 1.6f, beltEnd = 2.8f, spawnStart = 2.3f, spawnEnd = 1.7f, patterns = Basic,      gearReward = 20 },
            new LevelDef { number = 3,  name = "The Wall",         duration = 30, beltStart = 1.8f, beltEnd = 3.0f, spawnStart = 2.4f, spawnEnd = 1.8f, patterns = Walls,      gearReward = 25 },
            new LevelDef { number = 4,  name = "Rolling Thunder",  duration = 35, beltStart = 2.0f, beltEnd = 3.4f, spawnStart = 2.2f, spawnEnd = 1.6f, patterns = Rolling,    gearReward = 30 },
            new LevelDef { number = 5,  name = "Mid-Week Rush",    duration = 35, beltStart = 2.2f, beltEnd = 3.8f, spawnStart = 2.0f, spawnEnd = 1.5f, patterns = Rolling,    gearReward = 40 },
            new LevelDef { number = 6,  name = "Hail Storm",       duration = 40, beltStart = 2.4f, beltEnd = 4.0f, spawnStart = 2.0f, spawnEnd = 1.4f, patterns = Stormy,     gearReward = 50 },
            new LevelDef { number = 7,  name = "Conveyor Chaos",   duration = 40, beltStart = 2.6f, beltEnd = 4.4f, spawnStart = 1.8f, spawnEnd = 1.3f, patterns = Stormy,     gearReward = 60 },
            new LevelDef { number = 8,  name = "Zig Zag",          duration = 45, beltStart = 2.8f, beltEnd = 4.8f, spawnStart = 1.8f, spawnEnd = 1.2f, patterns = Everything, gearReward = 75 },
            new LevelDef { number = 9,  name = "Leg Day",          duration = 45, beltStart = 3.0f, beltEnd = 5.2f, spawnStart = 1.6f, spawnEnd = 1.1f, patterns = Everything, gearReward = 90 },
            new LevelDef { number = 10, name = "Cardio Hell",      duration = 50, beltStart = 3.2f, beltEnd = 5.6f, spawnStart = 1.5f, spawnEnd = 1.0f, patterns = Everything, gearReward = 110 },
            new LevelDef { number = 11, name = "Can't Stop Won't Stop", duration = 55, beltStart = 3.4f, beltEnd = 6.0f, spawnStart = 1.4f, spawnEnd = 0.9f, patterns = Everything, gearReward = 135 },
            new LevelDef { number = 12, name = "Tuna Legend",      duration = 60, beltStart = 3.6f, beltEnd = 6.6f, spawnStart = 1.3f, spawnEnd = 0.8f, patterns = Everything, gearReward = 175 },
        };

        public static int Count => All.Count;

        public static LevelDef Get(int number) => All[Mathf.Clamp(number, 1, All.Count) - 1];

        /// <summary>Free Play: an endless ramp that keeps adding patterns.</summary>
        public static LevelDef FreePlay => new LevelDef
        {
            number = 0, name = "Free Play", duration = float.PositiveInfinity,
            beltStart = 1.6f, beltEnd = 7f, spawnStart = 2.4f, spawnEnd = 0.75f, patterns = Everything, gearReward = 0,
        };
    }
}
