# Treadmill Mayhem - bot playtest report

Finished: 50 / 50 runs  •  2026-10-28 13:42  •  speed x4

## Survival by skill band and mode (median seconds, runs)
| Band | Race | Free Play | Levels (cleared/played) |
|---|---|---|---|
| first-timer | 44s (6) | 15s (2) | 1/2 |
| casual | 60s (7) | 98s (4) | 2/4 |
| regular | 118s (7) | 120s (4) | 4/4 |
| expert | 75s (6) | 129s (2) | 2/2 |

## Race results by AI difficulty
| AI | Runs | Wins | Median survival | Avg coins |
|---|---|---|---|---|
| EASY | 7 | 4 | 107s (7) | 314 |
| EXPERT | 2 | 0 | 75s (2) | 253 |
| HARD | 9 | 0 | 58s (9) | 213 |
| INSANE | 3 | 0 | 72s (3) | 215 |
| NORMAL | 5 | 2 | 76s (5) | 314 |

## NPC elimination times (median s, count) - lower = weaker racer
- Ball: 18s (7 outs)
- Apple: 34s (5 outs)
- Orange: 34s (4 outs)
- Fish: 38s (10 outs)
- Paper Towel: 38s (3 outs)
- Cola: 46s (6 outs)
- Onion: 46s (7 outs)
- Tuna: 50s (5 outs)
- Water Bottle: 51s (6 outs)
- Beans: 57s (5 outs)
- Bread Can: 64s (7 outs)
- Rolling Pin: 65s (2 outs)

## Playing as each character (median survival, runs)
- Rolling Pin: 137s (1)  •  avg skill 0.94
- Orange: 129s (4)  •  avg skill 0.49
- Cola: 96s (3)  •  avg skill 0.41
- Apple: 93s (3)  •  avg skill 0.62
- Paper Towel: 76s (6)  •  avg skill 0.56
- Tuna: 70s (6)  •  avg skill 0.39
- Beans: 69s (5)  •  avg skill 0.71
- Ball: 58s (5)  •  avg skill 0.47
- Fish: 45s (3)  •  avg skill 0.35
- Water Bottle: 45s (5)  •  avg skill 0.48
- Onion: 34s (4)  •  avg skill 0.40
- Bread Can: 25s (5)  •  avg skill 0.47

## Pacing
- Runs that used Super Speed at least once: 29 / 50
- Causes of player elimination: - x14, fell off x36
- Outcomes: SOFTLOCK (state Racing) x2, WON x6, OUT x30, TIMEOUT (survived 150s) x3, LEVEL CLEAR x9
- FPS (unscaled, editor): avg 20, worst min 0

## Problems (2 runs)
- Run 1 (ObstacleLevels, skill 0.22): SOFTLOCK (state Racing)
- Run 3 (Race, skill 0.41): SOFTLOCK (state Racing)

## All runs
| # | Char | Mode | Skill | AI/Lvl | Time | Score | Outcome | Cause | Place | Coins | Gears | Super | FPS min |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Fish | ObstacleLevels | 0.22 | L11 | 5 | 247 | SOFTLOCK (state Racing) | - | - | 0 | 0 | 0 | 0 |
| 2 | Paper Towel | Race | 0.84 | NORMAL | 92.2 | 6361 | WON | fell off | 1/6 | 357 | 0 | 2 | 9 |
| 3 | Bread Can | Race | 0.41 | HARD | 22.5 | 1122 | SOFTLOCK (state Racing) | - | 0/6 | 0 | 0 | 0 | 0 |
| 4 | Orange | Race | 1 | HARD | 109.4 | 9008 | OUT | fell off | 2/6 | 496 | 0 | 3 | 16 |
| 5 | Ball | Race | 0.16 | NORMAL | 75.7 | 4286 | OUT | fell off | 3/6 | 257 | 0 | 0 | 3 |
| 6 | Bread Can | ObstacleLevels | 0.31 | L8 | 10.7 | 559 | OUT | fell off | - | 0 | 1 | 0 | 17 |
| 7 | Apple | ObstacleFree | 0.86 | - | 121.1 | 10208 | OUT | fell off | - | 0 | 33 | 3 | 4 |
| 8 | Water Bottle | Race | 0.51 | EASY | 150 | 11548 | TIMEOUT (survived 150s) | - | 0/6 | 0 | 0 | 3 | 13 |
| 9 | Tuna | Race | 0.35 | EXPERT | 82.1 | 4769 | OUT | fell off | 5/6 | 271 | 0 | 1 | 6 |
| 10 | Ball | ObstacleFree | 0.2 | - | 101.7 | 8787 | OUT | fell off | - | 0 | 39 | 0 | 14 |
| 11 | Ball | Race | 0.27 | HARD | 4.7 | 237 | OUT | fell off | 6/6 | 33 | 0 | 0 | 13 |
| 12 | Bread Can | Race | 0.18 | HARD | 56.3 | 3542 | OUT | fell off | 3/6 | 213 | 0 | 0 | 13 |
| 13 | Paper Towel | Race | 0.49 | EASY | 107 | 7910 | WON | fell off | 1/6 | 438 | 0 | 2 | 7 |
| 14 | Fish | ObstacleFree | 0.45 | - | 94.9 | 5568 | OUT | fell off | - | 0 | 35 | 2 | 15 |
| 15 | Tuna | Race | 0.43 | HARD | 57.6 | 3227 | OUT | fell off | 5/6 | 206 | 0 | 1 | 10 |
| 16 | Paper Towel | Race | 0.24 | NORMAL | 60 | 3400 | OUT | fell off | 4/6 | 210 | 0 | 0 | 9 |
| 17 | Paper Towel | ObstacleLevels | 0.96 | L4 | 35.2 | 2108 | LEVEL CLEAR | - | - | 0 | 43 | 0 | 14 |
| 18 | Rolling Pin | ObstacleFree | 0.94 | - | 137.3 | 16810 | OUT | fell off | - | 0 | 25 | 6 | 11 |
| 19 | Orange | ObstacleFree | 0.53 | - | 149.3 | 15840 | OUT | fell off | - | 0 | 49 | 5 | 10 |
| 20 | Tuna | ObstacleLevels | 0.14 | L5 | 35 | 2702 | LEVEL CLEAR | - | - | 0 | 53 | 0 | 17 |
| 21 | Tuna | Race | 0.57 | EASY | 118.2 | 10565 | WON | fell off | 1/6 | 584 | 0 | 3 | 15 |
| 22 | Onion | ObstacleLevels | 0.55 | L4 | 35 | 2700 | LEVEL CLEAR | - | - | 0 | 39 | 1 | 13 |
| 23 | Paper Towel | Race | 0.76 | HARD | 94.3 | 5981 | OUT | fell off | 3/6 | 339 | 0 | 2 | 15 |
| 24 | Beans | Race | 0.98 | INSANE | 71.9 | 4379 | OUT | fell off | 5/6 | 251 | 0 | 1 | 14 |
| 25 | Orange | ObstacleFree | 0.37 | - | 150.1 | 11901 | TIMEOUT (survived 150s) | - | - | 0 | 0 | 3 | 14 |
| 26 | Bread Can | ObstacleLevels | 0.63 | L1 | 25 | 1577 | LEVEL CLEAR | - | - | 0 | 27 | 0 | 10 |
| 27 | Ball | Race | 0.9 | HARD | 58.2 | 3407 | OUT | fell off | 6/6 | 207 | 0 | 1 | 16 |
| 28 | Water Bottle | ObstacleLevels | 0.47 | L9 | 45 | 3958 | LEVEL CLEAR | - | - | 0 | 98 | 1 | 16 |
| 29 | Onion | Race | 0.02 | INSANE | 32.4 | 1619 | OUT | fell off | 5/6 | 106 | 0 | 0 | 15 |
| 30 | Paper Towel | ObstacleFree | 0.04 | - | 27.8 | 1691 | OUT | fell off | - | 0 | 8 | 0 | 11 |
| 31 | Onion | Race | 0.73 | EASY | 120.5 | 9710 | WON | fell off | 1/6 | 531 | 0 | 3 | 15 |
| 32 | Onion | ObstacleFree | 0.29 | - | 2 | 97 | OUT | fell off | - | 0 | 0 | 0 | 15 |
| 33 | Beans | ObstacleLevels | 0.88 | L2 | 30.2 | 2335 | LEVEL CLEAR | - | - | 0 | 31 | 0 | 15 |
| 34 | Beans | Race | 0.92 | EXPERT | 68.7 | 3898 | OUT | fell off | 5/6 | 235 | 0 | 1 | 16 |
| 35 | Water Bottle | ObstacleFree | 0.12 | - | 1.8 | 88 | OUT | fell off | - | 0 | 0 | 0 | 17 |
| 36 | Beans | ObstacleFree | 0.69 | - | 91.3 | 7366 | OUT | fell off | - | 0 | 39 | 2 | 15 |
| 37 | Cola | Race | 0.65 | INSANE | 95.7 | 4886 | OUT | fell off | 3/6 | 289 | 0 | 1 | 15 |
| 38 | Orange | ObstacleLevels | 0.06 | L11 | 18.5 | 1224 | OUT | fell off | - | 0 | 2 | 0 | 13 |
| 39 | Ball | ObstacleLevels | 0.8 | L5 | 35.2 | 3207 | LEVEL CLEAR | - | - | 0 | 50 | 1 | 13 |
| 40 | Cola | Race | 0 | EASY | 3.8 | 190 | OUT | fell off | 6/6 | 29 | 0 | 0 | 18 |
| 41 | Bread Can | Race | 0.82 | HARD | 77.2 | 5912 | OUT | fell off | 2/6 | 337 | 0 | 2 | 11 |
| 42 | Tuna | ObstacleFree | 0.78 | - | 150.1 | 14952 | TIMEOUT (survived 150s) | - | - | 0 | 0 | 5 | 16 |
| 43 | Cola | Race | 0.59 | NORMAL | 131.3 | 9794 | WON | fell off | 1/6 | 539 | 0 | 3 | 16 |
| 44 | Apple | Race | 0.33 | NORMAL | 59.7 | 3471 | OUT | fell off | 5/6 | 208 | 0 | 0 | 16 |
| 45 | Water Bottle | ObstacleFree | 0.61 | - | 41.6 | 2768 | OUT | fell off | - | 0 | 7 | 1 | 12 |
| 46 | Apple | Race | 0.67 | EASY | 93.4 | 6755 | WON | fell off | 1/6 | 380 | 0 | 2 | 16 |
| 47 | Beans | Race | 0.1 | EASY | 67.3 | 3963 | OUT | fell off | 2/6 | 237 | 0 | 0 | 10 |
| 48 | Tuna | Race | 0.08 | HARD | 20.4 | 1218 | OUT | fell off | 6/6 | 85 | 0 | 0 | 17 |
| 49 | Water Bottle | ObstacleLevels | 0.71 | L8 | 45 | 3930 | LEVEL CLEAR | - | - | 0 | 87 | 1 | 14 |
| 50 | Fish | ObstacleLevels | 0.39 | L8 | 45.1 | 2654 | LEVEL CLEAR | - | - | 0 | 88 | 1 | 14 |
