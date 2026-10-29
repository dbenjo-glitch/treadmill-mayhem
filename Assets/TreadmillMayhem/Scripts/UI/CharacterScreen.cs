using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>
    /// Character select: a portrait grid on the left, and a detail panel on the right with stats and the
    /// unlock / select button. Unlocks cost Tuna Coins; the chosen character is used in every mode.
    /// </summary>
    public class CharacterScreen : MonoBehaviour
    {
        public Action Closed;

        class Tile { public string name; public Image frame, portrait; public GameObject lockGo, playing; public Text price; }

        readonly List<Tile> tiles = new List<Tile>();
        List<GameObject> prefabs = new List<GameObject>();
        string viewing;

        Image bigPortrait;
        Text nameText, shapeText, blurbText, actionLabel, coinText;
        Button actionButton;
        Image[] statFills;
        RectTransform grid;

        public static CharacterScreen Build(Transform canvas)
        {
            var root = UIKit.Backdrop(canvas, "Characters", 0.97f);
            var c = root.AddComponent<CharacterScreen>();
            c.Construct();
            root.SetActive(false);
            return c;
        }

        void Construct()
        {
            var t = transform;
            UIKit.Text(t, "CHARACTERS", 84, new Vector2(0.5f, 1f), new Vector2(0f, -28f), TextAnchor.UpperCenter).color = UIKit.Gold;
            coinText = UIKit.CurrencyPill(t, new Vector2(1f, 1f), new Vector2(-30f, -30f), Currency.Coins, out _);
            UIKit.Button(t, "BACK", new Vector2(0f, 1f), new Vector2(150f, -75f), new Vector2(220f, 80f), new Color(0.75f, 0.75f, 0.85f), Close, 34);

            grid = (RectTransform)UIKit.Node(t, "Grid").transform;
            grid.anchorMin = grid.anchorMax = new Vector2(0.5f, 0.5f);
            grid.anchoredPosition = new Vector2(-430f, -50f);

            var panel = UIKit.Box(t, "Detail", new Vector2(0.5f, 0.5f), new Vector2(470f, -50f), new Vector2(760f, 800f), UIKit.Panel).transform;
            bigPortrait = UIKit.Box(panel, "Portrait", new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(300f, 300f), Color.white).GetComponent<Image>();
            bigPortrait.preserveAspect = true;
            bigPortrait.type = Image.Type.Simple;
            nameText = UIKit.Text(panel, "", 60, new Vector2(0.5f, 1f), new Vector2(0f, -340f), TextAnchor.UpperCenter);
            shapeText = UIKit.Text(panel, "", 26, new Vector2(0.5f, 1f), new Vector2(0f, -412f), TextAnchor.UpperCenter, false);
            shapeText.color = UIKit.Cyan;
            blurbText = UIKit.Text(panel, "", 26, new Vector2(0.5f, 1f), new Vector2(0f, -450f), TextAnchor.UpperCenter, false, 660f);
            blurbText.horizontalOverflow = HorizontalWrapMode.Wrap;
            ((RectTransform)blurbText.transform).sizeDelta = new Vector2(660f, 70f);

            string[] labels = { "SPEED", "POWER", "GRIP", "CONTROL" };
            Color[] colors = { UIKit.Orange, UIKit.Red, UIKit.Green, UIKit.Cyan };
            statFills = new Image[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float y = -535f - i * 40f;
                var l = UIKit.Text(panel, labels[i], 22, new Vector2(0.5f, 1f), new Vector2(-270f, y), TextAnchor.MiddleLeft, false, 160f);
                l.color = UIKit.Muted;
                statFills[i] = UIKit.Bar(panel, new Vector2(0.5f, 1f), new Vector2(95f, y), new Vector2(430f, 16f), colors[i]);
            }

            actionButton = UIKit.Button(panel, "", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(560f, 86f), UIKit.Green, OnAction, out actionLabel, 36);

            SaveSystem.CurrencyChanged += Refresh;
        }

        void OnDestroy() => SaveSystem.CurrencyChanged -= Refresh;

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (tiles.Count == 0) BuildTiles();
            View(Characters.Selected);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        void BuildTiles()
        {
            prefabs = Characters.AllPrefabs(GameManager.Instance);
            const int cols = 4;
            const float cell = 205f;
            int rows = Mathf.CeilToInt(prefabs.Count / (float)cols);
            for (int i = 0; i < prefabs.Count; i++)
            {
                var name = prefabs[i].name;
                int r = i / cols, c = i % cols;
                var pos = new Vector2((c - (cols - 1) * 0.5f) * cell, ((rows - 1) * 0.5f - r) * cell);
                var tile = new Tile { name = name };
                var frame = UIKit.Box(grid, "Tile_" + name, new Vector2(0.5f, 0.5f), pos, new Vector2(190f, 190f), UIKit.PanelLight, true);
                tile.frame = frame.GetComponent<Image>();
                var portrait = UIKit.Box(frame.transform, "Portrait", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(168f, 168f), Color.white);
                tile.portrait = portrait.GetComponent<Image>();
                tile.portrait.sprite = Characters.Portrait(name);
                if (tile.portrait.sprite == null) tile.portrait.color = Color.Lerp(prefabs[i].GetComponent<Racer>().definition.color, Color.white, 0.3f);
                tile.portrait.preserveAspect = true;
                tile.portrait.type = Image.Type.Simple;
                tile.portrait.raycastTarget = false;

                tile.lockGo = UIKit.Box(frame.transform, "Lock", new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(168f, 46f), new Color(0f, 0f, 0f, 0.7f));
                tile.price = UIKit.Text(tile.lockGo.transform, "", 24, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, false, 168f);
                tile.price.color = UIKit.Gold;
                tile.playing = UIKit.Box(frame.transform, "Playing", new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(168f, 36f), UIKit.Green);
                UIKit.Text(tile.playing.transform, "PLAYING", 20, new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, false, 168f).color = UIKit.Ink;

                var btn = frame.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => { AudioFX.Play(Sfx.Click, 0.6f); View(name); });
                frame.AddComponent<PressBounce>();
                tiles.Add(tile);
            }
        }

        void View(string name)
        {
            viewing = name;
            var prefab = prefabs.Find(p => p.name == name);
            var def = prefab ? prefab.GetComponent<Racer>().definition : null;
            bigPortrait.sprite = Characters.Portrait(name);
            nameText.text = name.ToUpperInvariant();
            nameText.color = def ? Color.Lerp(def.color, Color.white, 0.3f) : Color.white;
            shapeText.text = def ? Characters.ShapeLabel(def.shape) : "";
            blurbText.text = Characters.Blurb(name);
            var (speed, power, grip, control) = Characters.Ratings(def);
            float[] v = { speed, power, grip, control };
            for (int i = 0; i < statFills.Length; i++) BarFill.To(statFills[i], Mathf.Lerp(0.08f, 1f, v[i]));
            ScalePunch.On(bigPortrait.transform, 0.08f);
            Refresh();
        }

        void Refresh()
        {
            if (coinText) coinText.text = SaveSystem.Data.totalCoins.ToString("N0");
            foreach (var t in tiles)
            {
                bool owned = Characters.Owned(t.name);
                t.lockGo.SetActive(!owned);
                t.price.text = Characters.Price(t.name).ToString("N0") + " COINS";
                t.portrait.color = owned ? Color.white : new Color(0.55f, 0.55f, 0.6f);
                t.playing.SetActive(t.name == Characters.Selected);
                t.frame.color = t.name == viewing ? UIKit.Gold : UIKit.PanelLight;
            }
            if (viewing == null) return;

            bool own = Characters.Owned(viewing);
            int price = Characters.Price(viewing);
            int coins = SaveSystem.Data.totalCoins;
            var img = actionButton.GetComponent<Image>();
            if (own && viewing == Characters.Selected)
            {
                actionLabel.text = "SELECTED";
                img.color = UIKit.Cyan;
                actionButton.interactable = false;
            }
            else if (own)
            {
                actionLabel.text = "PLAY AS " + viewing.ToUpperInvariant();
                img.color = UIKit.Green;
                actionButton.interactable = true;
            }
            else if (coins >= price)
            {
                actionLabel.text = "UNLOCK  -  " + price.ToString("N0") + " COINS";
                img.color = UIKit.Gold;
                actionButton.interactable = true;
            }
            else
            {
                actionLabel.text = "NEED " + (price - coins).ToString("N0") + " MORE COINS";
                img.color = new Color(0.4f, 0.42f, 0.5f);
                actionButton.interactable = false;
            }
        }

        void OnAction()
        {
            if (viewing == null) return;
            if (Characters.Owned(viewing))
            {
                Characters.Select(viewing);
                AudioFX.Play(Sfx.Pickup, 0.7f);
            }
            else if (Characters.TryBuy(viewing))
            {
                AudioFX.Play(Sfx.SuperReady, 0.8f, 1.1f);
                ScalePunch.On(bigPortrait.transform, 0.25f);
                FloatUp.Spawn(transform, viewing.ToUpperInvariant() + " UNLOCKED!", new Vector2(470f, 250f), UIKit.Gold, 48);
            }
            else
            {
                AudioFX.Play(Sfx.Out, 0.4f, 1.4f);
                return;
            }
            Refresh();
        }
    }
}
