# Treadmill Mayhem - bot playtest report

In progress: 18 / 50 runs  •  2026-10-28 12:41  •  speed x4

## Survival by skill band and mode (median seconds, runs)
| Band | Race | Free Play | Levels (cleared/played) |
|---|---|---|---|
| first-timer | 28s (3) | - | 0/0 |
| casual | 90s (4) | 144s (1) | 2/2 |
| regular | 93s (3) | 150s (1) | 2/2 |
| expert | - | 149s (1) | 1/1 |

## Race results by AI difficulty
| AI | Runs | Wins | Median survival | Avg coins |
|---|---|---|---|---|
| EASY | 3 | 0 | 28s (3) | 217 |
| EXPERT | 2 | 0 | 73s (2) | 244 |
| HARD | 3 | 1 | 89s (3) | 283 |
| INSANE | 1 | 0 | 92s (1) | 288 |
| NORMAL | 1 | 1 | 75s (1) | 248 |

## NPC elimination times (median s, count) - lower = weaker racer
- Ball: 10s (6 outs)
- Rolling Pin: 20s (1 outs)
- Water Bottle: 21s (3 outs)
- Cola: 25s (1 outs)
- Orange: 43s (3 outs)
- Fish: 52s (5 outs)
- Beans: 73s (3 outs)
- Apple: 74s (5 outs)
- Bread Can: 81s (3 outs)

## Pacing
- Runs that used Super Speed at least once: 9 / 18
- Causes of player elimination: fell off x11, - x7
- Outcomes: OUT x9, WON x2, LEVEL CLEAR x5, TIMEOUT (survived 150s) x1, SOFTLOCK (state Racing) x1
- FPS (unscaled, editor): avg 23, worst min 0

## Problems (1 runs)
- Run 18 (Race, skill 0.57): SOFTLOCK (state Racing)

## All runs
| # | Mode | Skill | AI/Lvl | Time | Score | Outcome | Cause | Place | Coins | Gears | Super | FPS min |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Race | 0.02 | EASY | 27.8 | 1489 | OUT | fell off | 3/6 | 102 | 0 | 0 | 16 |
| 2 | Race | 0.59 | HARD | 125.3 | 10570 | WON | fell off | 1/6 | 576 | 0 | 3 | 14 |
| 3 | Race | 0.49 | EASY | 107.6 | 8580 | OUT | fell off | 2/6 | 471 | 0 | 2 | 11 |
| 4 | ObstacleFree | 0.45 | - | 143.6 | 15230 | OUT | fell off | - | 0 | 35 | 4 | 14 |
| 5 | ObstacleLevels | 0.55 | L2 | 30.2 | 2282 | LEVEL CLEAR | - | - | 0 | 33 | 0 | 15 |
| 6 | ObstacleLevels | 0.96 | L4 | 35.1 | 2431 | LEVEL CLEAR | - | - | 0 | 47 | 0 | 15 |
| 7 | ObstacleLevels | 0.39 | L1 | 25 | 1651 | LEVEL CLEAR | - | - | 0 | 25 | 0 | 16 |
| 8 | Race | 0.1 | EASY | 22.1 | 1104 | OUT | fell off | 3/6 | 79 | 0 | 0 | 16 |
| 9 | ObstacleFree | 0.53 | - | 150.1 | 14027 | TIMEOUT (survived 150s) | - | - | 0 | 0 | 4 | 14 |
| 10 | Race | 0.73 | EXPERT | 93.3 | 5090 | OUT | fell off | 3/6 | 294 | 0 | 1 | 12 |
| 11 | Race | 0.27 | INSANE | 91.8 | 4890 | OUT | fell off | 3/6 | 288 | 0 | 1 | 14 |
| 12 | ObstacleLevels | 0.71 | L9 | 45.1 | 3280 | LEVEL CLEAR | - | - | 0 | 105 | 1 | 16 |
| 13 | Race | 0.16 | EXPERT | 53.4 | 3055 | OUT | fell off | 5/6 | 193 | 0 | 0 | 15 |
| 14 | ObstacleLevels | 0.31 | L6 | 40.1 | 3206 | LEVEL CLEAR | - | - | 0 | 68 | 0 | 15 |
| 15 | Race | 0.43 | HARD | 89 | 4649 | OUT | fell off | 3/6 | 272 | 0 | 1 | 15 |
| 16 | ObstacleFree | 0.86 | - | 148.9 | 15044 | OUT | fell off | - | 0 | 47 | 5 | 16 |
| 17 | Race | 0.24 | NORMAL | 75.4 | 4171 | WON | fell off | 1/6 | 248 | 0 | 0 | 16 |
| 18 | Race | 0.57 | HARD | 16.3 | 917 | SOFTLOCK (state Racing) | - | 0/6 | 0 | 0 | 0 | 0 |
