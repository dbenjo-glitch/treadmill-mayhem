# Master AI Prompt Log & Workflow Analysis

**Course:** AME 294: Games and AI, Creating Games with Artificial Intelligence
**Assignment:** Portfolio Game 3, Unreal or Unity, Built with AI
**Student Name:** David Benjo
**Project Title:** Treadmill Mayhem
**Engine:** Unity 6000.6.7f1 (URP)
**Repository URL:** [ADD AFTER CREATING THE REPO]
**Itch.io URL (Bonus):** [ADD AFTER UPLOAD]

---

## 1. Toolchain & AI Session Inventory

| Category | Primary Tool / Platform | Model Version / Specification | Purpose in Project |
| :---- | :---- | :---- | :---- |
| **Coding agent** | Claude Code (Code tab of the Claude desktop app) | claude-opus-5-5 | All C# scripts, scene building, UI, physics tuning, builds |
| **Engine bridge** | Unity MCP server from the Unity AI Assistant package (`com.unity.ai.assistant`) | Package 2.21.0-pre.1 | Let Claude run editor commands, read the console, enter Play mode and build |
| **Desktop control** | Claude computer use | claude-opus-5-5 | Focusing the Unity window, clicking through menus, taking gameplay screenshots |
| **Playtesting** | 50 scripted bot players written by Claude (`Scripts/Testing/`) | | Balance data, bug finding, difficulty checks |
| **3D asset generator** | Tripo 3D (tripo3d.ai, web app) | [STUDENT: Tripo model version] | All 12 racer models and the treadmill (.glb) |
| **Reference images** | ChatGPT image generation | [STUDENT: model] | Reference pictures for the first 7 models, fed to Tripo image-to-3D |
| **Audio** | None. Synthesized in code by `Scripts/AudioFX.cs` | | Coin, click, orb, acceleration, super speed, cheer, camera shutter and fall sounds |
| **Trailer** | [STUDENT: tools used] | | 30–60 s trailer |
| **Presentation** | Claude (Slides artifact) | claude-opus-5-5 | Demo Day slides |

Blender and ElevenLabs were not used.

**Totals:** 41 prompts to the coding agent over two sessions (Oct 21 and Oct 28, 2026), about 8 hours of agent work: roughly 7 hours of building and testing, plus about an hour of bot playtests running in the background.

---

## 2. Code Development Prompts (Reward, Damage, End Loop)

All prompts are the student's exact words, typos included, in the order they were sent. Prompts sent while Claude was already working are marked *(mid-task)*.

### 2.0 Connecting Claude to Unity (Oct 21)

#### Exact Prompts Submitted:

> do i need an mcp for you to work in unity

> do i need to use unity AI and pay for that subscription?

> how do i connect my unity to my claude

> place a block at 0,0,0

> list every actor

> Spawn a cube at world origin. Next step: move it to (300, 0, 100) and scale it 2x. Check the viewport each time.

#### Agent Output Summary & Key Changes:

* Claude walked the student through connecting Claude Code to the Unity editor over MCP, using the MCP server in the Unity AI Assistant package.
* The block, actor list and cube prompts were connection tests. They confirmed Claude could create, list and move objects in the open scene.

### 2.1 The Design Prompt and Initial Scaffolding

* **Agent Used:** Claude Code
* **Target Objective:** Build the whole game from the student's design document, using the student's Tripo models.

#### Exact Prompt Submitted:

The student pasted a long design document. As recorded in the session (the paste was cut off by the chat at this point), followed by the student's note:

<details>
<summary>Show the design prompt</summary>

> PROJECT: TREADMILL MAYHEM — 3D PHYSICS SURVIVAL GAME You are an expert Unity game developer, gameplay programmer, physics engineer, and mobile game designer. Your task is to BUILD a fully playable, polished 3D Unity arcade game inspired by the viral Instagram treadmill races where cans, fruits, and balls compete to see who can stay on a moving treadmill the longest. The visual style should be inspired by Ketchapp-style hypercasual games: bright colors, simple stylized 3D graphics, polished animations, satisfying physics, smooth controls, minimal clutter, and highly addictive gameplay. IMPORTANT: Do not simply produce a game design document. Implement the actual game, scripts, prefabs, UI, scene setup, and gameplay systems in the Unity project. Work autonomously, make reasonable design decisions, test as much as the available environment allows, and prioritize a playable, polished single-player MVP. 1. CORE GAME CONCEPT The player controls a rolling tuna can racing against five AI-controlled objects on a continuously moving treadmill. The objective is simple: Stay on the treadmill as long as possible. The treadmill continuously moves backward, forcing all racers toward its rear edge. Players must roll, steer, avoid obstacles, collide with opponents, collect power-ups, and use momentum strategically. As time progresses, the treadmill gets progressively faster, making survival increasingly difficult. The game ends when the player falls off the treadmill. Other racers can be eliminated through collisions, mistakes, or falling off the treadmill. The game should be fun even without multiplayer, with realistic-feeling but deliberately exaggerated arcade physics. Target platform: PC for initial development, with the architecture designed for future iOS and Android deployment. Use a stable Unity LTS version installed in the environment, the Universal Render Pipeline (URP), C#, and Unity's built-in physics system. 2. PLAYER CHARACTER The player's character is always a TUNA CAN in Version 1. Appearance: * A short, wide cylindrical tuna can. * Bright blue outer label. * Simple white fish icon. * Silver metal lid and bottom. * Rounded edges. * Slightly glossy, cartoon-stylized materials. * No face, arms, or legs. * The can lies horizontally on its curved side so it can roll naturally. The tuna can should have a Rigidbody
>
> in my files my most recents all from between 12:40 and 1pm today 10/21/2026 you will find all the charachters for the game and all the objects, if there are no default people with cameras i can design them in tripo

</details>

> did you get the tripo 3d files? *(mid-task)*

> @"C:\Users\David\Downloads\treadmill_parts.glb" @"C:\Users\David\Downloads\treadmill.glb" @"C:\Users\David\Downloads\cola.glb" @"C:\Users\David\Downloads\ball.glb" @"C:\Users\David\Downloads\beans.glb" @"C:\Users\David\Downloads\tuna.glb" @"C:\Users\David\Downloads\apple.glb" @"C:\Users\David\Downloads\orange.glb" these are the files you need, make sure you used these files, do a check, dont just re import them *(mid-task)*

#### Agent Output Summary & Key Changes:

* Claude wrote an editor scene builder (`Editor/TreadmillMayhemSetup.cs`) that builds the whole scene from code: the treadmill, the arena, the racers, the camera, the UI and the configs. Rebuilding the scene is one menu click, so any change can be re-applied cleanly.
* The treadmill is a kinematic conveyor: the belt pushes every racer backward each physics step, and its speed rises over time.
* Each racer is a Rigidbody with its own stat sheet (a ScriptableObject in `Config/Racers/`): size, mass, grip, bounce, acceleration, wobble and knockback.
* **Human check:** the student made Claude verify it was using his exact Tripo files, not re-imported copies. Claude compared file hashes for every model.

### 2.2 Core Loop Mechanics

#### Exact Prompts Submitted:

> use the button on the treadmill as a start game button. so in the main menu the start button should be a yellow circle wich looks 3d ish and then you press it down and it zooms out of the button and into focus on the tuna can showing the enviroment and the treadmill *(mid-task)*

> do the start menu last, for now focus on the game, making it better, adding the backround, fixing bugs, making it look polished, etc *(mid-task)*

> how are things going

> work on it, its bugging out to much especially at the end of the round. it doesnt accept my command to restart game. also lets just havve a game over screen which allows you to record that run with a name, lets also add coins into the game, also make the treadmill 1.5x wide to allow more space and 1.5x long.

> also have it so you can save up coins and with those ventually buy new skins and items (this is for a later time though). *(mid-task)*

> Yes, build the start menu with the 3D yellow console button and the camera fly-out to the tuna.

> where is the stamina bar, also make the different difficultys

#### A. Reward Mechanic

* **Implementation Outcome:** +50 points per second survived, +100 per stamina orb, +250 per second while accelerating. Coins spawn on the belt. Stamina orbs fill a boost bar (hold Shift). Rolling straight for about 3 seconds triggers ACCELERATION. Every 2,500 points charges Super Speed (Space).
* Run records are saved with a player name to a local top-10 leaderboard.

#### B. Damage Mechanic

* **Implementation Outcome:** racers bump each other with real physics. Off-center hits make the can wobble and lose control for a moment. Obstacles roll down the belt and knock racers around.

#### C. End State & Win/Loss Conditions

* **Implementation Outcome:** falling off the back or sides of the treadmill is a loss. The last racer on the belt wins. A results screen shows score, time and the leaderboard, with a name field, and Play Again restarts instantly without reloading the editor.
* **Bug fixed:** the original game-over flow ignored the restart command. Claude rewrote the end-of-round state handling so Play Again and Main Menu always work.

#### D. Difficulty

* **Implementation Outcome:** five AI difficulty levels (Easy, Normal, Hard, Expert, Insane), each a config asset that changes reaction time, steering accuracy, obstacle awareness, pickup greed and how aggressively the AI shoves.

### 2.3 Anti-Camping Rule

#### Exact Prompts Submitted:

> make it so that no one can just hang in the corner not needing to move around *(mid-task)*

> the no camping zones should not be visible rather you should only get a warning after being 5 seconds in it, after 7 seconds you will lose, if you get 2 warnings you will also lose *(mid-task)*

* **Agent's first version:** visible zones in the corners that zapped racers out.
* **Human decision:** the student replaced that with his own rule: invisible zones, a warning at 5 seconds, out at 7 seconds, and a second warning also ends the run. Claude implemented it exactly as written.

### 2.4 Tripo Skins and the Arena

#### Exact Prompts Submitted:

> communicate with the claude chat/cowork and have it build mre tripo skins, i have a tab that has tripo open in the web, create skins for the objects we already have

> keep improving on everything we have spoken about for the next hour *(mid-task)*

> continue going i feel like we have to much backround in the game, like it takes up to much of the screen. add more detail to the backround though

> focus on just detailing the backround for the next hour *(mid-task)*

* **Outcome:** the camera was pulled in so the treadmill fills more of the screen. The arena gained a TV-studio look: bleachers with a crowd, ring lights, speakers, balloons, a stage truss, gym props and floor stickers. Non-moving decor is static-batched so it does not cost frame rate.

### 2.5 New Racers

#### Exact Prompts Submitted:

> @"C:\Users\David\Downloads\fish.glb" @"C:\Users\David\Downloads\breadcan.glb" @"C:\Users\David\Downloads\rollingpin.glb" @"C:\Users\David\Downloads\waterbottle.glb" here are some new models. make a ton of changes add them in, see how they prefrom configure from there

> @"C:\Users\David\Downloads\rollingpin_v2.glb" @"C:\Users\David\Downloads\onion.glb" @"C:\Users\David\Downloads\waterbottle_v2.glb" @"C:\Users\David\Downloads\papertowel.glb" @"C:\Users\David\Downloads\breadcan_v2.glb" @"C:\Users\David\Downloads\waterbottle_v3.glb" here is these as well, i have to go in 8 minutes *(mid-task)*

> so wrap up what you are doing *(mid-task)*

> and save the work *(mid-task)*

> Race and tune them

* **Outcome:** each model got a racer type that matches how it moves: Can (rolls on its side), Ball (rolls any direction) or Slider (the fish slides on its belly). Claude raced them against each other and tuned each one's stats until no racer was always first or always last (see incidents 4 and 5).

### 2.6 Progression, Obstacle Mode and Polish (Oct 28)

#### Exact Prompts Submitted:

> for the next 3 hours work on polishing the game into something people would want to play, after that i want you to start making it so that the mobile version is so good that people would download it from the appstore. make new modes, lets do obstacle mode (where objects come at you and you must dodge them, we can do a free play on that and also a levels mode). we can also make it so that with the coins you collect from the game you can uprgrade your backrounds, for example you can add a lamp in a corner, add led lights, maybe a disco ball, maybe a camera. lets also have offline rewards so that players can earn coins slowly even when they are not their. for the obstacle course with the levels lets include upgrades that only work with that and coins that only work with that, these uprgardes can be like increase speed level, increase treadmill lenghtt, increase treadmill width, increase money earned offline, etc. The things needed to properly make this game addictive. Also make the treadmill belt look like a real treadmill, right now it stops when it gets to the end, but make it actually go under the treadmill and around. here is a trial prompt, adjust it as needed to fit the game better, and to fit my prompt from above better

The student attached a second, detailed spec for the upgrade economy:

<details>
<summary>Show the upgrade-system spec</summary>

> You are working inside my existing Unity project: TUNA CAN TREADMILL SURVIVAL. Your task is to transform the existing game into an extremely satisfying, replayable mobile-style arcade game with permanent upgrades, progression, rewards, and polished UI. IMPORTANT: Inspect the existing Unity project, scripts, scenes, prefabs, physics, and game systems before changing anything. Work with what already exists. Do not rebuild the game from scratch, break working features, or replace the core gameplay.
>
> 1. EXISTING GAME MECHANICS. The player controls a tuna can trying to survive on an increasingly fast treadmill against AI-controlled objects. Existing gameplay includes: Move forward, backward, left, and right. Physics-based movement and collisions. Wobbling when partially hit by obstacles or opponents. AI opponents with 5 difficulty settings. A treadmill that progressively increases in speed. Collectible stamina orbs rolling backward. Stamina boosts that increase movement force. Acceleration after moving continuously in a straight line for 3 seconds without interruption. Speed boosts and a separate super-speed mechanic. EXISTING SCORING: +50 points per second survived. +100 points per speed boost collected. +250 points per second while acceleration is active. Players can earn access to stronger speed boosts through their score. Preserve the existing super-speed functionality. The player loses when they fall off the treadmill. Do not remove or fundamentally change these systems.
>
> 2. PERMANENT UPGRADE SHOP. Add an upgrade shop accessible from the main menu and after each run. Create these six upgrades: A. MOVEMENT SPEED: +4% movement speed per level, maximum 10 levels, never modify the treadmill's global speed. B. STAMINA CAPACITY: +8% maximum stamina per level, maximum 10 levels. C. ACCELERATION: each upgrade reduces activation time by 0.08 seconds, maximum 10 levels, minimum activation time 2.2 seconds. D. BALANCE / WOBBLE RESISTANCE: reduce collision wobble intensity by 5% per level, maximum 8 levels, do not eliminate collisions or remove the risk of falling. E. BOOST POWER: +5% boost effectiveness per level, maximum 8 levels, cap forces to prevent unrealistic movement. F. COIN MULTIPLIER: +10% coins per level, maximum 5 levels, does not multiply score. All upgrades must directly change gameplay, not just display numbers.
>
> 3. PROGRESSION ECONOMY. Add a permanent currency called TUNA COINS. Coins and score are SEPARATE. Base Coins = 20 + floor(Run Score / 20). Final Coins = Base Coins x Coin Multiplier. Starting upgrade costs: Movement Speed 120, Stamina 100, Acceleration 130, Balance 90, Boost Power 150, Coin Multiplier 200. Cost = Base Cost x (1.35 ^ Current Level). The first few upgrades should be achievable after approximately 1-3 normal runs.
>
> 4. SATISFYING UPGRADE SHOP UI. Each upgrade has a distinctive icon, name, current level, progress bar, current stat, next-level stat, cost and purchase button. On purchase: a positive sound, button animation, card glow, smooth progress bar, animated level number, a "+4% SPEED!" popup, instant coin update, and disabled unaffordable or maxed-out upgrades.
>
> 5. RESULTS SCREEN AND REPLAY LOOP. After every run show TUNA DOWN!, survival time, final score, best score, coins earned, total coins and the next upgrade goal with the exact amount still needed, [PLAY AGAIN] [UPGRADE TUNA] [MAIN MENU]. NEW HIGH SCORE! with a celebration. UPGRADE AVAILABLE! when affordable.
>
> 6. LONG-TERM GOALS. Achievements such as FIRST STEPS (survive 10 s), TUNA RUNNER (30 s), TREADMILL MASTER (60 s), SPEED DEMON (accelerated 15 s in a run), BOOST COLLECTOR (10 boosts in a run), UNSTOPPABLE TUNA (10,000 points), each with a one-time coin reward, saved permanently.
>
> 7. GAMEPLAY BALANCING. The game should become more exciting as the player progresses, not easier forever. Keep the 5 bot difficulty settings. Upgrades should not upgrade the opponents. No artificial waiting timers, forced ads, paywalls, or purchases with real money.
>
> 8. VISUAL AND AUDIO FEEDBACK. Animated coin counter, coin particles, acceleration effects, more impressive super speed effects, upgrade purchase animations.
>
> 9. SAVE SYSTEM. Persist coins, upgrade levels, high score, best survival time, achievements and claimed rewards. Clamp loaded values to valid ranges.
>
> 10. DEVELOPMENT REQUIREMENTS. Implement actual working Unity C# scripts, UI, event connections, and game logic. Inspect the project first, extend existing scripts instead of duplicating systems, test and correct errors, and end with a summary of features, files changed, setup steps, untested items and known problems.
>
> FINAL GOAL: PLAY → SURVIVE → EARN COINS → UPGRADE TUNA → FEEL STRONGER → BEAT HIGH SCORE → REPEAT.

(Condensed for length; the full text is in the session transcript. All numbers above are exactly as the student wrote them.)

</details>

> Spend as much time needed on all of this, work for at least 3 hours on this, if you need 10 hours, take ten hours, jsut however much time you need to make this be done well and professionally. *(mid-task)*

> after that prepare it to be able to be downloadable in the app store and be monetized, do this only once everything else is done. work for as long as needed, dont ask questions back to me, assume what you need to keep going *(mid-task)*

> continue for the next 3 hours

#### Implementation Outcome:

* **Reward:** the six coin upgrades exactly as specified, each wired into the real physics code (not just a number on screen). Tuna Coins use the student's formula. 14 achievements, a decor shop (lamp, LED lights, disco ball, TV camera and more), offline earnings for up to 8 hours, a 7-day daily reward calendar, and 12 unlockable characters.
* **Obstacle Mode:** Free Play plus 12 levels, from "Warm Up" (25 s) to "Tuna Legend" (60 s). Junk arrives in five wave patterns (singles, walls, rollers, rain, zigzags) with warning markers. Gears are its own currency, spent on six Obstacle-only upgrades: runner speed, treadmill length, treadmill width, offline earnings, an extra life and a gear magnet.
* **Real treadmill belt:** the stripes now run along a looping mesh that goes over the top, around the back roller, under the treadmill and back up the front.
* **Results screen:** "TUNA DOWN!", real calculated values, the exact coins still needed for the next upgrade, UPGRADE AVAILABLE and a new-high-score celebration.
* **Mobile:** touch stick plus BOOST and SUPER buttons, safe-area handling for notched phones, pause on app switch, haptics, and an Android back button.
* **App Store prep:** store listing text, privacy policy, a launch checklist and placeholder company details. Real in-app purchases and ads were not connected; they need the student's store accounts.

### 2.7 Playtesting with 50 Bots

#### Exact Prompts Submitted:

> have 50 agents with varying skill levels test it as well when its ready, spend an additional hour testing throughout the development of the game noting down thigns to add, bugs to fix, things to polish, etc, create a prompt explaining this, then send to yout self *(mid-task)*

> Yes, add grace

> i have to go in 3 minutes wrap up what you are doing *(mid-task)*

* **Outcome:** Claude wrote a playtest brief (`PLAYTEST_PROMPT.md`) and a bot player whose skill runs from first-timer to expert. Batches of bots played Race and Obstacle Mode in the editor and wrote reports (`PLAYTEST_*_REPORT.md`, notes in `PLAYTEST_NOTES.md`): survival times per racer, causes of death and frame rate.
* The bot data drove balance changes. For example, the Ball rival kept falling first, so its bounce and mass were retuned.
* **"Yes, add grace":** the bots showed first-time players dying in about 11 seconds. Claude proposed a start-of-run grace period and the student approved it: in a player's first 3 runs, falling off in the first 8 seconds rescues the tuna back onto the belt once.

### 2.8 Builds and Delivery (Oct 28, evening)

#### Exact Prompts Submitted:

> give me a playtest version within 2 minutes so i can send to my teacher to play test

> upload it to my school github accoiunt which is dbenjo@asu.edu *(mid-task)*

> slim it, but also find things in my c drive that i could uninstall in general that have been taking up space and that i dont really use, make a list of rme and ill approve what to delete and what not to. at the same time slim the game and make it so that i can upload it to github

> or can i just make a link to the game and send it to my teacher? *(mid-task)*

> make a copy and an original so that we dont accidently lose something improtant *(mid-task)*

> do it with the original version not the slimmed version as the slimemd version is worse *(mid-task)*

> how long would it take to get a unity play link

> do this what i put in your web, lets go for extra credit as well if possible

> dont shrink it *(mid-task)*

> is there some way we can do it wihtout shrinking it *(mid-task)*

> can we do a unity play link instead? *(mid-task)*

> you can compress things as needed, but dont over compress things, make sure thye run just as smooth and look just as good because with your first run the assets look bad, and the game wasnt good *(mid-task)*

> look at my past projects for this class as examples if needed for how to do this *(mid-task)*

* **Outcome:** see incident 7. The final WebGL build keeps every Tripo texture at full 4096 px, downloads 117 MB and ran at 60 FPS in testing.
* A full backup of the project was made before any build changes (`My project - ORIGINAL (before slimming)`), at the student's request.
* The repository files (this log, the README and the .gitignore) follow the format of the student's PG2 repository.

---

## 3. Tripo 3D Asset Pipeline

**Route:** generated directly at tripo3d.ai from the student's prompts, downloaded as `.glb`, and imported into Unity. Tripo was not driven over MCP.
**Credits:** [STUDENT: Tripo plan and credit balance before/after, if you have it]

**Two generation methods were used:**

1. **Image-to-3D (first 7 models, Oct 21, about 12:25–1:00 pm):** the student made front-on reference pictures in ChatGPT, then fed each picture to Tripo. The pictures are kept in `Assets/TreadmillMayhem/Reference/`.
2. **Text-to-3D (later models, about 3:00–3:30 pm):** written prompts, often run several times (`_v2`, `_v3` files).

**Shared style line.** Every prompt ended with the same sentence so the whole set matches:

> Simple stylized 3D game asset, soft realism, clean shapes, minimal detail, smooth materials, polished studio look, toy-like proportions, isolated object, no character face, no arms or legs, bright but controlled colors, consistent with casual mobile game art.

**Format pipeline into Unity (same for every asset):**

1. Downloaded from Tripo as `.glb` with PBR textures (base color, metallic-roughness and normal maps, 4096 px each).
2. Imported with glTFast, with no conversion, retopology or Blender step. Claude checked file hashes to prove it used the student's exact files.
3. The scene builder wraps each model in a racer prefab: it measures the mesh, sets a matching collider (a rolling cylinder, a sphere or a capsule), applies a rotation so the model lies the right way, and links a stat sheet.
4. For web builds, the build tool copies the textures into Unity-compressed form at the same 4096 px resolution (incident 7).

### 3.1 Tuna Can (the player)

* **Route:** ChatGPT reference image (`Reference/tuna.png`), then Tripo image-to-3D. File: `Models/tuna.glb`.
* **Exact Prompt:**

> A horizontal tuna can designed as a rolling prop. It is short, cylindrical, and compact, with a blue label, silver metallic ends, and a simple fish icon on the side. The form is clean and minimal, with a slightly more realistic can structure than the fruit. No tuna pieces, no face, and no limbs. The style should match the rest of the set: simple, polished, and game-ready.
>
> Simple stylized 3D game asset, soft realism, clean shapes, minimal detail, smooth materials, polished studio look, toy-like proportions, isolated object, no character face, no arms or legs, bright but controlled colors, consistent with casual mobile game art.

* **Placement:** the player character in every mode, the camera's focus in the menu fly-out, and a selectable character.

### 3.2 Canned Bread

* **Route:** Tripo text-to-3D. Files: `breadcan.glb` (in the game), `breadcan_v2.glb`, and a third take, `breadcan_v3.glb`, that was not imported.
* **Exact Prompt:**

> sealed can of bread lying horizontally on its side, designed to roll. Simple closed cylindrical metal can with silver ridged rims and flat metal lids on both ends. Wrap-around paper label in warm cream and toasty brown, with a simple illustration of a golden loaf of bread in the middle of the label, no text. Clean, polished, same style as a simple canned-food prop. No bread outside the can, no extra pieces. Simple stylized 3D game asset, soft realism, clean shapes, minimal detail, smooth materials, polished studio look, toy-like proportions, isolated single object, no face, no arms or legs, bright but controlled colors, consistent with casual mobile game art.

* **Placement:** AI rival and playable character. In bot testing it was the toughest rival (heavy and stable), so its knockback was tuned down.

### 3.3 The other models

| Model | File | Route | Placement in game | Prompt |
| --- | --- | --- | --- | --- |
| Treadmill | `treadmill.glb` | ChatGPT image, then Tripo image-to-3D | The treadmill itself, center of the scene | [STUDENT: add prompt] |
| Apple | `apple.glb` | ChatGPT image, then Tripo image-to-3D | Rival and playable character | [STUDENT: add prompt] |
| Orange | `orange.glb` | ChatGPT image, then Tripo image-to-3D | Rival and playable character | [STUDENT: add prompt] |
| Bouncy Ball | `ball.glb` | ChatGPT image, then Tripo image-to-3D | Rival and playable character | [STUDENT: add prompt] |
| Canned Beans | `beans.glb` | ChatGPT image, then Tripo image-to-3D | Rival and playable character | [STUDENT: add prompt] |
| Cola Can | `cola.glb` | ChatGPT image, then Tripo image-to-3D | Rival and playable character | [STUDENT: add prompt] |
| Raw Fish | `fish.glb` | Tripo text-to-3D | Rival and playable character (slides instead of rolling) | [STUDENT: add prompt] |
| Rolling Pin | `rollingpin.glb` | Tripo text-to-3D | Rival and playable character | [STUDENT: add prompt] |
| Water Bottle | `waterbottle.glb` | Tripo text-to-3D | Rival and playable character | [STUDENT: add prompt] |
| Onion | `onion.glb` | Tripo text-to-3D | Rival and playable character | [STUDENT: add prompt] |
| Paper Towels | `papertowel.glb` | Tripo text-to-3D | Rival and playable character | [STUDENT: add prompt] |

Unused takes kept in the folder for the record: `treadmill_parts.glb`, `breadcan_v2.glb`, `rollingpin_v2.glb`, `waterbottle_v2.glb`, `waterbottle_v3.glb`.

---

## 4. Audio and Trailer Prompts

### 4.1 Audio

No generative audio tool was used for this game. Every sound is synthesized in code when the game starts (`Scripts/AudioFX.cs`), so there are no audio files:

| Sound | Trigger | Loop role |
| --- | --- | --- |
| Coin | Picking up a coin, coin count-ups | Reward |
| Orb | Collecting a stamina orb | Reward |
| Accel | Acceleration activates | Reward |
| Super ready / Super go | Super Speed charged / fired | Reward |
| Fall | A racer falls off the treadmill | Damage / End |
| Cheer | The crowd reacts to eliminations and wins | End |
| Click, Shutter | Buttons, the TV camera decor | UI |

[STUDENT: if you add ElevenLabs sounds from your Week 6 pipeline, list the prompts here.]

### 4.2 Trailer Shot Prompts

[STUDENT: list each generated shot's prompt and tool, and which shots are real gameplay capture.]

---

## 5. Debugging, Error Recovery & Friction Log

### Incident 1: One bump launched the whole field

* **Symptom:** a single collision set off a chain, and every racer went flying off the belt at once.
* **Cause:** each bump added knockback to racers that were already moving fast, so the force kept stacking.
* **Resolution:** a 0.35 second cooldown on knockback per racer and a maximum knock speed.

### Incident 2: Unity silently lost game scripts from the scene

* **Symptom:** after reopening the scene, some decor objects had empty script slots.
* **Cause:** Unity only saves a MonoBehaviour in a scene if it lives in a file with the same name as the class. Several classes shared one file.
* **Resolution:** split them into one file per class (the same problem came back later with the decor manager and got the same fix).

### Incident 3: The AI drove itself off the treadmill

* **Symptom:** Easy AI racers died within 2 seconds by reversing straight off the back.
* **Resolution:** a clamp on reverse speed near the back edge, plus logic that eases off instead of flooring it when in danger. Later, the bots showed the Ball stuck in a "recover" state at the edge; it now switches to escaping the edge instead.

### Incident 4: Tripo models had oversized colliders

* **Symptom:** rotated models such as the rolling pin and water bottle hit things they were not touching.
* **Cause:** Unity's bounding boxes for rotated meshes are bigger than the visible model.
* **Resolution:** the scene builder measures the actual mesh vertices instead.

### Incident 5: Some racers could not survive at all

* **Symptom:** the rolling pin and water bottle could not keep up with a fast belt; the fish kept flipping over, and then was too slippery once fixed.
* **Resolution:** a spin-speed cap based on each model's radius; for the fish, a low center of mass and a righting force so it pops back upright like a weeble, then friction, mass and knockback retuned from race data.

### Incident 6: Playtest bots leaked into the student's real save (3 times)

* **Symptom:** after bot batches, the student's save had extra coins, characters, achievements and a used daily reward. Another time the "best time" showed 5.6 seconds from a test run.
* **Resolution:** the real save is snapshotted before every test and restored afterward; the bot runner restores in a `finally` block and on editor shutdown; best time is now computed from real player runs only.
* **Related friction:** the editor nearly stops running when its window loses focus, which stalled bot batches. Saving a script during a batch triggered a recompile that killed the batch. Fixes: `Application.runInBackground` during tests, and a rule never to edit scripts while bots are running.

### Incident 7: Getting the game into a browser without making it look worse

* **Symptom 1:** the first Windows build failed after 8.5 minutes. The disk was full because the build was heading for about 3 GB.
* **Cause:** Tripo's `.glb` files embed 4096 px textures, and Unity was storing them uncompressed (64 MB each, 39 textures).
* **First attempt (rejected by the student):** Claude shrank the textures to 1024 px. The student compared them and said the slimmed version looked worse. Claude restored the originals from the backup.
* **Second attempt:** a build tool that keeps every texture at the full 4096 px and only changes how Unity stores it (compressed), for the build only. The editor project stays untouched. Test renders were checked against the originals before building.
* **Symptom 2:** that build succeeded, but in the browser it crashed trying to allocate 4 GB.
* **Cause:** the prefabs still pointed at meshes inside the `.glb` files, and Unity packs a referenced `.glb` whole, including its uncompressed textures, so the originals were still in the build (214 MB each).
* **Resolution:** the build tool also copies the meshes out and points everything at the copies, so the build never touches a `.glb`. The build report confirmed 0 MB from `.glb` files. Final result: 441 MB unpacked, 117 MB download, loads in the browser and ran at 60 FPS in testing.
* **Takeaway:** "the build succeeded" was not proof. Only loading it in a real browser caught the crash.

---

## 6. Human-in-the-Loop Curation & Analytical Reflection

[STUDENT: write 200–300 words in your own voice. Things you might cover, based on what happened:]

* What you designed versus what Claude built: the concept from the viral treadmill videos, the tuna can as the player, every Tripo model and prompt, the yellow-button start, the invisible anti-camping rule, the upgrade numbers in your spec.
* Where you overruled the AI: replacing its visible zap zones with your warning rule, rejecting the slimmed textures because they looked worse, insisting it check it used your exact Tripo files.
* How the engine changed the division of labor compared to PG2: in the browser game Claude edited files you could open directly; in Unity it worked through the MCP bridge and desktop control, and spent much of its time on engine problems (colliders, scene serialization, editor focus, build size) instead of game rules.
* What you would do differently next time.

---

## 7. Asset Attribution & Licensing Table

| Asset | Type | Source / Tool | License / Terms |
| :---- | :---- | :---- | :---- |
| `Models/*.glb` (12 racers, treadmill) | 3D models | Generated by the student with Tripo 3D | Tripo terms of service. [STUDENT: free plan or paid? Models made on Tripo's free plan are public; check Tripo's current terms for the exact license and credit wording.] |
| `Reference/*.png` | Reference images | Generated by the student with ChatGPT | OpenAI terms of use; the user owns the output |
| Sound effects | Synthesized audio | Written in code with Claude | Student's own work |
| Game code, UI, arena decor | C#, Unity scene | Written with Claude Code, directed by the student | Student's own work |
| Unity 6, URP, glTFast, Input System | Engine and packages | Unity Technologies | Unity Terms of Service / Unity Companion License |
| LegacyRuntime font | UI font | Ships with Unity | Unity Terms of Service |
