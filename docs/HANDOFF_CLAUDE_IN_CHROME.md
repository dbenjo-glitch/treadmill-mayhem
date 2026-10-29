# Prompt for Claude in Chrome: publish Treadmill Mayhem

Paste everything below the line into Claude in Chrome.

---

Help me publish my Unity WebGL game "Treadmill Mayhem" for my AME 294 class (PG3). I'm signed in myself; stop and ask me whenever a sign-in, a captcha or a file picker comes up, and I'll handle it. Ask me before clicking any final Publish or Save button.

**1. Itch.io (extra credit)** at https://itch.io/game/new

- Title: Treadmill Mayhem
- Project URL: treadmill-mayhem
- Short description: Stay on the belt. Shove everyone else off.
- Classification: Games
- Kind of project: HTML
- Uploads: I'll pick `C:\Users\David\My project\Builds\TreadmillMayhem_WebGL.zip` (117 MB). Once it finishes, tick "This file will be played in the browser".
- Embed options:
  - Viewport 1280 × 720
  - Tick "Fullscreen button"
  - Leave "Mobile friendly" and "SharedArrayBuffer support" unticked
- Description: paste the text from the "Description" section below.
- Genre: Action. Tags: 3d, physics, arcade, funny, unity, ai-generated
- AI generation disclosure: Yes (code, 3D models and images were made with AI tools).
- Cover image: I'll pick `C:\Users\David\My project\docs\itch_cover.png`.
- Screenshots: I'll pick the 4 files in `C:\Users\David\My project\docs\screenshots\` (main_menu.jpg, race.jpg, results.jpg, tripo_racers.png).
- Visibility: Public. Then save, open the game page, and confirm it loads and the main menu appears (the first load takes a while, ~117 MB).

**2. GitHub Release** at https://github.com/dbenjo-glitch/treadmill-mayhem/releases/new

- Tag: v1.0 (create it on publish), target: main
- Title: Treadmill Mayhem v1.0 (WebGL build)
- Notes: "Playable WebGL build. Unzip and serve the folder with any static web server, or play it on itch.io: [itch link]. Every Tripo texture is kept at full 4096 px, compressed."
- Attach: I'll pick `C:\Users\David\My project\Builds\TreadmillMayhem_WebGL.zip`.
- Publish the release, and tell me the release URL.

**3. Unity Play (optional)** at https://play.unity.com: sign in (I'll do it), choose to upload a WebGL game, and upload the same zip. Title "Treadmill Mayhem". Tell me the link.

**4. When done:** give me the itch.io link, the GitHub Release link and the Unity Play link, so they can go into the README.

## Description (for the itch.io description box)

**Treadmill Mayhem**

A tuna can, five rival groceries and one treadmill that keeps getting faster. Roll, shove and boost to be the last one standing before the belt throws you off the back.

**How to play**

- Roll: WASD or arrow keys
- Stamina boost: hold Shift
- Super Speed: Space, when the bar is full
- Pause: Esc or P

Grab stamina orbs, roll in a straight line to hit ACCELERATION, and charge Super Speed every 2,500 points. Don't camp in a corner: hidden zones warn you after 5 seconds and knock you out at 7.

**Modes**

- Race: you against five AI racers, at 5 difficulty levels from Easy to Insane.
- Obstacle: Free Play: dodge waves of junk for as long as you can.
- Obstacle: Levels: 12 levels from "Warm Up" to "Tuna Legend".

Earn Tuna Coins to buy upgrades, unlock 12 characters, decorate the arena and chase 14 achievements. Progress saves in your browser.

The first load downloads about 117 MB and works best in Chrome or Edge on a computer.

Made for AME 294: Games and AI (Arizona State University) by David Benjo. Built in Unity 6 with Claude Code; all 12 racers and the treadmill are 3D models made with Tripo 3D.
