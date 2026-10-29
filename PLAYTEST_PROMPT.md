# Playtest brief (standing instructions for Claude while developing Treadmill Mayhem)

You are developing and testing Treadmill Mayhem in Unity (`C:\Users\David\My project`).
Testing is part of development, not a final step.

## 1. The 50-bot playtest
**Tool:** Tools ▸ Treadmill Mayhem ▸ Run 50-Bot Playtest (`Scripts/Testing/PlaytestRunner.cs`).

**What it runs:** 50 simulated players. Each one plays a full run with the real player controls (stick, boost, super) through an autopilot.

**Skill spread** is evenly distributed from 0.0 to 1.0:
- **0.0–0.2 "first-timer":** slow reactions, wanders, rarely boosts, never uses super.
- **0.2–0.5 "casual":** dodges some obstacles, boosts late.
- **0.5–0.8 "regular":** good positioning, uses boost and super.
- **0.8–1.0 "expert":** near-optimal; finds exploits if they exist.

**Mode mix:**
- 50% Race, at random AI difficulties
- 25% Obstacle Free Play
- 25% Obstacle Levels (random unlocked level)

**What each run records:** mode, skill, difficulty or level, survival time, score, place, cause of elimination, coins and gears earned, min/avg fps, errors or exceptions logged, and "stuck" events (no progress for 8 s).

**Output:** `Temp/Playtest/report.md` plus `runs.jsonl`.

**The player's save is snapshotted before the batch and restored after**, so testing never changes real progress.

## 2. When to test (about one extra hour in total)
- **Baseline:** before starting new features.
- **After each feature:** a quick 10-bot smoke run of the affected mode.
- **Final:** the full 50-bot pass when the features are ready.
- **Manual checks too:** screenshots of every new screen, and console errors after every compile.

## 3. What to look for, and how to judge it
- **Bugs:** exceptions, softlocks (run never ends), stuck racers, UI overlap or clipping, values that don't add up (coins, gears, scores), saves not restored.
- **Balance:**

  | Who | Target survival |
  |---|---|
  | First-timer, Race on Easy | about 20–40 s |
  | Regular | about 60–90 s |
  | Expert vs Insane AI | about 50% chance of winning |
  | Level 1 | most skills ≥ 0.3 clear it |
  | Level 12 | only experts clear it |

  If no bot below skill 0.5 ever survives 15 s, the game is too hard for new players.
- **Pacing:** Super Speed should charge at least once in a typical 60 s run. Coins per run should afford an upgrade every 1–3 runs early on.
- **Polish:** anything that looks unclear, cramped, or confusing for a first-time player.

## 4. Notes file
Keep a running `PLAYTEST_NOTES.md` with three sections, each item dated with the evidence (run numbers, numbers, screenshot):
- **Bugs to fix**
- **Things to polish**
- **Things to add**

Fix what you can in the same session; leave the rest listed for the user.

## 5. Rule learned the hard way (10/28)
**Never save script changes while a bot batch is running.**

Even with "Recompile After Finished Playing", the editor wedged between Play mode and the pending compile. That froze the batch at run 17, and the old harness never restored the save.

- Write new code only between batches, or in a scratch folder, and move it in afterwards.
- The harness now restores the save in a `finally` block and in `OnDestroy`.
