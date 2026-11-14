/* Balance checks, 200 seeds per site. Run: node tests/balance.test.js */
'use strict';
var sim = require('../js/sim.js');
var P = require('./players.js');
var N = 200;
var failures = 0;

function run(site, mk) {
  var r = { evac: 0, win: 0, three: 0 };
  for (var s = 1; s <= N; s++) {
    var st = P.play(site, s, mk(s, site));
    if (st.ending.type === 'success') { r.win++; if (st.ending.stars === 3) { r.three++; } } else { r.evac++; }
  }
  return r;
}
function pct(x) { return (100 * x / N).toFixed(1) + '%'; }
function check(label, cond, detail) {
  console.log('  ' + (cond ? 'PASS' : 'FAIL') + '  ' + label + '  ' + detail);
  if (!cond) { failures++; }
}

['moon', 'mars'].forEach(function (site) {
  console.log('\n' + site.toUpperCase() + ' (' + N + ' seeds)');
  var a = run(site, function () { return P.nothing(); });
  check('Does nothing: evacuated in 100%', a.evac === N, 'evacuated ' + pct(a.evac));
  var b = run(site, function () { return P.naive(); });
  check('Naive: evacuated in at least 90%', b.evac >= 0.9 * N, 'evacuated ' + pct(b.evac));
  var c = run(site, function (s, si) { return P.decent(s, si); });
  check('Decent: completes in 40-90%', c.win >= 0.4 * N && c.win <= 0.9 * N, 'completed ' + pct(c.win));
  var d = run(site, function () { return P.expert(); });
  check('Expert: completes in at least 95%', d.win >= 0.95 * N, 'completed ' + pct(d.win));
  check('Expert: 3 stars in at least 50%', d.three >= 0.5 * N, '3 stars ' + pct(d.three));
});

// Targeted impossibility checks, over many different build plans and seeds.
function randomBuilds(r, allowed) {
  var pool = allowed.slice(), out = [], crates = 14, n = 3 + Math.floor(r() * 5);
  for (var i = 0; i < n; i++) {
    var t = pool[Math.floor(r() * pool.length)];
    out.push(t);
  }
  return out;
}
function searchPlans(site, allowed, expertOpts, label) {
  var r = sim.mulberry32(2026), plans = 40, wins = 0, games = 0;
  for (var i = 0; i < plans; i++) {
    var b = randomBuilds(r, allowed);
    for (var s = 1; s <= 10; s++) {
      var o = Object.assign({ builds: b }, expertOpts);
      var st = P.play(site, s, P.expert(o));
      games++;
      if (st.ending.type === 'success') { wins++; }
    }
  }
  check(label, wins === 0, wins + ' of ' + games + ' games completed');
}
console.log('\nImpossible plans');
searchPlans('moon', ['solar', 'battery', 'drill', 'greenhouse'], {}, 'Moon, no reactor and no shielding: cannot complete');
searchPlans('moon', ['solar', 'battery', 'drill', 'greenhouse', 'shield'], {}, 'Moon, shielding but no reactor (random plans, careful play): cannot complete');
searchPlans('moon', ['reactor', 'solar', 'battery', 'drill', 'greenhouse'], {}, 'Moon, reactor but no shielding: cannot complete');
searchPlans('mars', ['solar', 'battery', 'drill', 'greenhouse', 'shield'], { neverClean: true }, 'Mars, ignores dust (never cleans, no reactor): cannot complete');
(function () {
  var wins = 0;
  for (var s = 1; s <= N; s++) {
    var st = P.play('mars', s, P.naive());
    if (st.ending.type === 'success') { wins++; }
  }
  check('Mars, solar only, never cleans, no storm plan: cannot complete', wins === 0, wins + ' of ' + N + ' games completed');
})();

console.log(failures ? '\n' + failures + ' balance check(s) FAILED' : '\nAll balance checks passed');
process.exit(failures ? 1 : 0);
