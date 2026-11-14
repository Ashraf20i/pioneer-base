/* Trace one scripted game: node tests/trace.js moon 1 expert */
'use strict';
var P = require('./players.js');
var site = process.argv[2] || 'moon', seed = +(process.argv[3] || 1), who = process.argv[4] || 'expert';
var pl = who === 'expert' ? P.expert() : who === 'decent' ? P.decent(seed, site) : who === 'naive' ? P.naive() : P.nothing();
var st = P.play(site, seed, pl);
function f(x) { return (Math.round(x * 10) / 10).toString(); }
st.history.forEach(function (h) {
  console.log('d' + h.day, 'sup', f(h.supply), 'use', f(h.used), 'bat', f(h.battery), 'o2', f(h.o2), 'w', f(h.water), 'food', f(h.food), 'hp', f(h.health), 'dose', f(h.dose), 'sci', h.science, h.activity, h.ration, h.shut.join('+'), h.noWater ? 'NOWATER' : '');
});
console.log(JSON.stringify(st.ending), 'mods', JSON.stringify(st.mod), 'crates', st.crates);
if (process.argv[5]) { console.log(JSON.stringify(require('../js/debrief.js').analyze(st), null, 1)); }
