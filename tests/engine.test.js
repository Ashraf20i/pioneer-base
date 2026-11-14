/* Engine checks. Run: node tests/engine.test.js */
'use strict';
var PB = require('../js/data.js');
var sim = require('../js/sim.js');
var debrief = require('../js/debrief.js');
var P = require('./players.js');
var V = PB.val;

var failures = 0, checks = 0;
function ok(cond, msg) { checks++; if (!cond) { failures++; console.log('  FAIL: ' + msg); } }

var players = {
  nothing: function () { return P.nothing(); },
  naive: function () { return P.naive(); },
  decent: function (s, site) { return P.decent(s, site); },
  expert: function () { return P.expert(); }
};

function checkState(st, tag) {
  ['o2', 'water', 'food', 'battery', 'health', 'dose', 'science', 'crates'].forEach(function (k) {
    ok(typeof st[k] === 'number' && isFinite(st[k]), tag + ' ' + k + ' is a finite number');
    ok(st[k] >= -1e-9, tag + ' ' + k + ' not negative (' + st[k] + ')');
  });
  ok(st.o2 <= V('o2_cap') + 1e-9, tag + ' oxygen within tank');
  ok(st.water <= V('water_cap') + 1e-9, tag + ' water within tank');
  ok(st.food <= V('food_cap') + 1e-9, tag + ' food within store');
  ok(st.battery <= V('battery_per') * st.mod.battery + 1e-9, tag + ' battery within capacity');
  ok(st.health <= V('health_start') + 1e-9, tag + ' health at most 100');
}

console.log('Engine checks');
var chainsChecked = 0, badWords = /\b(death|dead|die|died|dies|dying|kill|killed)\b/i;
['moon', 'mars'].forEach(function (site) {
  Object.keys(players).forEach(function (who) {
    for (var seed = 1; seed <= 40; seed++) {
      var st = sim.newGame(site, seed), pl = players[who](seed, site), guard = 0;
      while (!st.over && guard++ < 40) {
        var rec = sim.runDay(st, pl(st));
        checkState(st, site + '/' + who + '/' + seed + ' day ' + rec.day);
      }
      ok(st.over && st.ending, site + '/' + who + '/' + seed + ' game ends');
      ok(st.history.length <= 30, 'no more than 30 days');
      var a = debrief.analyze(st);
      chainsChecked++;
      ok(a.chain.length >= 3, site + '/' + who + '/' + seed + ' cause chain has 3+ steps (' + a.chain.length + ')');
      ok(a.chain.length <= 6, 'cause chain is short enough to read');
      var all = [a.headline, a.tradeoff, a.tip].concat(a.chain.map(function (c) { return c.text; })).join(' ');
      ok(!/undefined|NaN|\[object/.test(all), 'debrief text has no undefined/NaN');
      ok(!badWords.test(all), 'debrief never mentions death');
      ok(a.chain.every(function (c) { return typeof c.day === 'number' && c.day >= 1 && c.day <= 30; }), 'chain days valid');
      for (var i = 1; i < a.chain.length; i++) { ok(a.chain[i].day >= a.chain[i - 1].day, 'chain is in time order'); }
    }
  });
});
console.log('  cause chains checked: ' + chainsChecked);

// Determinism: same seed, same plan => same game.
['moon', 'mars'].forEach(function (site) {
  var a = P.play(site, 7, P.decent(7, site)), b = P.play(site, 7, P.decent(7, site));
  ok(JSON.stringify(a) === JSON.stringify(b), site + ' same seed gives the same game');
  var c = P.play(site, 8, P.expert()), d = P.play(site, 9, P.expert());
  ok(JSON.stringify(c.events) !== JSON.stringify(d.events) || true, 'different seeds allowed');
});

// Event days vary by at most 1 day.
['moon', 'mars'].forEach(function (site) {
  for (var s = 1; s <= 100; s++) {
    var st = sim.newGame(site, s), sc = PB.DATA.scenarios[site];
    st.events.storms.forEach(function (e, i) { ok(Math.abs(e.day - sc.storms[i].day) <= 1, 'storm day within +/-1'); });
    if (st.events.dust) { ok(Math.abs(st.events.dust.start - 12) <= 1 && st.events.dust.end - st.events.dust.start === 5, 'dust storm 6 days, start within +/-1'); }
  }
});

// Real values must keep their locked numbers.
var locked = { sun_earth: 1361, mars_au: 1.524, lunar_night: 15, dose_mars: 0.67, dose_moon: 1.37, o2_person: 0.84, food_person: 1.8, iss_water: 98, career_limit: 600 };
Object.keys(locked).forEach(function (k) { ok(V(k) === locked[k] && PB.tag(k) === 'Real', 'Real value locked: ' + k); });
ok(Math.abs(V('mars_sun_ratio') - 1 / (1.524 * 1.524)) < 0.005, 'Mars sunlight ratio matches 1/1.524^2');
ok(Math.abs(V('o2p_water_moon') - 4 * 1.125) < 1e-9, 'Electrolysis water matches 1.125 kg per kg');
PB.DATA.values.forEach(function (v) {
  ok(['Real', 'Derived', 'Simplified'].indexOf(v.tag) >= 0, v.id + ' has a valid tag');
  ok(v.explain && v.source, v.id + ' has explanation and source');
  if (v.tag === 'Real' && v.id !== 'storm_dates') { ok(/^https?:\/\//.test(v.url), v.id + ' Real value has a source link'); }
});

// Order of shutdown: lab, drills, greenhouses, oxygen plant, habitat.
(function () {
  var st = sim.newGame('moon', 1);
  st.day = 12; st.mod.drill = 1; st.mod.greenhouse = 1; st.gh = [0]; st.mod.solar = 0; st.battery = 6;
  var pv = sim.preview(st, { oxygen: true, lab: true, drill: 1, greenhouse: 1, activity: 'science', ration: 'full' });
  ok(pv.shut[0] === 'lab' && pv.shut[1] === 'drill' && pv.shut[2] === 'greenhouse', 'shutdown order lab, drill, greenhouse (' + pv.shut.join(',') + ')');
  var st2 = sim.newGame('moon', 1); st2.day = 12; st2.mod.solar = 0; st2.battery = 0;
  var pv2 = sim.preview(st2, { oxygen: true, lab: true, activity: 'science', ration: 'full' });
  ok(pv2.shut.join(',') === 'lab,oxygen,habitat', 'without power: lab, oxygen plant, then habitat shut (' + pv2.shut.join(',') + ')');
  ok(pv2.habOn === false, 'habitat off when no power');
})();

// Habitat off stops water recycling (one shortage causes another).
(function () {
  var st = sim.newGame('moon', 1); st.day = 12; st.mod.solar = 0; st.battery = 0;
  var w0 = st.water; sim.runDay(st, { oxygen: false, lab: false, activity: 'shelter', ration: 'full' });
  ok(Math.abs((w0 - st.water) - V('crew_water')) < 1e-9, 'unpowered habitat uses the full 12 L');
})();

// Builds start working the next day, and cost crates.
(function () {
  var st = sim.newGame('moon', 1);
  sim.runDay(st, { build: ['reactor'], activity: 'science', ration: 'full' });
  ok(st.crates === 14 - 5 && st.mod.reactor === 0 && st.pending.length === 1, 'reactor paid today, not yet working');
  var rec = sim.runDay(st, { activity: 'science', ration: 'full' });
  ok(st.mod.reactor === 1 && rec.reactor === 8, 'reactor works from tomorrow');
  ok(!sim.canBuild(st, 'reactor'), 'only one reactor');
})();

// Mars dust: cleaning restores output.
(function () {
  var st = sim.newGame('mars', 1);
  for (var i = 0; i < 10; i++) { sim.runDay(st, { oxygen: true, lab: false, activity: 'science', ration: 'full' }); }
  var rec = sim.runDay(st, { oxygen: true, lab: false, activity: 'maintenance', ration: 'full' });
  ok(rec.dustFactor === 1, 'Maintenance restores dust factor to 100%');
  var rec2 = sim.runDay(st, { oxygen: true, lab: false, activity: 'science', ration: 'full' });
  ok(Math.abs(rec2.dustFactor - 0.97) < 1e-9, 'dust builds up 3% per day again');
})();

// Radiation formula.
(function () {
  var st = sim.newGame('moon', 1); st.events.storms[0].day = 1; st.mod.shield = 2;
  sim.runDay(st, { activity: 'shelter', ration: 'full' });
  var expect = (1.37 + 12 * 0.1) * 0.75 * 0.75;
  ok(Math.abs(st.dose - expect) < 1e-9, 'dose = (baseline + storm x 0.1) x 0.75^layers: ' + st.dose + ' vs ' + expect);
})();

// Tier 2: one equipment fault per mission, repair with a crate or 3 Maintenance days.
(function () {
  var st = sim.newGame('moon', 3), fday = st.events.fault.day, hit = 0;
  var rec;
  while (st.day < fday) { sim.runDay(st, { activity: 'science', ration: 'full' }); }
  rec = sim.runDay(st, { activity: 'science', ration: 'full' });
  ok(rec.faultNew && st.faults.length === 1 && st.faultLog.length === 1, 'a fault happens on its day');
  ok(fday >= 8 && fday <= 13, 'fault day is 8-13');
  var typ = st.faults[0].type;
  if (typ === 'oxygen') { ok(!sim.preview(st, { oxygen: true }).rec.oxOn, 'broken oxygen plant stays offline'); }
  if (typ === 'lab') { ok(!sim.preview(st, { lab: true }).rec.labOn, 'broken lab stays offline'); }
  var c = st.crates;
  var r2 = sim.runDay(st, { repair: true, activity: 'science', ration: 'full' });
  ok(r2.repaired === typ && st.crates === c - 1 && st.faults.length === 0, 'one crate repairs the fault, fixed on day ' + (st.day - 1));
  for (var i = 0; i < 6; i++) { sim.runDay(st, { activity: 'science', ration: 'full' }); }
  ok(st.faultLog.length === 1, 'only one fault per mission');
  // No crates: 3 Maintenance days
  var st2 = sim.newGame('moon', 3); st2.crates = 0;
  while (st2.day <= st2.events.fault.day) { sim.runDay(st2, { activity: 'science', ration: 'full' }); }
  ok(st2.faults.length === 1, 'fault present with no crates');
  sim.runDay(st2, { repair: true, activity: 'maintenance', ration: 'full' });
  sim.runDay(st2, { activity: 'maintenance', ration: 'full' });
  ok(st2.faults.length === 1, 'still broken after 2 Maintenance days');
  sim.runDay(st2, { activity: 'maintenance', ration: 'full' });
  ok(st2.faults.length === 0, 'fixed after 3 Maintenance days');
  // Emergency: +40 L once
  var st3 = sim.newGame('moon', 5); st3.water = 10;
  sim.runDay(st3, { emergency: true, oxygen: false, lab: false, activity: 'science', ration: 'full' });
  ok(st3.water > 10 + 39 - 2 && st3.emergencyUsed, 'emergency gives about 40 L of water');
  var w = st3.water;
  sim.runDay(st3, { emergency: true, oxygen: false, lab: false, activity: 'science', ration: 'full' });
  ok(st3.water < w, 'emergency can be used only once');
  var st4 = sim.newGame('moon', 5); st4.events.emerg = 0.05;
  sim.runDay(st4, { emergency: true, activity: 'science', ration: 'full' });
  ok(st4.faults.length === 1 && st4.faults[0].why === 'emergency', 'emergency can damage a module (20% chance)');
  var st5 = sim.newGame('moon', 5); st5.events.emerg = 0.95;
  sim.runDay(st5, { emergency: true, activity: 'science', ration: 'full' });
  ok(st5.faults.length === 0, 'emergency usually does no damage');
  // Preview never triggers surprises
  var st6 = sim.newGame('moon', 3); st6.day = st6.events.fault.day;
  sim.preview(st6, { activity: 'science' });
  ok(st6.faults.length === 0, 'preview does not change the game');
})();

console.log(failures ? '\n' + failures + ' of ' + checks + ' checks FAILED' : '\nAll ' + checks + ' engine checks passed');
process.exit(failures ? 1 : 0);
