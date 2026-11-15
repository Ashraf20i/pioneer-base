# Project page draft: Pioneer Base

> Draft for the NASA Space Apps Challenge 2026 project page. Everything below is in English. Replace the two link placeholders and check every item under "Before you submit" at the end.

## Project name

Pioneer Base

## Summary (three sentences)

Pioneer Base is a free web game where a student aged 7 to 12 runs a lunar or Martian outpost for 30 mission days and keeps four astronauts healthy. Every day the player balances power, oxygen, water, food and radiation shielding, with a live preview that shows what a plan will do before it is run. When the mission ends, the game shows the exact chain of decisions and shortages that made it fail or succeed, and tags every number as Real, Derived or Simplified.

## Challenge

Build a Junior Astronaut Mission Trainer.

## Detailed description

**The problem.** Space-themed STEM content often oversimplifies the engineering trade-offs that define a real mission, or presents them at a level too complex to hold a young learner's attention. We wanted to make those trade-offs tangible and fun.

**What we built.** Pioneer Base lets students run a **lunar or Martian outpost** in a turn-based game. One turn is one mission day, and a full game takes 8 to 12 minutes. Each day the player plans, then presses Run day:

- **Power.** Solar arrays, batteries and a fission reactor. On the Moon, the 15-day night stops every solar array. On Mars, dust slowly covers the arrays and a dust storm cuts their output. When power is short, systems shut down in a fixed order, and the player sees one shortage cause another.
- **Life support.** An oxygen plant (on the Moon it splits water, on Mars it takes oxygen from carbon dioxide, an idea tested by NASA's MOXIE), water from ice drills, and a habitat that recycles 90% of the crew's water.
- **Radiation shielding.** The Moon surface gives about 1.37 mSv a day and Mars about 0.67 mSv a day. Shield layers cut the dose, and sheltering in a solar storm cuts the storm dose to one tenth, but stops science that day. Two real solar storms (11 May 2024 and 3 October 2024) set the storm days.
- **Food production.** Greenhouses need power and water and take 10 days to grow a crop (the game shows that real lettuce on the ISS takes about a month). Rations can be Full, Lean or Half, and smaller rations cost health.
- **Trade-offs.** The player has 14 supply crates and no resupply. A reactor, a greenhouse, an ice drill and a shield layer cannot all be bought. Six main dilemmas are built in: panels or reactor, shielding or other modules, shelter or science, greenhouse or rationing, water to drink or to make oxygen, and cleaning panels or doing science.

**Why it is different.**

1. **The debrief makes the decisions that determine whether a mission fails or succeeds visible.** After each mission the game builds a cause chain from the real log of that game, for example: the lunar night began, no reactor, the battery emptied, the oxygen plant shut down, the oxygen tank ran dry, the crew was evacuated. It also names the trade-off behind the result, shows a chart of the 30 days, and gives one thing to try next time.
2. **Honest data.** Every number on screen has a tag: Real (measured or published), Derived (calculated), or Simplified (made simple on purpose). The panel "Where do these numbers come from?" lists every value with a plain explanation and a source link. It is generated from the same file the simulation reads, so the two can never disagree. Real values are never changed to make the game easier.
3. **Tested balance.** An automatic test plays 200 games per site with scripted players: a player who does nothing and a naive player always or almost always fail, a careful but imperfect player finishes about half the time on the Moon and most of the time on Mars, and an expert nearly always finishes. Plans with no reactor on the Moon, or that ignore Mars dust, cannot finish.
4. **Made for a classroom.** No install, no account, no internet needed, and it works on phones, tablets and laptops with keyboard support. A teacher guide with a 45-minute lesson and five discussion questions is built in. Failure is always an "evacuation", never worse, so students can try again.

**Who it is for.** Students aged 7 to 12, their teachers, and families.

**How we built it.** Plain HTML, CSS and JavaScript with no framework and no external files. The simulation engine is separate from the screen so it can be tested in Node. All artwork is original inline SVG.

**What is simplified.** Energy is in abstract units. Crates, module sizes and health penalties are game values. The 90% water recycling is lower than the 98% reached on the ISS. A crop takes 10 days in the game and about a month in real life. The 40 mSv mission limit is much lower than NASA's 600 mSv career limit. The game tags each of these as Simplified.

## NASA data and resources

- **NASA DONKI** (Space Weather Database Of Notifications, Knowledge, Information): the two solar storm dates, 11 May 2024 and 3 October 2024. <https://ccmc.gsfc.nasa.gov/tools/DONKI/>
- **NASA TSIS-1**: sunlight at Earth's distance, 1361 W/m2. <https://science.nasa.gov/mission/tsis-1/>
- **NASA Mars Fact Sheet**: Mars distance from the Sun, 1.524 AU, surface pressure, and distance from Earth (radio delay of 3 to 22 minutes). <https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html>
- **NASA Moon Facts**: length of the lunar day and night. <https://science.nasa.gov/moon/facts/>
- **NASA MSL Curiosity RAD** (Hassler et al. 2014): radiation on the Mars surface, 0.67 mSv/day. <https://doi.org/10.1126/science.1244797>
- **NASA life-support reference values**: oxygen 0.84 kg and food about 1.8 kg per astronaut per day. <https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/>
- **NASA ECLSS water recovery milestone, 2023**: the ISS recovers 98% of its water. <https://www.nasa.gov/missions/station/iss-research/nasa-achieves-water-recovery-milestone-on-international-space-station/>
- **NASA-STD-3001**: career radiation limit, 600 mSv. <https://www.nasa.gov/reference/nasa-std-3001/>
- **NASA MOXIE on Perseverance**: oxygen from Martian air. <https://www.nasa.gov/missions/mars-2020-perseverance/perseverance-rover/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/>
- **NASA InSight mission**: dust on solar panels. <https://www.nasa.gov/missions/insight/nasa-retires-insight-mars-lander-mission-after-years-of-science/>
- **NASA Moon Fact Sheet**: distance from Earth, used for the radio delay (about 1.3 seconds). <https://nssdc.gsfc.nasa.gov/planetary/factsheet/moonfact.html>

> Add here one resource from the challenge page's "Resources" tab, if the page has one.

## Bonus: Pioneer Base 3D (optional extra)

The web game above is our main submission. We also built a **3D version** of the same simulation in Unity, to show where the idea can go next. It uses the **same simulation engine** (ported to C# and checked against the web version with the same automatic tests), so every number and every result is the same.

- **An outpost you can walk around.** A third-person view of the base on Mars or the Moon. What you see is the state of the simulation: crates disappear from the depot when you build, crops grow, dust dims the solar arrays, windows go dark in the shutdown order, shield walls appear around the habitat.
- **True skies.** On the Moon the sky stays black in daylight and the Earth hangs in the sky; on Mars the sky is butterscotch and dust hides the horizon.
- **PIP, a companion drone,** reads the alerts and explains the real numbers when the player walks up to a module.
- **Hard mode, "Crash Landing".** The ship crashes 40 km from the outpost. The player drives a rover through a canyon, camps at night to recharge the battery, and opens supply pods on the way. Each pod opens fully only if the player solves a short **technical challenge in chemistry or physics** (electrolysis of water, burning hydrogen into water, energy = power x time, the inverse-square law for sunlight, shielding, recycling). The 30-sol mission then starts with what is left. All journey numbers are tagged Simplified in the code; the crew needs reuse the Real values.

The 3D version does not yet show the Real / Derived / Simplified tags or the sources panel; these stay in the web game.

- 3D version (playable in a browser): `<3D link>`

## AI-use statement

We used AI tools (Claude, by Anthropic) to help write the code, the automatic tests and the text of this project. Our team reviewed the results, checked the sources, and tested the game with people. All artwork is original inline SVG and CSS made for this project; in the 3D version, every model, texture and sound is generated by our own code at runtime. Neither version uses AI at runtime or makes network requests.

## Links

- Playable game: `<GitHub Pages link>`
- Bonus 3D version: `<3D link>`
- Source code: `<GitHub repository link>`
- 30-second video: `<video link>`

## References

All references are listed in the "NASA data and resources" section above and, with every value, tag and explanation, in `SOURCES.md` in the repository.

## Before you submit (team checklist)

- [ ] All four members appear in the "Members" tab.
- [ ] If the challenge page has a "Resources" tab, cite at least one resource listed there.
- [ ] Every item under "To verify by the team" in `SOURCES.md` is checked or removed.
- [ ] The game link and the repository link open in a private window.
- [ ] The video is 30 seconds or less, has English subtitles and shows the real game.
- [ ] The AI-use statement is included.
- [ ] Check the rules for any limit on showing minors in the demo.
- [ ] Agreement boxes are ticked and the Submit button is clicked.
