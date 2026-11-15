/* Generates SOURCES.md from js/data.js so the file can never disagree with the game. node tests/make-sources.js */
'use strict';
var fs = require('fs'), path = require('path');
var PB = require('../js/data.js');
var out = [];
out.push('# Sources and data tags', '');
out.push('This file is generated from `js/data.js` (run `npm run sources`). The in-game panel "Where do these numbers come from?" is generated from the same file.', '');
out.push('**Tags.** Real = a measured or published number, locked, never changed to tune balance. Derived = calculated from Real numbers. Simplified = a game number, chosen on purpose and tuned by the automatic balance tests.', '');
var groups = {}, order = [];
PB.DATA.values.forEach(function (v) { if (!groups[v.group]) { groups[v.group] = []; order.push(v.group); } groups[v.group].push(v); });
order.forEach(function (g) {
  out.push('## ' + g, '', '| Value | Number | Tag | Meaning | Source |', '|---|---|---|---|---|');
  groups[g].forEach(function (v) {
    var src = v.url ? '[' + v.source + '](' + v.url + ')' : v.source;
    out.push('| ' + v.label + ' | ' + v.value + ' ' + v.unit + ' | ' + v.tag + ' | ' + v.explain.replace(/\|/g, '/') + ' | ' + src + ' |');
  });
  out.push('');
});
out.push('## "Myth or Reality?" cards', '', '| Card | Answer | Source |', '|---|---|---|');
PB.DATA.cards.forEach(function (c) { out.push('| ' + c.text + ' | ' + (c.myth ? 'Myth' : 'Reality') + ' | [' + c.source + '](' + c.url + ') |'); });
out.push('', '## Solar storm events', '',
  'The game places two real solar storms on mission days 7 and 20 of the Moon scenario (11 May 2024 and 3 October 2024) and one on day 23 of the Mars scenario (labelled with 3 October 2024). Event days move by up to one day from a seeded random generator. Source: NASA DONKI, <' + PB.DATA.links.donki + '>.', '',
  'The game does **not** claim that the October 2024 storm was seen on Mars. It only uses the date as the "real storm" the event is based on.', '');
out.push('## To verify by the team', '',
  'Checked by the build on 14 November 2026 with automated web requests. Open each link in a browser before you submit.', '',
  '1. **NASA DONKI flare details (event ID, class, peak time, link).** The API call `https://api.nasa.gov/DONKI/FLR?startDate=2024-05-10&endDate=2024-05-12&api_key=DEMO_KEY` answered `404 Not Found` (path `/DONKI-API/get/FLR/`) when the game was built, and so did the October 2024 call and other DONKI addresses we tried. We kept the two dates, show **no** flare ID or class in the game, and link to the DONKI tool page. If the API works for you, add the strongest flare of each day to `js/data.js` (`donki` object) and to the in-game alert.',
  '2. **Radiation on Mars (0.67 mSv/day).** The DOI link returns HTTP 403 to scripts because the publisher blocks automated requests. Open it in a browser and confirm the dose: Hassler et al. 2014 (<https://doi.org/10.1126/science.1244797>). The Moon dose (1.37 mSv/day) is now a Simplified game value, so that every source in the game is a NASA source.',
  '3. **Oxygen 0.84 kg/day and food 1.8 kg/day per astronaut.** We could not find one NASA page that states both numbers. The game links the NASA ECLSS reference page. Please confirm them in the NASA life-support baseline values document, or reword the source line.',
  '4. **Sunlight at Earth 1361 W/m2 (NASA TSIS-1).** The TSIS-1 page opens (HTTP 200), but a text search of the page did not find the number. Please confirm 1361 W/m2 in a NASA solar irradiance page and fix the link if needed.',
  '5. **Lunar night about 14.8 Earth days.** The NASA Moon Facts page confirms a lunar day of about 29 Earth days seen from Earth. The 14.8 days (half of that) is our reading. Please confirm the wording, or call this value Derived.',
  '6. **Mars distance 1.524 AU.** Confirmed in the NASA Mars Fact Sheet (semimajor axis 1.524 AU). The newer NASA Mars Facts page rounds it to 1.5 AU.',
  '7. **NASA career limit 600 mSv (NASA-STD-3001).** The NASA-STD-3001 page opens, but a text search did not find "600". Please confirm the number in the standard.',
  '8. **ISS water recovery 98%.** Confirmed on the NASA article "NASA Achieves Water Recovery Milestone on International Space Station" (it says the station can recycle 98% of the water collected from the US segment).',
  '9. **MOXIE.** The NASA article on the end of the MOXIE mission confirms that it separated oxygen from carbon dioxide in the Martian air.',
  '10. **InSight and dust.** The "Myth or Reality?" card says dust on solar panels ended InSight in 2022. Please read the linked NASA article and check the wording.',
  '11. **Mars air under 1% of Earth\'s.** From the NASA Mars Fact Sheet surface pressure (about 6.36 mb) against Earth (about 1013 mb). This is our own calculation (about 0.6%).',
  '12. **Links.** Every link opened with HTTP 200 on 14 November 2026 except the two DOI links (403, see item 2).', '');
fs.writeFileSync(path.join(__dirname, '..', 'SOURCES.md'), out.join('\n'));
console.log('SOURCES.md written');
