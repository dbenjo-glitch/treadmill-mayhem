using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>
    /// Full-screen shop overlay with three tabs: permanent Tuna upgrades (coins), Obstacle Mode upgrades (gears)
    /// and arena decorations (coins). Every purchase is animated so the player sees exactly what they gained.
    /// </summary>
    public class ShopScreen : MonoBehaviour
    {
        public enum Tab { Tuna, Obstacle, Decor, Store }

        public Action Closed;

        Transform cardsRoot;
        Text coinText, gearText, title;
        readonly Dictionary<Tab, Button> tabButtons = new Dictionary<Tab, Button>();
        readonly List<Action> refreshers = new List<Action>();
        Tab current;

        public static ShopScreen Build(Transform canvas)
        {
            var root = UIKit.Backdrop(canvas, "Shop", 0.97f);
            var shop = root.AddComponent<ShopScreen>();
            shop.Construct();
            root.SetActive(false);
            return shop;
        }

        void Construct()
        {
            var t = transform;
            title = UIKit.Text(t, "SHOP", 84, new Vector2(0.5f, 1f), new Vector2(0f, -28f), TextAnchor.UpperCenter);
            title.color = UIKit.Gold;
            coinText = UIKit.CurrencyPill(t, new Vector2(1f, 1f), new Vector2(-30f, -30f), Currency.Coins, out _);
            gearText = UIKit.CurrencyPill(t, new Vector2(1f, 1f), new Vector2(-30f, -106f), Currency.Gears, out _);
            UIKit.Button(t, "BACK", new Vector2(0f, 1f), new Vector2(150f, -75f), new Vector2(220f, 80f), new Color(0.75f, 0.75f, 0.85f), Close, 34);

            var tabs = new[] { (Tab.Tuna, "TUNA UPGRADES", UIKit.Gold), (Tab.Obstacle, "OBSTACLE GEAR", UIKit.GearColor), (Tab.Decor, "ARENA DECOR", new Color(1f, 0.5f, 0.75f)), (Tab.Store, "STORE", UIKit.Green) };
            for (int i = 0; i < tabs.Length; i++)
            {
                var (tab, label, color) = tabs[i];
                var b = UIKit.Button(t, label, new Vector2(0.5f, 1f), new Vector2((i - 1.5f) * 420f, -200f), new Vector2(395f, 76f), color, () => Show(tab), 30);
                tabButtons[tab] = b;
            }

            cardsRoot = UIKit.Node(t, "Cards").transform;
            UIKit.Stretch((RectTransform)cardsRoot);

            SaveSystem.CurrencyChanged += RefreshBalances;
            Purchases.Changed += RefreshBalances;
        }

        void OnDestroy()
        {
            SaveSystem.CurrencyChanged -= RefreshBalances;
            Purchases.Changed -= RefreshBalances;
        }

        public void Show(Tab tab)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            current = tab;
            foreach (var kv in tabButtons)
            {
                bool on = kv.Key == tab;
                kv.Value.transform.localScale = Vector3.one * (on ? 1.06f : 0.94f);
                var img = kv.Value.GetComponent<Image>();
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, on ? 1f : 0.55f);
                var bounce = kv.Value.GetComponent<PressBounce>();
                if (bounce) bounce.Rebase(kv.Value.transform.localScale);
            }
            title.text = tab == Tab.Store ? "STORE" : tab == Tab.Decor ? "ARENA DECOR" : tab == Tab.Obstacle ? "OBSTACLE GEAR" : "UPGRADE TUNA";
            BuildCards();
            RefreshBalances();
        }

        public void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        void RefreshBalances()
        {
            if (coinText) coinText.text = SaveSystem.Data.totalCoins.ToString("N0");
            if (gearText) gearText.text = SaveSystem.Data.gears.ToString("N0");
            foreach (var r in refreshers) r();
        }

        void BuildCards()
        {
            foreach (Transform c in cardsRoot) Destroy(c.gameObject);
            refreshers.Clear();

            if (current == Tab.Store) { BuildStore(); return; }

            if (current == Tab.Decor)
            {
                var list = DecorCatalog.All;
                for (int i = 0; i < list.Count; i++) DecorCard(list[i], GridPos(i, list.Count, 3, new Vector2(560f, 230f)), new Vector2(530f, 205f));
                return;
            }

            var currency = current == Tab.Tuna ? Currency.Coins : Currency.Gears;
            var defs = Upgrades.All.FindAll(u => u.currency == currency);
            for (int i = 0; i < defs.Count; i++) UpgradeCard(defs[i], GridPos(i, defs.Count, 3, new Vector2(560f, 345f)), new Vector2(530f, 320f));
        }

        static Vector2 GridPos(int i, int count, int cols, Vector2 cell)
        {
            int rows = Mathf.CeilToInt(count / (float)cols);
            int r = i / cols, c = i % cols;
            float x = (c - (cols - 1) * 0.5f) * cell.x;
            float y = -60f - (r - (rows - 1) * 0.5f) * cell.y;
            return new Vector2(x, y);
        }

        // ---------------- Upgrade card ----------------

        void UpgradeCard(UpgradeDef def, Vector2 pos, Vector2 size)
        {
            var card = UIKit.Box(cardsRoot, "Card_" + def.id, new Vector2(0.5f, 0.5f), pos, size, UIKit.PanelLight);
            var bg = card.GetComponent<Image>();
            var t = card.transform;

            var badge = UIKit.Disc(t, new Vector2(0f, 1f), new Vector2(70f, -70f), 96f, def.color);
            UIKit.Text(badge.transform, def.icon, 30, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, false, 96f).color = UIKit.Ink;
            UIKit.Text(t, def.name, 34, new Vector2(0f, 1f), new Vector2(135f, -30f), TextAnchor.UpperLeft, true, 380f);
            var level = UIKit.Text(t, "", 26, new Vector2(0f, 1f), new Vector2(135f, -76f), TextAnchor.UpperLeft, false, 380f);
            level.color = def.color;

            var bar = UIKit.Bar(t, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(size.x - 50f, 20f), def.color);
            var now = UIKit.Text(t, "", 26, new Vector2(0f, 1f), new Vector2(26f, -170f), TextAnchor.UpperLeft, false, 480f);
            var next = UIKit.Text(t, "", 26, new Vector2(0f, 1f), new Vector2(26f, -205f), TextAnchor.UpperLeft, false, 480f);
            next.color = UIKit.Green;

            Text buyLabel;
            var buy = UIKit.Button(t, "", new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(size.x - 50f, 70f), UIKit.Green, null, out buyLabel, 30);

            void Refresh()
            {
                int lvl = Upgrades.Level(def.id);
                bool maxed = lvl >= def.maxLevel;
                level.text = "LEVEL " + lvl + " / " + def.maxLevel;
                now.text = "Current: " + def.describe(lvl);
                next.text = maxed ? "Fully upgraded!" : "Next: " + def.describe(lvl + 1);
                if (bar.fillAmount == 0f && lvl > 0 && !bar.GetComponent<BarFill>()) bar.fillAmount = lvl / (float)def.maxLevel;
                int cost = Upgrades.NextCost(def);
                bool afford = Upgrades.CanAfford(def);
                buy.interactable = afford;
                buyLabel.text = maxed ? "MAXED" : "UPGRADE  -  " + cost.ToString("N0") + (def.currency == Currency.Coins ? " COINS" : " GEARS");
                buy.GetComponent<Image>().color = maxed ? new Color(0.45f, 0.45f, 0.55f) : afford ? UIKit.Green : new Color(0.4f, 0.42f, 0.5f);
            }

            buy.onClick.AddListener(() =>
            {
                if (!Upgrades.TryBuy(def)) { AudioFX.Play(Sfx.Out, 0.4f, 1.4f); ScalePunch.On(buy, 0.08f); return; }
                AudioFX.Play(Sfx.SuperReady, 0.8f, 1.2f);
                Flash.On(bg, Color.Lerp(def.color, Color.white, 0.4f));
                ScalePunch.On(level, 0.35f);
                ScalePunch.On(card.transform, 0.06f);
                BarFill.To(bar, Upgrades.Level(def.id) / (float)def.maxLevel);
                FloatUp.Spawn(transform, def.gainText, pos + new Vector2(0f, 60f), def.color, 48);
                RefreshBalances();
            });

            bar.fillAmount = Upgrades.Level(def.id) / (float)def.maxLevel;
            Refresh();
            refreshers.Add(Refresh);
        }

        // ---------------- Decor card ----------------

        void DecorCard(DecorDef def, Vector2 pos, Vector2 size)
        {
            var card = UIKit.Box(cardsRoot, "Decor_" + def.id, new Vector2(0.5f, 0.5f), pos, size, UIKit.PanelLight);
            var bg = card.GetComponent<Image>();
            var t = card.transform;
            var badge = UIKit.Disc(t, new Vector2(0f, 1f), new Vector2(64f, -64f), 86f, def.color);
            UIKit.Text(badge.transform, def.badge, 26, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, false, 86f).color = UIKit.Ink;
            UIKit.Text(t, def.name, 32, new Vector2(0f, 1f), new Vector2(122f, -30f), TextAnchor.UpperLeft, true, 400f);
            var blurb = UIKit.Text(t, def.blurb, 22, new Vector2(0f, 1f), new Vector2(122f, -74f), TextAnchor.UpperLeft, false, 390f);
            blurb.color = UIKit.Muted;
            blurb.horizontalOverflow = HorizontalWrapMode.Wrap;
            ((RectTransform)blurb.transform).sizeDelta = new Vector2(390f, 60f);

            Text label;
            var btn = UIKit.Button(t, "", new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(size.x - 50f, 64f), UIKit.Green, null, out label, 28);

            void Refresh()
            {
                bool owned = DecorCatalog.Owned(def.id);
                if (owned)
                {
                    bool shown = DecorCatalog.Visible(def.id);
                    btn.interactable = true;
                    label.text = shown ? "SHOWING  (tap to hide)" : "HIDDEN  (tap to show)";
                    btn.GetComponent<Image>().color = shown ? UIKit.Cyan : new Color(0.45f, 0.45f, 0.55f);
                }
                else
                {
                    bool afford = SaveSystem.Data.totalCoins >= def.cost;
                    btn.interactable = afford;
                    label.text = "BUY  -  " + def.cost.ToString("N0") + " COINS";
                    btn.GetComponent<Image>().color = afford ? UIKit.Green : new Color(0.4f, 0.42f, 0.5f);
                }
            }

            btn.onClick.AddListener(() =>
            {
                if (DecorCatalog.Owned(def.id))
                {
                    DecorCatalog.SetVisible(def.id, !DecorCatalog.Visible(def.id));
                    Refresh();
                    return;
                }
                if (!DecorCatalog.TryBuy(def)) { AudioFX.Play(Sfx.Out, 0.4f, 1.4f); return; }
                AudioFX.Play(Sfx.SuperReady, 0.8f, 1.1f);
                Flash.On(bg, Color.Lerp(def.color, Color.white, 0.4f));
                ScalePunch.On(card.transform, 0.08f);
                FloatUp.Spawn(transform, def.name + " ADDED!", pos + new Vector2(0f, 50f), def.color, 40);
                RefreshBalances();
            });

            Refresh();
            refreshers.Add(Refresh);
        }

        // ---------------- Store (real money, optional) ----------------

        void BuildStore()
        {
            var list = Purchases.Catalog;
            for (int i = 0; i < list.Count; i++) ProductCard(list[i], GridPos(i, list.Count, 3, new Vector2(560f, 345f)) + new Vector2(0f, 60f), new Vector2(530f, 320f));

            var note = UIKit.Text(cardsRoot, Purchases.IsTestStore
                    ? "TEST STORE - purchases are simulated in the Editor and development builds."
                    : "Everything in Treadmill Mayhem can be earned by playing. Purchases are optional.",
                24, new Vector2(0.5f, 0f), new Vector2(0f, 200f), TextAnchor.MiddleCenter, false);
            note.color = UIKit.Muted;

            Text restoreLabel;
            var restore = UIKit.Button(cardsRoot, "RESTORE PURCHASES", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(420f, 74f), new Color(0.45f, 0.5f, 0.95f), null, out restoreLabel, 28);
            restore.onClick.AddListener(() =>
            {
                restore.interactable = false;
                restoreLabel.text = "RESTORING...";
                Purchases.Restore(ok =>
                {
                    restore.interactable = true;
                    restoreLabel.text = ok ? "RESTORED!" : "RESTORE FAILED - TRY AGAIN";
                    RefreshBalances();
                });
            });
        }

        void ProductCard(ProductDef def, Vector2 pos, Vector2 size)
        {
            var card = UIKit.Box(cardsRoot, "Product_" + def.id, new Vector2(0.5f, 0.5f), pos, size, UIKit.PanelLight);
            var bg = card.GetComponent<Image>();
            var t = card.transform;
            var badge = UIKit.Disc(t, new Vector2(0f, 1f), new Vector2(70f, -70f), 96f, def.color);
            UIKit.Text(badge.transform, def.badge, 28, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, false, 96f).color = UIKit.Ink;
            UIKit.Text(t, def.name, 34, new Vector2(0f, 1f), new Vector2(135f, -40f), TextAnchor.UpperLeft, true, 380f);
            var blurb = UIKit.Text(t, def.blurb, 26, new Vector2(0f, 1f), new Vector2(26f, -150f), TextAnchor.UpperLeft, false, 480f);
            ((RectTransform)blurb.transform).sizeDelta = new Vector2(480f, 80f);
            blurb.horizontalOverflow = HorizontalWrapMode.Wrap;

            Text label;
            var btn = UIKit.Button(t, "", new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(size.x - 50f, 70f), UIKit.Green, null, out label, 30);

            void Refresh()
            {
                bool owned = def.kind == ProductKind.NonConsumable && Purchases.Owns(def.id);
                bool available = Purchases.StoreAvailable;
                btn.interactable = !owned && available;
                label.text = owned ? "OWNED" : available ? "BUY  -  " + Purchases.Price(def) : "STORE UNAVAILABLE";
                btn.GetComponent<Image>().color = owned ? UIKit.Cyan : available ? UIKit.Green : new Color(0.4f, 0.42f, 0.5f);
            }

            btn.onClick.AddListener(() =>
            {
                btn.interactable = false;
                Purchases.Buy(def, ok =>
                {
                    if (ok)
                    {
                        AudioFX.Play(Sfx.SuperReady, 0.8f, 1.1f);
                        Flash.On(bg, Color.Lerp(def.color, Color.white, 0.4f));
                        ScalePunch.On(card.transform, 0.08f);
                        FloatUp.Spawn(transform, def.coins > 0 ? "+" + def.coins.ToString("N0") + " COINS!" : "THANK YOU!", pos + new Vector2(0f, 60f), def.color, 44);
                    }
                    else AudioFX.Play(Sfx.Out, 0.4f, 1.4f);
                    RefreshBalances();
                });
            });

            Refresh();
            refreshers.Add(Refresh);
        }
    }
}
