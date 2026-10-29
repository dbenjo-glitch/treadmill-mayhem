using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    public enum Currency { Coins, Gears }

    /// <summary>One purchasable, levelled upgrade.</summary>
    public class UpgradeDef
    {
        public string id;
        public string name;
        public string icon;          // short glyph shown on the card
        public Color color;
        public Currency currency;
        public int maxLevel;
        public int baseCost;
        public float costGrowth = 1.35f;
        /// <summary>Human-readable value at a given level, e.g. "+12% Speed".</summary>
        public Func<int, string> describe;
        /// <summary>Short gain popup shown on purchase, e.g. "+4% SPEED!".</summary>
        public string gainText;
        public string blurb;
    }

    /// <summary>
    /// Permanent upgrades. Tuna Coin upgrades improve the tuna in every mode; Gear upgrades only apply in
    /// Obstacle Mode. All values here are read by the gameplay systems, so every level changes real physics.
    /// </summary>
    public static class Upgrades
    {
        // Tuna Coin upgrades.
        public const string Speed = "speed", Stamina = "stamina", Accel = "accel", Balance = "balance", Boost = "boost", CoinMult = "coinmult";
        // Gear upgrades (Obstacle Mode).
        public const string ORunner = "o_runner", OLength = "o_length", OWidth = "o_width", OOffline = "o_offline", OLife = "o_life", OMagnet = "o_magnet";

        public static readonly List<UpgradeDef> All = new List<UpgradeDef>
        {
            new UpgradeDef { id = Speed, name = "MOVEMENT SPEED", icon = "SPD", color = new Color(1f, 0.75f, 0.2f), currency = Currency.Coins, maxLevel = 10, baseCost = 120,
                describe = l => "+" + (l * 4) + "% Speed", gainText = "+4% SPEED!", blurb = "Roll faster relative to the belt." },
            new UpgradeDef { id = Stamina, name = "STAMINA CAPACITY", icon = "STA", color = new Color(0.3f, 0.8f, 1f), currency = Currency.Coins, maxLevel = 10, baseCost = 100,
                describe = l => Mathf.RoundToInt(100f * (1f + 0.08f * l)) + " Max Stamina", gainText = "+8% STAMINA!", blurb = "Store more stamina for Shift boosts." },
            new UpgradeDef { id = Accel, name = "ACCELERATION", icon = "ACC", color = new Color(1f, 0.55f, 0.25f), currency = Currency.Coins, maxLevel = 10, baseCost = 130,
                describe = l => Mathf.Max(2.2f, 3f - 0.08f * l).ToString("0.00") + "s to Activate", gainText = "-0.08s ACCEL TIME!", blurb = "Kick into acceleration sooner." },
            new UpgradeDef { id = Balance, name = "BALANCE", icon = "BAL", color = new Color(0.55f, 0.85f, 0.45f), currency = Currency.Coins, maxLevel = 8, baseCost = 90,
                describe = l => "-" + (l * 5) + "% Wobble", gainText = "-5% WOBBLE!", blurb = "Shrug off glancing hits." },
            new UpgradeDef { id = Boost, name = "BOOST POWER", icon = "BST", color = new Color(0.75f, 0.5f, 1f), currency = Currency.Coins, maxLevel = 8, baseCost = 150,
                describe = l => "+" + (l * 5) + "% Boost", gainText = "+5% BOOST!", blurb = "Stronger stamina boosts and Super Speed." },
            new UpgradeDef { id = CoinMult, name = "COIN MULTIPLIER", icon = "x$", color = new Color(1f, 0.85f, 0.2f), currency = Currency.Coins, maxLevel = 5, baseCost = 200,
                describe = l => "x" + (1f + 0.1f * l).ToString("0.0") + " Coins", gainText = "+10% COINS!", blurb = "Earn more Tuna Coins every run." },

            new UpgradeDef { id = ORunner, name = "RUNNER SPEED", icon = "RUN", color = new Color(1f, 0.6f, 0.3f), currency = Currency.Gears, maxLevel = 8, baseCost = 40, costGrowth = 1.4f,
                describe = l => "+" + (l * 5) + "% Dodge Speed", gainText = "+5% DODGE SPEED!", blurb = "Faster sidesteps in Obstacle Mode." },
            new UpgradeDef { id = OLength, name = "LONGER BELT", icon = "LEN", color = new Color(0.45f, 0.75f, 1f), currency = Currency.Gears, maxLevel = 5, baseCost = 70, costGrowth = 1.5f,
                describe = l => "+" + (l * 6) + "% Length", gainText = "+6% BELT LENGTH!", blurb = "More room before the edge." },
            new UpgradeDef { id = OWidth, name = "WIDER BELT", icon = "WID", color = new Color(0.4f, 0.9f, 0.7f), currency = Currency.Gears, maxLevel = 5, baseCost = 70, costGrowth = 1.5f,
                describe = l => "+" + (l * 6) + "% Width", gainText = "+6% BELT WIDTH!", blurb = "More room to dodge sideways." },
            new UpgradeDef { id = OOffline, name = "OFFLINE EARNINGS", icon = "ZZZ", color = new Color(0.65f, 0.55f, 1f), currency = Currency.Gears, maxLevel = 8, baseCost = 50, costGrowth = 1.45f,
                describe = l => Mathf.RoundToInt(OfflineCoinsPerHour(l)) + " Coins / hour", gainText = "+25% OFFLINE COINS!", blurb = "Earn Tuna Coins while you're away." },
            new UpgradeDef { id = OLife, name = "EXTRA LIFE", icon = "1UP", color = new Color(1f, 0.4f, 0.5f), currency = Currency.Gears, maxLevel = 2, baseCost = 150, costGrowth = 2f,
                describe = l => l + (l == 1 ? " Revive" : " Revives") + " per Run", gainText = "+1 REVIVE!", blurb = "Get rescued back onto the belt." },
            new UpgradeDef { id = OMagnet, name = "GEAR MAGNET", icon = "MAG", color = new Color(1f, 0.8f, 0.35f), currency = Currency.Gears, maxLevel = 5, baseCost = 45, costGrowth = 1.45f,
                describe = l => (1f + 0.6f * l).ToString("0.0") + "m Pickup Range", gainText = "+0.6m MAGNET!", blurb = "Pull in gears from further away." },
        };

        public static UpgradeDef Find(string id) => All.Find(u => u.id == id);

        public static int Level(string id) => SaveSystem.UpgradeLevelOf(id);

        public static bool IsMaxed(UpgradeDef def) => Level(def.id) >= def.maxLevel;

        /// <summary>Base × growth^level, rounded to a friendly number (nearest 5).</summary>
        public static int Cost(UpgradeDef def, int level)
        {
            float raw = def.baseCost * Mathf.Pow(def.costGrowth, level);
            return Mathf.Max(5, Mathf.RoundToInt(raw / 5f) * 5);
        }

        public static int NextCost(UpgradeDef def) => Cost(def, Level(def.id));

        public static bool CanAfford(UpgradeDef def) => !IsMaxed(def) && SaveSystem.Balance(def.currency) >= NextCost(def);

        public static event Action<UpgradeDef> Purchased;

        public static bool TryBuy(UpgradeDef def)
        {
            if (IsMaxed(def)) return false;
            int cost = NextCost(def);
            if (!SaveSystem.TrySpend(def.currency, cost)) return false;
            SaveSystem.SetUpgradeLevel(def.id, Level(def.id) + 1);
            Purchased?.Invoke(def);
            Achievements.Unlock("first_upgrade");
            return true;
        }

        /// <summary>The cheapest upgrade the player can't yet afford (for "only N more coins!" prompts).</summary>
        public static UpgradeDef NextGoal(Currency currency)
        {
            UpgradeDef best = null;
            int bestCost = int.MaxValue;
            foreach (var u in All)
            {
                if (u.currency != currency || IsMaxed(u)) continue;
                int c = NextCost(u);
                if (c < bestCost) { bestCost = c; best = u; }
            }
            return best;
        }

        public static bool AnyAffordable(Currency currency)
        {
            foreach (var u in All) if (u.currency == currency && CanAfford(u)) return true;
            return false;
        }

        // ---------- Gameplay values (read by Racer / systems) ----------

        public static float MovementMultiplier => 1f + 0.04f * Level(Speed);
        public static float StaminaMultiplier => 1f + 0.08f * Level(Stamina);
        public static float AccelHoldSeconds(float baseSeconds) => Mathf.Max(2.2f, baseSeconds - 0.08f * Level(Accel));
        public static float WobbleMultiplier => 1f - 0.05f * Level(Balance);
        public static float BoostMultiplier => 1f + 0.05f * Level(Boost);
        public static float CoinMultiplier => 1f + 0.1f * Level(CoinMult);

        public static float ObstacleRunnerMultiplier => 1f + 0.05f * Level(ORunner);
        public static float BeltLengthMultiplier => 1f + 0.06f * Level(OLength);
        public static float BeltWidthMultiplier => 1f + 0.06f * Level(OWidth);
        public static int ExtraLives => Level(OLife);
        public static float MagnetRange => 1f + 0.6f * Level(OMagnet);

        public static float OfflineCoinsPerHour(int offlineLevel) => 45f * (1f + 0.25f * offlineLevel);
    }
}
