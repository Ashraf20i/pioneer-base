# Sources and data tags

This file is generated from `js/data.js` (run `npm run sources`). The in-game panel "Where do these numbers come from?" is generated from the same file.

**Tags.** Real = a measured or published number, locked, never changed to tune balance. Derived = calculated from Real numbers. Simplified = a game number, chosen on purpose and tuned by the automatic balance tests.

## Real values

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Sunlight at Earth's distance | 1361 W/m² | Real | How strong sunlight is where Earth is. Mars gets less because it is farther away. | [NASA TSIS-1](https://science.nasa.gov/mission/tsis-1/) |
| Mars distance from the Sun | 1.524 AU | Real | Mars is about one and a half times farther from the Sun than Earth. | [NASA Mars Fact Sheet](https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html) |
| Length of the lunar night | 15 turns (days) | Real | One night on the Moon lasts about 14.8 Earth days. The game rounds to 15 turns. | [NASA Moon Facts](https://science.nasa.gov/moon/facts/) |
| Radiation on the Mars surface | 0.67 mSv/day | Real | How much radiation a person gets each day on Mars. Measured by the Curiosity rover. | [MSL Curiosity RAD (Hassler et al. 2014)](https://doi.org/10.1126/science.1244797) |
| Radiation on the Moon surface | 1.37 mSv/day | Real | How much radiation a person gets each day on the Moon. The Moon has no air to block it. This is a non-NASA measurement (China/Germany). | [Chang'e 4 LND (Zhang et al. 2020) - not NASA](https://doi.org/10.1126/sciadv.aaz1334) |
| Oxygen one astronaut breathes | 0.84 kg/day | Real | Four astronauts need 3.36 kg of oxygen every day. | [NASA life-support reference values](https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/) |
| Food one astronaut needs | 1.8 kg/day | Real | About 1.8 kg a day with packaging. Four astronauts need 7.2 kg. | [NASA life-support reference values](https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/) |
| Water recovered on the ISS | 98 % | Real | The space station recycles about 98% of its water. This is a fact to learn. The game outpost recycles 90%. | [NASA ECLSS milestone, 2023](https://www.nasa.gov/missions/station/iss-research/nasa-achieves-water-recovery-milestone-on-international-space-station/) |
| Real solar storms used as events | 11 May 2024, 3 Oct 2024 dates | Real | Two big flares from the Sun. The game places them on mission days 7 and 20 (Moon). | [NASA DONKI](https://ccmc.gsfc.nasa.gov/tools/DONKI/) |
| NASA career radiation limit | 600 mSv | Real | The most radiation an astronaut may collect in a whole career. The game uses a much smaller 40 mSv for one mission. | [NASA-STD-3001](https://www.nasa.gov/reference/nasa-std-3001/) |

## Derived values

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Sunlight at Mars compared with the Moon | 0.43 fraction | Derived | Light gets weaker with distance squared: 1 / 1.524² = 0.43. | [Calculated from the two values above](https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html) |
| Water for 4 kg of oxygen (Moon plant) | 4.5 L | Derived | Splitting water (2 H₂O → O₂ + 2 H₂) needs 1.125 kg of water for every 1 kg of oxygen. | Chemistry |
| Water the crew uses (before recycling) | 12 L/day | Derived | 4 astronauts × about 3 litres each for drinking, food and washing. | Calculated: 4 × 3 L |

## Starting stocks

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Oxygen at the start | 20 kg | Simplified | Oxygen in the tank on day 1. | Game value |
| Oxygen tank size | 40 kg | Simplified | The tank cannot hold more than this. | Game value |
| Water at the start | 60 L | Simplified | Water in the tank on day 1. | Game value |
| Water tank size | 120 L | Simplified | The tank cannot hold more than this. | Game value |
| Food at the start | 150 kg | Simplified | Food in the store on day 1. | Game value |
| Food store size | 250 kg | Simplified | The store cannot hold more than this. | Game value |
| Supply crates | 14 crates | Simplified | Crates you can spend on new modules. No more arrive. | Game value |
| Battery charge at the start | 5 ⚡ | Simplified | Stored energy on day 1. | Game value |
| Crew health at the start | 100 points | Simplified | Health goes from 0 to 100. | Game value |

## Power

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Solar array output on the Moon | 7 ⚡/day | Simplified | Each array makes this much in daylight, and nothing at night. | Game value |
| Solar array output on Mars | 3 ⚡/day | Simplified | About 43% of the Moon value (7 × 0.43), before dust. | Game value (7 × 43%) |
| Solar array cost | 2 crates | Simplified | Price of one array. | Game value |
| Most solar arrays | 6 arrays | Simplified | The outpost has room for this many. | Game value |
| Battery size | 10 ⚡ | Simplified | Each battery adds this much storage. | Game value |
| Battery cost | 2 crates | Simplified | Price of one battery. | Game value |
| Most batteries | 4 batteries | Simplified | Room for this many. | Game value |
| Fission reactor output | 8 ⚡/day | Simplified | Works day and night, in dust and in dark. | Game value |
| Fission reactor cost | 5 crates | Simplified | Price of the reactor. You can build only one. | Game value |
| Mars dust loss | 0.03 per day | Simplified | Dust settles on the arrays and cuts their power by 3% each day. | Game value |
| Lowest dust factor | 0.4 fraction | Simplified | Arrays never drop below 40% output. | Game value |
| Dust storm effect on solar | 0.15 fraction | Simplified | In a dust storm the arrays make only 15% of their power. | Game value |

## Systems

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Habitat core power | 4 ⚡/day | Simplified | Always on. Gives the crew air pressure, heat and water recycling. | Game value |
| Habitat water recycling | 0.9 fraction | Simplified | The outpost recycles 90% of crew water (the ISS reaches 98%). | Game value |
| Oxygen plant power (Moon) | 4 ⚡/day | Simplified | Moon plant splits water into oxygen (electrolysis). | Game value |
| Oxygen plant power (Mars) | 6 ⚡/day | Simplified | Mars plant takes oxygen from CO₂ in the air (like NASA's MOXIE test). It needs more power. | [Game value, MOXIE-inspired](https://www.nasa.gov/missions/mars-2020-perseverance/perseverance-rover/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/) |
| Oxygen plant output | 4 kg/day | Simplified | Oxygen made on a day the plant runs. | Game value |
| Science lab power | 3 ⚡/day | Simplified | The lab is the first system to shut down when power is short. | Game value |
| Science per day | 4 points | Simplified | Points earned on a day the lab runs and the crew does Science. | Game value |
| Greenhouse cost | 3 crates | Simplified | Price of one greenhouse. | Game value |
| Most greenhouses | 3 greenhouses | Simplified | Room for this many. | Game value |
| Greenhouse power | 3 ⚡/day | Simplified | Lights and pumps. | Game value |
| Greenhouse water | 2 L/day | Simplified | Plants need water every day or they stop growing. | Game value |
| Crop growing time | 10 days | Simplified | Real life is slower: lettuce on the ISS takes about a month. The game uses 10 days. | Game value (real: about 30 days) |
| Harvest size | 20 kg | Simplified | Food from one greenhouse after one full cycle. | Game value |
| Greenhouse oxygen | 0.3 kg/day | Simplified | Plants make a little oxygen. | Game value |
| Ice drill cost | 2 crates | Simplified | Price of one drill. | Game value |
| Most ice drills | 3 drills | Simplified | Room for this many. | Game value |
| Ice drill power | 3 ⚡/day | Simplified | Power one drill uses. | Game value |
| Ice drill water | 6 L/day | Simplified | Water from melted ice, each day a drill runs. | Game value |

## Radiation

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Shield layer cost | 2 crates | Simplified | Price of one layer. | Game value |
| Most shield layers | 3 layers | Simplified | Room for this many. | Game value |
| Shield layer effect | 0.75 per layer | Simplified | Each layer lets through 75% of the radiation. | Game value |
| Solar storm dose on the Moon | 12 mSv | Simplified | Extra radiation on a storm day if the crew is not sheltered. | Game value |
| Solar storm dose on Mars | 4 mSv | Simplified | Mars has a thin air layer that helps a little. | Game value |
| Shelter effect | 0.1 fraction | Simplified | Sheltering cuts the storm dose to one tenth. | Game value |
| Mission dose limit | 40 mSv | Simplified | Go over this and the crew is evacuated. NASA's career limit is 600 mSv. | Game value |

## Crew

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Full rations: health | 2 per day | Simplified | Crew gains health if all their needs are met. | Game value |
| Lean rations (75% food): health | -1 per day | Simplified | Less food, health slowly drops. | Game value |
| Half rations (50% food): health | -3 per day | Simplified | Much less food, health drops fast. | Game value |
| Lean ration size | 0.75 fraction | Simplified | The crew eats three quarters of normal. | Game value |
| Half ration size | 0.5 fraction | Simplified | The crew eats half of normal. | Game value |
| No oxygen: health | -30 per day | Simplified | Health lost on a day the oxygen runs short. | Game value |
| Habitat core off: health | -15 per day | Simplified | Health lost on a day the habitat has no power. | Game value |
| No water: health | -15 per day | Simplified | Health lost on a day the water runs out. | Game value |
| No food: health | -10 per day | Simplified | Health lost on a day the food runs out. | Game value |

## Faults and emergency

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Repair cost | 1 crate | Simplified | A broken module can be fixed with one crate, on the day you choose Repair. | Game value |
| Repair without crates | 3 Maintenance days | Simplified | With no crates, the crew needs three Maintenance days to fix a broken module. | Game value |
| Rocket fuel to water | 40 L | Simplified | One-time emergency: turn rocket fuel into water. | Game value |
| Emergency accident chance | 0.2 fraction | Simplified | A 20% chance that the emergency breaks one module. | Game value |

## Stars

| Value | Number | Tag | Meaning | Source |
|---|---|---|---|---|
| Two stars: science | 30 points | Simplified | Science needed for the second star. | Game value |
| Three stars: science | 45 points | Simplified | Science needed for the third star. | Game value |
| Three stars: health | 70 points | Simplified | Crew health needed at the end for the third star. | Game value |
| Three stars: dose (Moon) | 35 mSv | Simplified | Total dose must be at or under this. | Game value |
| Three stars: dose (Mars) | 22 mSv | Simplified | Total dose must be at or under this. | Game value |

## "Myth or Reality?" cards

| Card | Answer | Source |
|---|---|---|
| A Martian dust storm has winds strong enough to flip over a rocket. | Myth | [NASA Mars Fact Sheet (surface pressure about 6 mb, Earth about 1013 mb)](https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html) |
| Dust on solar panels can end a Mars mission. | Reality | [NASA, InSight mission retired](https://www.nasa.gov/missions/insight/nasa-retires-insight-mars-lander-mission-after-years-of-science/) |
| The space station recycles almost all of its water. | Reality | [NASA, water recovery milestone](https://www.nasa.gov/missions/station/iss-research/nasa-achieves-water-recovery-milestone-on-international-space-station/) |
| You can make oxygen from the air on Mars. | Reality | [NASA MOXIE](https://www.nasa.gov/missions/mars-2020-perseverance/perseverance-rover/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/) |
| The Moon has air that blocks space radiation, like Earth does. | Myth | [Chang'e 4 (Zhang et al. 2020)](https://doi.org/10.1126/sciadv.aaz1334) |
| A night on the Moon lasts only a few hours. | Myth | [NASA Moon Facts](https://science.nasa.gov/moon/facts/) |

## Solar storm events

The game places two real solar storms on mission days 7 and 20 of the Moon scenario (11 May 2024 and 3 October 2024) and one on day 23 of the Mars scenario (labelled with 3 October 2024). Event days move by up to one day from a seeded random generator. Source: NASA DONKI, <https://ccmc.gsfc.nasa.gov/tools/DONKI/>.

The game does **not** claim that the October 2024 storm was seen on Mars. It only uses the date as the "real storm" the event is based on.

## To verify by the team

Checked by the build on 14 November 2026 with automated web requests. Open each link in a browser before you submit.

1. **NASA DONKI flare details (event ID, class, peak time, link).** The API call `https://api.nasa.gov/DONKI/FLR?startDate=2024-05-10&endDate=2024-05-12&api_key=DEMO_KEY` answered `404 Not Found` (path `/DONKI-API/get/FLR/`) when the game was built, and so did the October 2024 call and other DONKI addresses we tried. We kept the two dates, show **no** flare ID or class in the game, and link to the DONKI tool page. If the API works for you, add the strongest flare of each day to `js/data.js` (`donki` object) and to the in-game alert.
2. **Radiation numbers (0.67 and 1.37 mSv/day).** The two DOI links return HTTP 403 to scripts because the publisher blocks automated requests. Open them in a browser and confirm the dose numbers: Hassler et al. 2014 (<https://doi.org/10.1126/science.1244797>) and Zhang et al. 2020 (<https://doi.org/10.1126/sciadv.aaz1334>). The Moon value is a non-NASA measurement and is labelled that way in the game.
3. **Oxygen 0.84 kg/day and food 1.8 kg/day per astronaut.** We could not find one NASA page that states both numbers. The game links the NASA ECLSS reference page. Please confirm them in the NASA life-support baseline values document, or reword the source line.
4. **Sunlight at Earth 1361 W/m2 (NASA TSIS-1).** The TSIS-1 page opens (HTTP 200), but a text search of the page did not find the number. Please confirm 1361 W/m2 in a NASA solar irradiance page and fix the link if needed.
5. **Lunar night about 14.8 Earth days.** The NASA Moon Facts page confirms a lunar day of about 29 Earth days seen from Earth. The 14.8 days (half of that) is our reading. Please confirm the wording, or call this value Derived.
6. **Mars distance 1.524 AU.** Confirmed in the NASA Mars Fact Sheet (semimajor axis 1.524 AU). The newer NASA Mars Facts page rounds it to 1.5 AU.
7. **NASA career limit 600 mSv (NASA-STD-3001).** The NASA-STD-3001 page opens, but a text search did not find "600". Please confirm the number in the standard.
8. **ISS water recovery 98%.** Confirmed on the NASA article "NASA Achieves Water Recovery Milestone on International Space Station" (it says the station can recycle 98% of the water collected from the US segment).
9. **MOXIE.** The NASA article on the end of the MOXIE mission confirms that it separated oxygen from carbon dioxide in the Martian air.
10. **InSight and dust.** The "Myth or Reality?" card says dust on solar panels ended InSight in 2022. Please read the linked NASA article and check the wording.
11. **Mars air under 1% of Earth's.** From the NASA Mars Fact Sheet surface pressure (about 6.36 mb) against Earth (about 1013 mb). This is our own calculation (about 0.6%).
12. **Links.** Every link opened with HTTP 200 on 14 November 2026 except the two DOI links (403, see item 2).
