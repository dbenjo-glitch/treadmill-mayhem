using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    public class DecorDef
    {
        public string id;
        public string name;
        public string blurb;
        public int cost;
        public string badge;
        public Color color;
    }

    /// <summary>Arena decorations bought with Tuna Coins. Each one is a prebuilt, hidden object in the scene.</summary>
    public static class DecorCatalog
    {
        public static readonly List<DecorDef> All = new List<DecorDef>
        {
            new DecorDef { id = "plants",   name = "POTTED PLANTS",    cost = 120, badge = "PLT", color = new Color(0.45f, 0.85f, 0.45f), blurb = "Big leafy palms around the stage." },
            new DecorDef { id = "lamps",    name = "CORNER LAMPS",     cost = 150, badge = "LMP", color = new Color(1f, 0.85f, 0.5f),    blurb = "Warm floor lamps in the corners." },
            new DecorDef { id = "neon",     name = "NEON SIGN",        cost = 300, badge = "NEO", color = new Color(1f, 0.35f, 0.75f),   blurb = "A buzzing 'GO TUNA!' neon sign." },
            new DecorDef { id = "led",      name = "LED STRIPS",       cost = 350, badge = "LED", color = new Color(0.4f, 0.8f, 1f),     blurb = "Colour-cycling LEDs along the treadmill." },
            new DecorDef { id = "drone",    name = "DRONE CAMERA",     cost = 400, badge = "DRN", color = new Color(0.75f, 0.75f, 0.85f), blurb = "A camera drone that follows the action." },
            new DecorDef { id = "cannons",  name = "CONFETTI CANNONS", cost = 450, badge = "CNF", color = new Color(1f, 0.6f, 0.3f),     blurb = "Fire confetti whenever someone falls off." },
            new DecorDef { id = "disco",    name = "DISCO BALL",       cost = 500, badge = "DSC", color = new Color(0.85f, 0.85f, 1f),   blurb = "Spinning mirror ball with dancing lights." },
            new DecorDef { id = "trophies", name = "TROPHY SHELF",     cost = 650, badge = "TRP", color = new Color(1f, 0.8f, 0.2f),     blurb = "Gleaming golden trophies on display." },
            new DecorDef { id = "lasers",   name = "LASER SHOW",       cost = 800, badge = "LSR", color = new Color(0.4f, 1f, 0.55f),    blurb = "Sweeping lasers over the stage." },
        };

        public static DecorDef Find(string id) => All.Find(d => d.id == id);
        public static bool Owned(string id) => SaveSystem.Data.decorOwned.Contains(id);
        public static bool Visible(string id) => Owned(id) && !SaveSystem.Data.decorHidden.Contains(id);

        public static bool TryBuy(DecorDef def)
        {
            if (Owned(def.id)) return false;
            if (!SaveSystem.TrySpend(Currency.Coins, def.cost)) return false;
            SaveSystem.Data.decorOwned.Add(def.id);
            SaveSystem.Save();
            Achievements.Unlock("decorator");
            Object.FindAnyObjectByType<DecorManager>()?.Refresh();
            return true;
        }

        public static void SetVisible(string id, bool visible)
        {
            var hidden = SaveSystem.Data.decorHidden;
            if (visible) hidden.Remove(id);
            else if (!hidden.Contains(id)) hidden.Add(id);
            SaveSystem.Save();
            Object.FindAnyObjectByType<DecorManager>()?.Refresh();
        }
    }
}
