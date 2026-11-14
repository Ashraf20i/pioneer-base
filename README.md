# Pioneer Base

A turn-based browser game for ages 9 to 14. You run a **Moon** or **Mars** outpost and keep four astronauts safe for 30 mission days. Every day you plan, press **Run day**, and watch how power, oxygen, water, food and radiation pull against each other. At the end the **debrief** shows the exact chain of events and decisions that made the mission work or fail.

An independent student project for the NASA Space Apps Challenge 2026 (challenge: *Build a Junior Astronaut Mission Trainer*). Not affiliated with or endorsed by NASA.

## What makes it different

1. **The debrief.** A cause chain built from the real log of your game, a chart of your 30 days, the trade-off behind the result, and one thing to try next time.
2. **Honest data.** Every number on screen has a tag: Real, Derived or Simplified. The panel "Where do these numbers come from?" lists all 70+ numbers with sources. It is generated from the same file (`js/data.js`) that the simulation reads, so they cannot disagree.
3. **Playable by a child.** Short sentences, a live preview that shows what a plan will do before you commit, hints for the first three days, and a balance that was tested with scripted players.

## How to play

Open `index.html` in a browser. No install, no account, no internet needed.

1. Pick the Moon or Mars.
2. Each day: switch systems on or off, pick one crew activity (Science, Maintenance, Shelter), choose rations (Full, Lean, Half), and spend crates on new modules (they work from tomorrow). Step 5 holds repairs and the one-time rocket-fuel emergency.
3. Read "Today's preview". It shows energy made and needed, what would shut down, and "runs out in N days" for oxygen, water and food.
4. Press **Run day**. After day 30, or an evacuation, read the debrief.

A teacher guide (45-minute lesson, five discussion questions, printable) is in the game: press **Teacher guide**.

## Run the tests

You need Node.js 18 or newer. No packages to install.

```bash
npm test
```

This runs three files:

- `tests/engine.test.js`: no NaN, no negative stock, nothing above tank size, same seed gives the same game, shutdown order, builds, dust, radiation formula, Tier 2 faults and emergency, every ending has a cause chain of 3 to 6 steps, and no death wording.
- `tests/balance.test.js`: 200 seeds per site for the four scripted players, plus "impossible plan" searches. Takes about 20 seconds.
- `tests/data.test.js`: every data id used by the code exists, no network calls, no external files, footer text present.

Other commands:

```bash
node tests/trace.js moon 1 expert      # day-by-day trace of one scripted game
node tests/explore.js                  # quick balance summary
npm run sources                        # rebuild SOURCES.md from js/data.js
```

## Files

```
index.html          the page (includes the teacher guide)
css/style.css       styles
js/data.js          every constant with tag, explanation, source
js/sim.js           simulation engine (pure, no DOM)
js/debrief.js       cause chain, trade-off and tip builder (pure, no DOM)
js/ui.js            drawing and buttons
tests/              Node test scripts and scripted players
SOURCES.md          all values, tags, links, and "To verify by the team"
SUBMISSION.md       draft project page
DEMO_SCRIPT.md      shot-by-shot plan for the 30-second video
DECISIONS.md        choices, balance changes, results
```

## Publish on GitHub Pages

The game is static files, so any static host works.

1. Create a **public** repository on GitHub and push this folder (`git remote add origin <your repo url>` then `git push -u origin main`).
2. In the repository open **Settings, Pages**. Under "Build and deployment" choose **Deploy from a branch**, branch **main**, folder **/ (root)**, and save.
3. After a minute the game is at `https://<your-user>.github.io/<repo-name>/`.
4. Open that link in a private window and on a phone before you submit.

## Test with real people (needed before submission)

Ask at least three people who have never seen the game to finish one game with no spoken help. Ideally one is aged 9 to 14 (do not film them). Note where they stop. If most lose before day 12, raise the starting stocks in `js/data.js`, run `npm test`, and keep Real values unchanged.

## AI-use statement

AI tools (Claude, by Anthropic) assisted with writing the code, the tests and the text of this project. The team reviewed the result. The team is responsible for checking every Real value against its source (see "To verify by the team" in `SOURCES.md`).

## Credits and licence

All artwork is original inline SVG and CSS. No NASA logo or insignia is used. The data comes from public NASA pages and two scientific papers, listed in `SOURCES.md`. Code and text: MIT licence.
