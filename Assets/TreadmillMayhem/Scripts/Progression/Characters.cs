using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    /// <summary>
    /// Playable characters. Every racer can be unlocked with Tuna Coins and played as; each keeps its own
    /// handling (a ball bounces, the fish slides, the rolling pin bulldozes). Upgrades apply to whoever you play.
    /// </summary>
    public static class Characters
    {
        public const string Default = "Tuna";

        /// <summary>Unlock prices in Tuna Coins, by racer display name. Unlisted racers cost 800.</summary>
        static readonly Dictionary<string, int> Prices = new Dictionary<string, int>
        {
            { "Tuna", 0 }, { "Orange", 250 }, { "Apple", 250 }, { "Onion", 400 }, { "Beans", 500 }, { "Cola", 600 },
            { "Ball", 650 }, { "Bread Can", 700 }, { "Paper Towel", 750 }, { "Water Bottle", 850 }, { "Fish", 1000 }, { "Rolling Pin", 1300 },
        };

        static readonly Dictionary<string, string> Blurbs = new Dictionary<string, string>
        {
            { "Tuna", "The all-rounder. Steady, solid, never gives up." },
            { "Orange", "Medium weight with a smooth, steady roll." },
            { "Apple", "Lumpy and a little unpredictable - wobbles off-line." },
            { "Onion", "Layers of grit. Rolls slightly off-line but holds on." },
            { "Beans", "Heavy hitter. Slow to start, impossible to shove." },
            { "Cola", "Fast and fizzy, but wobbles when hit from the side." },
            { "Ball", "Super bouncy and quick. Hard to control, fun to fling." },
            { "Bread Can", "Grippy and sturdy. Sticks to the belt like toast." },
            { "Paper Towel", "Light and absorbent - soaks up bumps, slides on hits." },
            { "Water Bottle", "Light, tippy and sloshy. Expert mode." },
            { "Fish", "Slippery! Slides and flops instead of rolling." },
            { "Rolling Pin", "Barely turns, but bulldozes everything in its path." },
        };

        public static string Blurb(string name) => Blurbs.TryGetValue(name, out var b) ? b : "";

        public static event Action Changed;

        public static int Price(string name) => Prices.TryGetValue(name, out var p) ? p : 800;

        public static bool Owned(string name) => name == Default || SaveSystem.Data.charactersOwned.Contains(name);

        public static string Selected
        {
            get
            {
                var s = SaveSystem.Data.selectedCharacter;
                if (string.IsNullOrEmpty(s) || !Owned(s)) return Default;
                // A save can name a character this build doesn't have (older or newer version): fall back.
                var gm = GameManager.Instance;
                if (gm != null && !AllPrefabs(gm).Exists(p => p.name == s)) return Default;
                return s;
            }
        }

        public static bool TryBuy(string name)
        {
            if (Owned(name)) return false;
            if (!SaveSystem.TrySpend(Currency.Coins, Price(name))) return false;
            SaveSystem.Data.charactersOwned.Add(name);
            SaveSystem.Data.selectedCharacter = name;
            SaveSystem.Save();
            Achievements.Unlock("new_look");
            Changed?.Invoke();
            return true;
        }

        public static void Select(string name)
        {
            if (!Owned(name)) return;
            SaveSystem.Data.selectedCharacter = name;
            SaveSystem.Save();
            Changed?.Invoke();
        }

        /// <summary>Every racer prefab the game knows about: the tuna plus the opponent pool.</summary>
        public static List<GameObject> AllPrefabs(GameManager gm)
        {
            var list = new List<GameObject>();
            if (gm == null) return list;
            if (gm.playerPrefab) list.Add(gm.playerPrefab);
            if (gm.opponentPool != null) foreach (var p in gm.opponentPool) if (p && !list.Contains(p)) list.Add(p);
            list.Sort((a, b) => Price(a.name).CompareTo(Price(b.name)));
            return list;
        }

        public static Sprite Portrait(string name) => Resources.Load<Sprite>("Portraits/" + name);

        public static string ShapeLabel(RacerShape shape) => shape switch
        {
            RacerShape.Ball => "BOUNCY BALL",
            RacerShape.Slider => "SLIDER",
            _ => "ROLLER",
        };

        /// <summary>0..1 ratings for the character card, spread across the roster's range.</summary>
        public static (float speed, float power, float grip, float control) Ratings(RacerDefinition d)
        {
            if (d == null) return (0.5f, 0.5f, 0.5f, 0.5f);
            return (Mathf.InverseLerp(5.8f, 8.2f, d.maxRelativeSpeed),
                    Mathf.InverseLerp(0.35f, 1.7f, d.mass),
                    Mathf.InverseLerp(0.2f, 0.85f, d.friction),
                    Mathf.InverseLerp(9f, 25f, d.steering));
        }
    }
}
