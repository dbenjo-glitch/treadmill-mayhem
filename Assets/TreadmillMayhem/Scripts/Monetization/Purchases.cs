using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreadmillMayhem
{
    public enum ProductKind { NonConsumable, Consumable }

    /// <summary>A real-money product. <see cref="id"/> must match App Store Connect / Google Play exactly.</summary>
    public class ProductDef
    {
        public string id, name, blurb, badge, fallbackPrice;
        public ProductKind kind;
        public int coins;
        public Color color;
    }

    /// <summary>
    /// The store a build talks to. A Unity IAP adapter implements this and calls <see cref="Purchases.SetStore"/>
    /// at startup; the game never references the SDK directly.
    /// </summary>
    public interface IStoreProvider
    {
        bool Ready { get; }
        /// <summary>Localised price string from the store (e.g. "$2.99"), or null if unknown.</summary>
        string PriceOf(string productId);
        void Buy(string productId, Action<bool> onDone);
        void Restore(Action<bool> onDone);
    }

    /// <summary>
    /// In-app purchases. Ownership of non-consumables is persisted locally and re-granted by Restore.
    /// Purchases are entirely optional: everything in the game can be earned by playing.
    /// </summary>
    public static class Purchases
    {
        public const string RemoveAds = "treadmillmayhem.removeads";
        public const string CoinsSmall = "treadmillmayhem.coins.small";
        public const string CoinsLarge = "treadmillmayhem.coins.large";

        public static readonly List<ProductDef> Catalog = new List<ProductDef>
        {
            new ProductDef { id = RemoveAds, name = "REMOVE ADS", badge = "NO AD", kind = ProductKind.NonConsumable, fallbackPrice = "$2.99",
                             color = new Color(1f, 0.45f, 0.6f), blurb = "No more between-run ads, forever. Rewarded bonuses stay available." },
            new ProductDef { id = CoinsSmall, name = "BAG OF COINS", badge = "1K", kind = ProductKind.Consumable, coins = 1000, fallbackPrice = "$0.99",
                             color = UIKit.Gold, blurb = "1,000 Tuna Coins for upgrades and decor." },
            new ProductDef { id = CoinsLarge, name = "CRATE OF COINS", badge = "6K", kind = ProductKind.Consumable, coins = 6000, fallbackPrice = "$4.99",
                             color = new Color(1f, 0.65f, 0.25f), blurb = "6,000 Tuna Coins - the best value." },
        };

        public static event Action Changed;

        static IStoreProvider store;

        /// <summary>Called by a store adapter (Unity IAP) once it has initialised.</summary>
        public static void SetStore(IStoreProvider provider)
        {
            store = provider;
            Changed?.Invoke();
        }

        /// <summary>The live store, or the test store in the Editor / development builds.</summary>
        static IStoreProvider Store
        {
            get
            {
                if (store != null) return store;
                if (Application.isEditor || Debug.isDebugBuild) store = new TestStore();
                return store;
            }
        }

        public static bool StoreAvailable => Store != null && Store.Ready;
        public static bool IsTestStore => Store is TestStore;

        public static bool HasRemoveAds => Owns(RemoveAds);

        public static bool Owns(string productId) => PlayerPrefs.GetInt("TM.Owned." + productId, 0) == 1 ||
                                                    (productId == RemoveAds && PlayerPrefs.GetInt("TM.RemoveAds", 0) == 1);

        public static string Price(ProductDef p)
        {
            var s = Store != null ? Store.PriceOf(p.id) : null;
            return string.IsNullOrEmpty(s) ? p.fallbackPrice : s;
        }

        public static ProductDef Find(string id) => Catalog.Find(p => p.id == id);

        public static void Buy(ProductDef p, Action<bool> onDone)
        {
            if (!StoreAvailable || (p.kind == ProductKind.NonConsumable && Owns(p.id))) { onDone?.Invoke(false); return; }
            Store.Buy(p.id, ok =>
            {
                if (ok) Grant(p.id);
                onDone?.Invoke(ok);
            });
        }

        public static void Restore(Action<bool> onDone)
        {
            if (!StoreAvailable) { onDone?.Invoke(false); return; }
            Store.Restore(onDone);
        }

        /// <summary>
        /// Delivers a purchased product. Store adapters call this for restored and deferred (Ask to Buy)
        /// transactions too, so it must be idempotent for non-consumables.
        /// </summary>
        public static void Grant(string productId)
        {
            var p = Find(productId);
            if (p == null) return;
            if (p.kind == ProductKind.NonConsumable)
            {
                PlayerPrefs.SetInt("TM.Owned." + productId, 1);
                PlayerPrefs.Save();
            }
            else if (p.coins > 0)
            {
                SaveSystem.AddCoins(p.coins);
            }
            Changed?.Invoke();
        }

        /// <summary>Instant-approve store for the Editor and development builds, so the flow can be tested.</summary>
        class TestStore : IStoreProvider
        {
            public bool Ready => true;
            public string PriceOf(string productId) => null;
            public void Buy(string productId, Action<bool> onDone)
            {
                Debug.Log("[TestStore] Simulated purchase of " + productId);
                onDone?.Invoke(true);
            }
            public void Restore(Action<bool> onDone)
            {
                Debug.Log("[TestStore] Restore: nothing to restore beyond local ownership.");
                onDone?.Invoke(true);
            }
        }
    }
}
