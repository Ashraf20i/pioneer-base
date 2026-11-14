/* Pioneer Base - data.js
 * Every constant the simulation reads lives here, with a tag, a one-line
 * explanation and a source. The "Where do these numbers come from?" panel is
 * generated from this same list, so the game and the panel cannot disagree.
 *
 * Tags:  Real = measured/published value, never changed to tune balance.
 *        Derived = calculated from Real values.
 *        Simplified = game value, tuned by the balance tests.
 */
(function (root) {
  'use strict';
  var PB = root.PB = root.PB || {};

  var NASA = {
    tsis: 'https://science.nasa.gov/mission/tsis-1/',
    marsfs: 'https://nssdc.gsfc.nasa.gov/planetary/factsheet/marsfact.html',
    marsfacts: 'https://science.nasa.gov/mars/facts/',
    moonfacts: 'https://science.nasa.gov/moon/facts/',
    msl: 'https://doi.org/10.1126/science.1244797',
    change4: 'https://doi.org/10.1126/sciadv.aaz1334',
    eclss: 'https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/',
    iss98: 'https://www.nasa.gov/missions/station/iss-research/nasa-achieves-water-recovery-milestone-on-international-space-station/',
    donki: 'https://ccmc.gsfc.nasa.gov/tools/DONKI/',
    std3001: 'https://www.nasa.gov/reference/nasa-std-3001/',
    insight: 'https://science.nasa.gov/mission/insight/',
    moxie: 'https://www.nasa.gov/missions/mars-2020-perseverance/perseverance-rover/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/'
  };

  // id, group, label, value, unit, tag, explain, source, url
  var rows = [
    // ---------------- REAL (locked) ----------------
    ['sun_earth', 'Real values', 'Sunlight at Earth\'s distance', 1361, 'W/m²', 'Real', 'How strong sunlight is where Earth is. Mars gets less because it is farther away.', 'NASA TSIS-1', NASA.tsis],
    ['mars_au', 'Real values', 'Mars distance from the Sun', 1.524, 'AU', 'Real', 'Mars is about one and a half times farther from the Sun than Earth.', 'NASA Mars Fact Sheet', NASA.marsfs],
    ['lunar_night', 'Real values', 'Length of the lunar night', 15, 'turns (days)', 'Real', 'One night on the Moon lasts about 14.8 Earth days. The game rounds to 15 turns.', 'NASA Moon Facts', NASA.moonfacts],
    ['dose_mars', 'Real values', 'Radiation on the Mars surface', 0.67, 'mSv/day', 'Real', 'How much radiation a person gets each day on Mars. Measured by the Curiosity rover.', 'MSL Curiosity RAD (Hassler et al. 2014)', NASA.msl],
    ['dose_moon', 'Real values', 'Radiation on the Moon surface', 1.37, 'mSv/day', 'Real', 'How much radiation a person gets each day on the Moon. The Moon has no air to block it. This is a non-NASA measurement (China/Germany).', 'Chang\'e 4 LND (Zhang et al. 2020) - not NASA', NASA.change4],
    ['o2_person', 'Real values', 'Oxygen one astronaut breathes', 0.84, 'kg/day', 'Real', 'Four astronauts need 3.36 kg of oxygen every day.', 'NASA life-support reference values', NASA.eclss],
    ['food_person', 'Real values', 'Food one astronaut needs', 1.8, 'kg/day', 'Real', 'About 1.8 kg a day with packaging. Four astronauts need 7.2 kg.', 'NASA life-support reference values', NASA.eclss],
    ['iss_water', 'Real values', 'Water recovered on the ISS', 98, '%', 'Real', 'The space station recycles about 98% of its water. This is a fact to learn. The game outpost recycles 90%.', 'NASA ECLSS milestone, 2023', NASA.iss98],
    ['storm_dates', 'Real values', 'Real solar storms used as events', '11 May 2024, 3 Oct 2024', 'dates', 'Real', 'Two big flares from the Sun. The game places them on mission days 7 and 20 (Moon).', 'NASA DONKI', NASA.donki],
    ['career_limit', 'Real values', 'NASA career radiation limit', 600, 'mSv', 'Real', 'The most radiation an astronaut may collect in a whole career. The game uses a much smaller 40 mSv for one mission.', 'NASA-STD-3001', NASA.std3001],
    // ---------------- DERIVED ----------------
    ['mars_sun_ratio', 'Derived values', 'Sunlight at Mars compared with the Moon', 0.43, 'fraction', 'Derived', 'Light gets weaker with distance squared: 1 / 1.524² = 0.43.', 'Calculated from the two values above', NASA.marsfs],
    ['o2p_water_moon', 'Derived values', 'Water for 4 kg of oxygen (Moon plant)', 4.5, 'L', 'Derived', 'Splitting water (2 H₂O → O₂ + 2 H₂) needs 1.125 kg of water for every 1 kg of oxygen.', 'Chemistry', ''],
    ['crew_water', 'Derived values', 'Water the crew uses (before recycling)', 12, 'L/day', 'Derived', '4 astronauts × about 3 litres each for drinking, food and washing.', 'Calculated: 4 × 3 L', ''],
    // ---------------- SIMPLIFIED: stocks ----------------
    ['o2_start', 'Starting stocks', 'Oxygen at the start', 20, 'kg', 'Simplified', 'Oxygen in the tank on day 1.', 'Game value', ''],
    ['o2_cap', 'Starting stocks', 'Oxygen tank size', 40, 'kg', 'Simplified', 'The tank cannot hold more than this.', 'Game value', ''],
    ['water_start', 'Starting stocks', 'Water at the start', 60, 'L', 'Simplified', 'Water in the tank on day 1.', 'Game value', ''],
    ['water_cap', 'Starting stocks', 'Water tank size', 120, 'L', 'Simplified', 'The tank cannot hold more than this.', 'Game value', ''],
    ['food_start', 'Starting stocks', 'Food at the start', 150, 'kg', 'Simplified', 'Food in the store on day 1.', 'Game value', ''],
    ['food_cap', 'Starting stocks', 'Food store size', 250, 'kg', 'Simplified', 'The store cannot hold more than this.', 'Game value', ''],
    ['crates_start', 'Starting stocks', 'Supply crates', 14, 'crates', 'Simplified', 'Crates you can spend on new modules. No more arrive.', 'Game value', ''],
    ['battery_start', 'Starting stocks', 'Battery charge at the start', 5, '⚡', 'Simplified', 'Stored energy on day 1.', 'Game value', ''],
    ['health_start', 'Starting stocks', 'Crew health at the start', 100, 'points', 'Simplified', 'Health goes from 0 to 100.', 'Game value', ''],
    // ---------------- SIMPLIFIED: power ----------------
    ['solar_moon', 'Power', 'Solar array output on the Moon', 7, '⚡/day', 'Simplified', 'Each array makes this much in daylight, and nothing at night.', 'Game value', ''],
    ['solar_mars', 'Power', 'Solar array output on Mars', 3, '⚡/day', 'Simplified', 'About 43% of the Moon value (7 × 0.43), before dust.', 'Game value (7 × 43%)', ''],
    ['solar_cost', 'Power', 'Solar array cost', 2, 'crates', 'Simplified', 'Price of one array.', 'Game value', ''],
    ['solar_max', 'Power', 'Most solar arrays', 6, 'arrays', 'Simplified', 'The outpost has room for this many.', 'Game value', ''],
    ['battery_per', 'Power', 'Battery size', 10, '⚡', 'Simplified', 'Each battery adds this much storage.', 'Game value', ''],
    ['battery_cost', 'Power', 'Battery cost', 2, 'crates', 'Simplified', 'Price of one battery.', 'Game value', ''],
    ['battery_max', 'Power', 'Most batteries', 4, 'batteries', 'Simplified', 'Room for this many.', 'Game value', ''],
    ['reactor_out', 'Power', 'Fission reactor output', 8, '⚡/day', 'Simplified', 'Works day and night, in dust and in dark.', 'Game value', ''],
    ['reactor_cost', 'Power', 'Fission reactor cost', 5, 'crates', 'Simplified', 'Price of the reactor. You can build only one.', 'Game value', ''],
    ['dust_loss', 'Power', 'Mars dust loss', 0.03, 'per day', 'Simplified', 'Dust settles on the arrays and cuts their power by 3% each day.', 'Game value', ''],
    ['dust_floor', 'Power', 'Lowest dust factor', 0.4, 'fraction', 'Simplified', 'Arrays never drop below 40% output.', 'Game value', ''],
    ['dust_storm_mult', 'Power', 'Dust storm effect on solar', 0.15, 'fraction', 'Simplified', 'In a dust storm the arrays make only 15% of their power.', 'Game value', ''],
    // ---------------- SIMPLIFIED: systems ----------------
    ['hab_power', 'Systems', 'Habitat core power', 4, '⚡/day', 'Simplified', 'Always on. Gives the crew air pressure, heat and water recycling.', 'Game value', ''],
    ['hab_recycle', 'Systems', 'Habitat water recycling', 0.9, 'fraction', 'Simplified', 'The outpost recycles 90% of crew water (the ISS reaches 98%).', 'Game value', ''],
    ['o2p_power_moon', 'Systems', 'Oxygen plant power (Moon)', 4, '⚡/day', 'Simplified', 'Moon plant splits water into oxygen (electrolysis).', 'Game value', ''],
    ['o2p_power_mars', 'Systems', 'Oxygen plant power (Mars)', 6, '⚡/day', 'Simplified', 'Mars plant takes oxygen from CO₂ in the air (like NASA\'s MOXIE test). It needs more power.', 'Game value, MOXIE-inspired', NASA.moxie],
    ['o2p_out', 'Systems', 'Oxygen plant output', 4, 'kg/day', 'Simplified', 'Oxygen made on a day the plant runs.', 'Game value', ''],
    ['lab_power', 'Systems', 'Science lab power', 3, '⚡/day', 'Simplified', 'The lab is the first system to shut down when power is short.', 'Game value', ''],
    ['lab_science', 'Systems', 'Science per day', 4, 'points', 'Simplified', 'Points earned on a day the lab runs and the crew does Science.', 'Game value', ''],
    ['gh_cost', 'Systems', 'Greenhouse cost', 3, 'crates', 'Simplified', 'Price of one greenhouse.', 'Game value', ''],
    ['gh_max', 'Systems', 'Most greenhouses', 3, 'greenhouses', 'Simplified', 'Room for this many.', 'Game value', ''],
    ['gh_power', 'Systems', 'Greenhouse power', 3, '⚡/day', 'Simplified', 'Lights and pumps.', 'Game value', ''],
    ['gh_water', 'Systems', 'Greenhouse water', 2, 'L/day', 'Simplified', 'Plants need water every day or they stop growing.', 'Game value', ''],
    ['gh_cycle', 'Systems', 'Crop growing time', 10, 'days', 'Simplified', 'Real life is slower: lettuce on the ISS takes about a month. The game uses 10 days.', 'Game value (real: about 30 days)', ''],
    ['gh_yield', 'Systems', 'Harvest size', 20, 'kg', 'Simplified', 'Food from one greenhouse after one full cycle.', 'Game value', ''],
    ['gh_o2', 'Systems', 'Greenhouse oxygen', 0.3, 'kg/day', 'Simplified', 'Plants make a little oxygen.', 'Game value', ''],
    ['drill_cost', 'Systems', 'Ice drill cost', 2, 'crates', 'Simplified', 'Price of one drill.', 'Game value', ''],
    ['drill_max', 'Systems', 'Most ice drills', 3, 'drills', 'Simplified', 'Room for this many.', 'Game value', ''],
    ['drill_power', 'Systems', 'Ice drill power', 3, '⚡/day', 'Simplified', 'Power one drill uses.', 'Game value', ''],
    ['drill_out', 'Systems', 'Ice drill water', 6, 'L/day', 'Simplified', 'Water from melted ice, each day a drill runs.', 'Game value', ''],
    // ---------------- SIMPLIFIED: radiation ----------------
    ['shield_cost', 'Radiation', 'Shield layer cost', 2, 'crates', 'Simplified', 'Price of one layer.', 'Game value', ''],
    ['shield_max', 'Radiation', 'Most shield layers', 3, 'layers', 'Simplified', 'Room for this many.', 'Game value', ''],
    ['shield_factor', 'Radiation', 'Shield layer effect', 0.75, 'per layer', 'Simplified', 'Each layer lets through 75% of the radiation.', 'Game value', ''],
    ['storm_moon', 'Radiation', 'Solar storm dose on the Moon', 12, 'mSv', 'Simplified', 'Extra radiation on a storm day if the crew is not sheltered.', 'Game value', ''],
    ['storm_mars', 'Radiation', 'Solar storm dose on Mars', 4, 'mSv', 'Simplified', 'Mars has a thin air layer that helps a little.', 'Game value', ''],
    ['shelter_factor', 'Radiation', 'Shelter effect', 0.1, 'fraction', 'Simplified', 'Sheltering cuts the storm dose to one tenth.', 'Game value', ''],
    ['dose_limit', 'Radiation', 'Mission dose limit', 40, 'mSv', 'Simplified', 'Go over this and the crew is evacuated. NASA\'s career limit is 600 mSv.', 'Game value', ''],
    // ---------------- SIMPLIFIED: crew ----------------
    ['ration_full', 'Crew', 'Full rations: health', 2, 'per day', 'Simplified', 'Crew gains health if all their needs are met.', 'Game value', ''],
    ['ration_lean', 'Crew', 'Lean rations (75% food): health', -1, 'per day', 'Simplified', 'Less food, health slowly drops.', 'Game value', ''],
    ['ration_half', 'Crew', 'Half rations (50% food): health', -3, 'per day', 'Simplified', 'Much less food, health drops fast.', 'Game value', ''],
    ['lean_factor', 'Crew', 'Lean ration size', 0.75, 'fraction', 'Simplified', 'The crew eats three quarters of normal.', 'Game value', ''],
    ['half_factor', 'Crew', 'Half ration size', 0.5, 'fraction', 'Simplified', 'The crew eats half of normal.', 'Game value', ''],
    ['pen_o2', 'Crew', 'No oxygen: health', -30, 'per day', 'Simplified', 'Health lost on a day the oxygen runs short.', 'Game value', ''],
    ['pen_hab', 'Crew', 'Habitat core off: health', -15, 'per day', 'Simplified', 'Health lost on a day the habitat has no power.', 'Game value', ''],
    ['pen_water', 'Crew', 'No water: health', -15, 'per day', 'Simplified', 'Health lost on a day the water runs out.', 'Game value', ''],
    ['pen_food', 'Crew', 'No food: health', -10, 'per day', 'Simplified', 'Health lost on a day the food runs out.', 'Game value', ''],
    // ---------------- SIMPLIFIED: stars ----------------
    ['star2_science', 'Stars', 'Two stars: science', 30, 'points', 'Simplified', 'Science needed for the second star.', 'Game value', ''],
    ['star3_science', 'Stars', 'Three stars: science', 45, 'points', 'Simplified', 'Science needed for the third star.', 'Game value', ''],
    ['star3_health', 'Stars', 'Three stars: health', 70, 'points', 'Simplified', 'Crew health needed at the end for the third star.', 'Game value', ''],
    ['star3_dose_moon', 'Stars', 'Three stars: dose (Moon)', 35, 'mSv', 'Simplified', 'Total dose must be at or under this.', 'Game value', ''],
    ['star3_dose_mars', 'Stars', 'Three stars: dose (Mars)', 22, 'mSv', 'Simplified', 'Total dose must be at or under this.', 'Game value', '']
  ];

  var values = [], byId = {};
  rows.forEach(function (r) {
    var o = { id: r[0], group: r[1], label: r[2], value: r[3], unit: r[4], tag: r[5], explain: r[6], source: r[7], url: r[8] };
    values.push(o); byId[o.id] = o;
  });

  var TAGS = {
    Real: { icon: '●', word: 'Real', help: 'A measured or published number.' },
    Derived: { icon: '◆', word: 'Derived', help: 'Worked out from Real numbers.' },
    Simplified: { icon: '▲', word: 'Simplified', help: 'A game number, made simple on purpose.' }
  };

  // Scenario timelines. Event days vary by +/-1 from the seeded generator.
  var scenarios = {
    moon: {
      name: 'Moon', title: 'Lunar outpost',
      nightStart: 11, nightEnd: 25,
      storms: [
        { day: 7, alert: 1, real: '11 May 2024' },
        { day: 20, alert: 1, real: '3 Oct 2024' }
      ],
      dust: null
    },
    mars: {
      name: 'Mars', title: 'Martian outpost',
      nightStart: 0, nightEnd: 0,
      storms: [{ day: 23, alert: 1, real: '3 Oct 2024' }],
      dust: { start: 12, end: 17, alert: 2 }
    }
  };

  // DONKI details. The API did not answer when we built the game, so we show no
  // flare ID or class. See SOURCES.md "To verify by the team".
  var donki = {
    '11 May 2024': { confirmed: false, link: NASA.donki },
    '3 Oct 2024': { confirmed: false, link: NASA.donki }
  };

  var cards = [
    { id: 'c1', afterDay: 5, text: 'A Martian dust storm has winds strong enough to flip over a rocket.', myth: true,
      why: 'Myth! Mars air is under 1% as thick as Earth\'s, so even fast wind pushes very gently. The real danger is the fine dust: it blocks sunlight and covers solar panels.',
      source: 'NASA Mars Facts', url: NASA.marsfacts },
    { id: 'c2', afterDay: 9, text: 'Dust on solar panels can end a Mars mission.', myth: false,
      why: 'Reality! NASA\'s InSight lander ended its mission in 2022 when dust left its solar panels without enough power.',
      source: 'NASA InSight mission', url: NASA.insight },
    { id: 'c3', afterDay: 14, text: 'The space station recycles almost all of its water.', myth: false,
      why: 'Reality! NASA reported in 2023 that the station can recover about 98% of its water, even from urine and sweat.',
      source: 'NASA, water recovery milestone', url: NASA.iss98 },
    { id: 'c4', afterDay: 19, text: 'You can make oxygen from the air on Mars.', myth: false,
      why: 'Reality! A tool called MOXIE on the Perseverance rover made oxygen from Martian carbon dioxide. It made a small amount, as a test.',
      source: 'NASA MOXIE', url: NASA.moxie },
    { id: 'c5', afterDay: 23, text: 'The Moon has air that blocks space radiation, like Earth does.', myth: true,
      why: 'Myth! The Moon has almost no air. A lander measured about 1.37 mSv of radiation every day on the far side surface. That is why shielding matters.',
      source: 'Chang\'e 4 (Zhang et al. 2020)', url: NASA.change4 },
    { id: 'c6', afterDay: 27, text: 'A night on the Moon lasts only a few hours.', myth: true,
      why: 'Myth! One lunar night lasts about 14.8 Earth days. Solar panels make no power the whole time.',
      source: 'NASA Moon Facts', url: NASA.moonfacts }
  ];

  PB.DATA = { values: values, byId: byId, tags: TAGS, scenarios: scenarios, donki: donki, cards: cards, links: NASA };
  PB.val = function (id) {
    var o = byId[id];
    if (!o) { throw new Error('Unknown data id: ' + id); }
    return o.value;
  };
  PB.tag = function (id) { return byId[id] ? byId[id].tag : 'Simplified'; };

  if (typeof module !== 'undefined' && module.exports) { module.exports = PB; }
})(typeof globalThis !== 'undefined' ? globalThis : window);
