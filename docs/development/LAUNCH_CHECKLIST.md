# Treadmill Mayhem: App Store launch checklist

The project is configured for release. Everything below needs **your** accounts, money, or a Mac, so it can't be done from inside the project.

## Already done in the project
- **Identity:** Tuna Can Studios / Treadmill Mayhem, version 1.0.0 (build 1), bundle id `com.tunacanstudios.treadmillmayhem`.
  - To change any of these, edit `Assets/TreadmillMayhem/Editor/StoreSetup.cs`, then run **Tools ▸ Treadmill Mayhem ▸ Apply Store Settings**.
- **Orientation:** landscape only. Full screen with the status bar hidden. Edge swipes are deferred, so a swipe doesn't trigger the home gesture mid-run.
- **iOS:** IL2CPP, iOS 15+, iPhone and iPad. App icon at 1024×1024 with no alpha (`Assets/TreadmillMayhem/Branding/AppIcon.png`).
- **Info.plist (written at build time):**
  - App Tracking Transparency prompt text
  - `ITSAppUsesNonExemptEncryption = NO`
- **Android:** IL2CPP, ARM64, minimum API level 26.
- **Mobile controls:**
  - dual-thumb touch controls (stick on the left; BOOST and SUPER on the right)
  - safe-area layout for notches
  - aspect-aware UI and camera, so iPad 4:3 and wide phones both work
  - 60 fps, screen never sleeps mid-run
- **Monetization:**
  - Code seams: `Scripts/Monetization/Ads.cs` and `Purchases.cs`
  - In-game STORE tab with Restore Purchases
  - Optional rewarded ads: ×2 offline earnings, ×2 run coins
  - Interstitials: only between runs, every 4th run, never in the first 3 runs, never after Remove Ads
  - In the Editor and development builds, a TEST STORE simulates purchases.

## You need to do

### Accounts
1. **Apple Developer Program** ($99/yr): developer.apple.com.
2. **App Store Connect:** create the app using the bundle id above (or your own; update `StoreSetup.cs` to match).

### Build tools
3. **iOS Build Support:** Unity Hub ▸ Installs ▸ 6000.6.7f1 ▸ Add modules ▸ *iOS Build Support*. Add *Android Build Support* too if you want Google Play.
4. **Building for iOS needs a Mac with Xcode.** In Unity: File ▸ Build Profiles ▸ iOS ▸ Build, which produces an Xcode project. Open it on a Mac, sign it with your team, then Archive ▸ Upload.
   - No Mac? Use **Unity Build Automation** or **Codemagic** to build and upload from the cloud.

### In-app purchases
5. **Create the products in App Store Connect** with these exact IDs:

   | Product ID | Type | Price |
   |---|---|---|
   | `treadmillmayhem.removeads` | Non-Consumable | $2.99 |
   | `treadmillmayhem.coins.small` | Consumable (1,000 coins) | $0.99 |
   | `treadmillmayhem.coins.large` | Consumable (6,000 coins) | $4.99 |

6. **Unity IAP:**
   - Install it through Package Manager ▸ *In-App Purchasing*.
   - Write a small adapter that implements `IStoreProvider`. At startup it calls `Purchases.SetStore(adapter)`, and for each completed or restored transaction it calls `Purchases.Grant(productId)`.

### Ads
7. **Ads (optional), with LevelPlay / Unity Ads:**
   - Create the app in the LevelPlay dashboard and get the app key.
   - Install *Ads Mediation*.
   - In the adapter, set `Ads.RewardedReady`, `Ads.RewardedShow` and `Ads.InterstitialShow`.
   - The "WATCH AD ×2" buttons appear automatically once rewarded ads are ready.

### Store listing
8. **Privacy:**
   - Host a privacy policy URL.
   - Fill in App Privacy "nutrition labels". Without ads: *Data Not Collected*. With ads: identifiers and usage data used for advertising.
9. **Store listing:**
   - Screenshots for 6.7" and 6.5" iPhone and 13" iPad, in landscape. Play in the Editor at those resolutions and capture them.
   - Description, keywords, and age rating (cartoon slapstick, 4+).
   - Support URL.
10. **TestFlight:** upload, test on a real device, then submit for review.

## Before every release
- Bump `Version` in `StoreSetup.cs`, bump the iOS build number, then run **Apply Store Settings**.
- Run **Tools ▸ Treadmill Mayhem ▸ Build Scene** if models or tuning changed.
- Playtest Race, Obstacle Free Play, and Obstacle Levels on a phone.
