# Decisions

One line per choice the brief did not settle, and the reasoning for every value changed during balancing.

## Product choices

- **Platform.** Plain HTML, CSS and classic JavaScript. No build step, no CDN, no fonts or images. It runs from `index.html` opened from disk. Reason: the team can publish on GitHub Pages with no setup, and it works offline in a classroom.
- **Module limits.** The brief gave limits for the reactor (1) and shield layers (3). We added: 6 solar arrays, 4 batteries, 3 greenhouses, 3 ice drills. Reason: keep the outpost drawing readable and stop "just build 20 arrays" from solving every power problem.
- **Greenhouses and drills are switched on and off one by one.** Reason: a 10-year-old can see which one uses power, and the shutdown order stays clear.
- **Crates do not come back.** The brief gave 14 crates and no resupply. We kept that. Reason: it makes every crate a real trade-off.
- **Mars "sunlight".** Mars solar output is stored as 3 ⚡ (7 × 43%, tagged Simplified) and the Derived 43% is shown beside it. The dust factor multiplies it.
- **"Runs out in N days".** It uses today's rate only (the brief says "at the current rate"). Greenhouse food is counted as an average of 2 kg a day per lit greenhouse, and the screen says so.
- **Energy "demand" for shutdown.** Systems are shut one at a time in the brief's order until the demand fits what the arrays, reactor and battery can supply. A system with no water (oxygen plant on the Moon, greenhouse) does not run and does not use power.
- **Water order.** Drills add water first, then the crew uses its water, then the Moon oxygen plant, then the greenhouses. Reason: the crew comes first.
- **Health with full rations.** Full gives +2 only on a day with no shortage. On a day with no food at all, only the food penalty applies.
- **Dose at day 30.** The end check runs after day 30 too, so a mission that ends over 40 mSv counts as an evacuation (the Moon without shielding reaches 41.1 mSv).
- **Real values never tuned.** All Real values are locked by a test (`tests/engine.test.js`).
- **Moon storm days 7 and 20, Mars storm day 23, dust storm days 12-17.** Each moves by -1, 0 or +1 day from a seeded generator (the dust storm moves as a whole block, always 6 days).
- **Mars solar storm is labelled with 3 October 2024.** The game does not say that storm was seen on Mars.
- **DONKI.** The API returned 404, so no flare ID or class is shown. See `SOURCES.md`.
- **Failure wording.** Only "emergency evacuation" and "medical evacuation". A test scans the debrief text for banned words.
- **Cause chains are built from the game's real log.** The engine records the first day of each event (night, dust storm, battery empty, each shutdown, each shortage). `js/debrief.js` reads those and picks the dominant cause (the shortage that cost the most health, or radiation). Success also gets a chain (decisions, threat, radiation, end).
- **Tier 2 is included.** Equipment fault once per mission (day 8-13, a random installed system: oxygen plant, lab, a drill or a greenhouse). Repair: 1 crate, or 3 Maintenance days. Emergency fuel-to-water, once: +40 L, 20% chance one module breaks. The preview never shows a surprise fault or accident.
- **Maintenance on the Moon.** It only repairs broken modules (3 days). On Mars it also cleans the arrays.
- **Myth or Reality cards.** Six cards, shown after days 5, 9, 14, 19, 23 and 27. Two are Reality about the ISS and MOXIE, two are Myths about Mars wind and Moon air, one is Reality about dust and InSight, one is a Myth about the lunar night.
- **Hints.** Mission Control hints on days 1, 2 and 3, each pointing at one panel. They can be dismissed.
- **Phone layout.** The page stacks as outpost, gauges, report, preview, then the four decisions. Run day is a sticky button at the bottom.
- **No animation.** The page has no CSS animation. This also respects reduced-motion settings.
- **Debrief chart.** One line chart, every series shown as a percentage of its full value (dose as a percentage of the 40 mSv limit). Lines differ by colour and dash style, there is a legend, and a table with the same numbers.

## Balance: the scripted players

The balance tests use four scripted players (`tests/players.js`):

- **Does nothing.** Systems off, shelter every day, no builds.
- **Naive.** Everything on, builds 7 solar arrays and nothing else, never shelters, never cleans.
- **Decent.** Picks one of five plausible build plans at random (some are good, some have a hole such as no drill, no reactor, or no shield), then makes human slips: forgets to shelter 20% of storm days, skips cleaning 25% of the time, switches everything on 12% of days, uses Full rations every day in 35% of games, ignores a broken module 30% of the time. The Moon and Mars plan lists differ, because the sites differ.
- **Expert.** Best scripted plan. Moon: reactor, 2 drills, greenhouse, 1 shield. Mars: reactor, greenhouse, drill, shield, 1 array. Each day it tests every combination of systems with the same preview the player sees, picks the best one with no shutdowns, shelters on storm days, cleans on Mars every 5 days, and repairs or uses the emergency when needed.

## Balance: Simplified values that were changed

| Value | Brief | Final | Why |
|---|---|---|---|
| Starting food | 130 kg | **150 kg** | At 130 kg the best Moon plan finished every game but ended with health around 58, so 0% earned three stars (the brief needs at least 50%). The Moon greenhouse can only grow during daylight and reactor days, so food is tight. 150 kg gives a thoughtful player room to use Full rations late. |
| Dust storm solar multiplier | 0.25 | **0.15** | At 0.25, a Mars player who built nothing and only cleaned the arrays still won every game, so the "reactor or more power" decision did not matter. At 0.15 you need a reactor, or more arrays plus charged batteries. "Nothing built" now fails, and the dust-ignoring plans fail. |
| Added limits | none | 6 arrays, 4 batteries, 3 greenhouses, 3 drills, food store 250 kg | See above. |
| Tier 2 values | none | repair 1 crate or 3 Maintenance days, +40 L, 20% | From the brief. |

Everything else is exactly the brief's starting point (stocks, module costs, outputs, penalties, star limits).

## Balance: final results (200 seeds per site)

| Player | Moon | Mars | Required |
|---|---|---|---|
| Does nothing: evacuated | 100% | 100% | 100% |
| Naive: evacuated | 100% | 100% | at least 90% |
| Decent: completes | 58.5% | 85.0% | 40-90% |
| Expert: completes | 100% | 100% | at least 95% |
| Expert: three stars | 92% | 100% | at least 50% |

Impossible plans (random build lists, careful play, 10 seeds each):

- Moon, no reactor and no shielding: 0 of 400 complete.
- Moon, shielding but no reactor: 0 of 400 complete.
- Moon, reactor but no shielding: 0 of 400 complete.
- Mars, never cleans and no reactor: 0 of 400 complete. Mars, solar only and never cleans: 0 of 200.

## Limits of the balance

- The scripted players are not children. The Moon Decent rate is 58.5% and the Mars one 85.0%. A real 10-year-old will probably fail more often on the first try. That is intended (the debrief is the lesson), but the team should watch three people play (see `README.md`) and soften starting stocks if most first tries fail on day 12 or earlier.
- The Mars Decent rate is near the top of the 40-90% band. If you make Mars easier, re-run `npm test`.
