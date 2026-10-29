# Playtest notes

Running log, as `PLAYTEST_PROMPT.md` asks for. Evidence comes from:
- **Baseline:** 18 bot runs, before today's features (`PLAYTEST_BASELINE_REPORT.md`).
- **Final pass:** 50 bot runs with every feature in, bots playing random characters (`PLAYTEST_FINAL_REPORT.md`).
- **Code review:** an independent reviewer agent read the new code.
- **Manual checks:** screenshots in the Editor.

## Bugs to fix
- [fixed] **Camping rule in Obstacle Mode:** it disqualified a player for dodging at the front, where obstacles come from. The rule is now off in Obstacle Mode.
- [fixed] **Pause input:** Space or SUPER could spend a Super Speed charge while paused, and touches under the pause menu could grab the stick. Input is now ignored while paused. (review)
- [fixed] **Countdown pause:** switching apps during the countdown didn't pause, so the race started without you. Auto-pause now covers the countdown. (review)
- [fixed] **Corrupt date in a save:** a bad or future daily-reward date in the save could crash the menu or lock the reward. Loaded values are now sanitised. (review)
- [fixed] **Unknown character in a save:** a save naming a character this build doesn't have now falls back to the Tuna. Before, the menu reloaded in a loop. (review)
- [fixed] **Pause-menu clicks were silent:** they are audible now.
- [fixed] **Sound toggles forgot your volume:** turning sound or music back on restores your previous slider level. (review)
- [fixed] **Best time was missing for older saves:** it is now derived from your leaderboard runs. Your best: 81.7 s.
- [fixed] **Bot harness, save safety:** an aborted batch (script reload, leaving Play mode) could leave test coins and characters in the real save. It now restores in a `finally` block, in `OnDestroy`, before an assembly reload, and on exiting Play mode. This happened twice today; both times I restored by hand from the backup.
- [fixed] **Bot harness, false softlocks:** they were caused by editor throttling. The harness now runs in the background, and a softlock means the run clock actually stopped.
- [fixed] **Pause icon:** "II" rendered as a single black block. It is now two drawn bars.
- [verified] **First-run tips:** checked with a simulated brand-new save. They appear at 0.5 s and 6 s, worded for keyboard or touch.
- [fixed] **Earlier today:** shop tab overlap; Remove Ads text not wrapping; "Press SPACE" shown on phones; the character-card CONTROL label clipped.

## Things to polish
- [fixed, keep watching] **Ball NPC drops out first:**
  - Evidence: median 10 s in the baseline and 18 s in the final pass (7 outs). Elimination logging showed the Ball being *launched* off the back at about 13 m/s after hits, while other racers drift off slowly.
  - Fix: a ball in rear danger now always escapes instead of "recovering". Retuned bounciness 0.65→0.4, mass 0.6→0.8, knockback 1.15→0.95.
  - Recheck (16 Race runs, `PLAYTEST_BALL_CHECK_REPORT.md`): only 1 Ball NPC out, so it's inconclusive. As the player, the Ball now survives all 150 s, so watch that it isn't now too strong.
- [fixed] **Free Play plateaued at 2 minutes:**
  - Evidence: a skill-0.53 bot hit the 150 s cap in the baseline, and casual bots reached 144 s.
  - Fix: past 2 minutes, waves now speed up and double, and the belt speeds up.
- **Opening seconds can feel harsh for brand-new players:**
  - Also seen in the latest Race check: first-timer bots had a median of 11 s (3 runs); one died at 6 s.
  - Evidence: first-timer bots in Free Play had a median of 15 s; two runs died in about 2 s after steering off the side.
  - [added, not yet bot-tested] **Beginner safety net:** in a player's first 3 runs, one fall in the first 8 s is forgiven ("SAVED!" banner, back on the belt with a short shield). `GameManager.GraceRuns`/`GraceSeconds`. Bot runner now plays first-timer bots as brand-new players; next step: run the 10-bot smoke test and compare first-timer survival.
- **AI difficulty separation looks healthy:** Easy AI was beaten in 4 of 7 races; Hard won all 9 of its; Insane and Expert are close to unbeatable for bots. If human testers find Hard too harsh, lower its rear safety slightly.
- **Character balance (Race only, small samples):** Orange and Cola are strong; Fish and Water Bottle are tricky, which matches their "expert" blurbs. Re-check with more runs before changing anything.
- **Editor frame rate:** about 20 fps at 4× bot speed in the QHD Game view. That's an editor artifact. Profile on a real phone.
- **Fish portrait:** shows the fish head-on. A side view would read better.

## Things to add
- [added] **Pause menu:** button plus Esc/P/Android back; auto-pauses on app switch; sound and music toggles.
- [added] **Character select:** 12 racers, coin unlocks 250–1,300, a stats panel and a "NEW LOOK" achievement. New racers: Onion and Paper Towel.
- [added] **Daily login calendar:** 7 days; day 7 pays 1,000 coins + 100 gears.
- [added] **First-run coaching tips:** for brand-new players only.
- [added] **Phone vibration:** on knockout, Super Speed and level clear, with a Settings toggle.
- [added] **Store copy:** listing text and a privacy-policy draft (`STORE_LISTING.md`, `PRIVACY_POLICY.md`).
- **Idea: haptics.** The current vibration is a single buzz. A native plugin would allow light taps on bumps.
- **Idea: real music.** The current loop is synthesized. A licensed upbeat track would lift the whole game.
- **Idea: character-specific taunts and sounds**, such as the onion's tear splash or the bottle's slosh.
- **Idea: unused models.** Use the v2 models (breadcan_v2, rollingpin_v2, waterbottle_v2/v3) as alternate skins.
- **Idea: online leaderboards.** Game Center and Google Play Games, after launch.
