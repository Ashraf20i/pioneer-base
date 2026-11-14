/* Scripted players used by the balance tests. No dependencies. */
'use strict';
var PB = require('../js/data.js');
var sim = require('../js/sim.js');
var V = PB.val;

function stormToday(st) { return sim.envFor(st, st.day).solarStorm; }

// Pick the best set of systems for today with the preview, like a careful player would.
function chooseSystems(st, plan, opt) {
  opt = opt || {};
  var best = null, bestScore = -1e9, hasGh = st.mod.greenhouse, hasDr = st.mod.drill;
  var reserve = opt.reserve || 0;
  for (var ox = 1; ox >= 0; ox--) {
    for (var lab = 1; lab >= 0; lab--) {
      for (var g = hasGh; g >= 0; g--) {
        for (var dr = hasDr; dr >= 0; dr--) {
          var p = Object.assign({}, plan, { oxygen: !!ox, lab: !!lab, greenhouse: g, drill: dr, build: [] });
          var pv = sim.preview(st, p);
          if (!pv.habOn) { continue; }
          if (pv.shut.length || pv.idle.length) { continue; }
          var score = 0;
          if (ox) { score += st.o2 < 30 ? 100 : (st.o2 < 38 ? 40 : 0); }
          score += g * (st.food < 200 ? 22 : 5);
          score += dr * (st.water < 100 ? (st.water < 60 ? 40 : 25) : 3);
          if (lab && plan.activity === 'science') { score += 35; }
          var after = st.battery + pv.net.battery;
          score -= Math.max(0, reserve - after) * 3;
          score += pv.net.battery * 0.01;
          if (score > bestScore) { bestScore = score; best = p; }
        }
      }
    }
  }
  if (!best) { best = Object.assign({}, plan, { oxygen: false, lab: false, greenhouse: 0, drill: 0 }); }
  best.build = plan.build || [];
  return best;
}

function rationChoice(st, buildList) {
  var left = 31 - st.day, harvest = 0;
  var extra = (buildList || []).filter(function (t) { return t === 'greenhouse'; }).length;
  st.pending.forEach(function (p) { if (p.type === 'greenhouse') { extra++; } });
  harvest += extra * Math.floor((left - 1) / V('gh_cycle')) * V('gh_yield');
  for (var i = 0; i < st.gh.length; i++) {
    harvest += Math.floor((left + st.gh[i]) / V('gh_cycle')) * V('gh_yield');
  }
  harvest *= 0.8; // some days will be dark
  var need = function (f) { return 7.2 * f * left; };
  var afford = function (f) { return st.food + harvest >= need(f); };
  if (st.health < 85 && afford(0.75)) { return afford(1) ? 'full' : 'lean'; }
  if (afford(0.75)) { return 'lean'; }
  return 'half';
}

var BUILDS = {
  moon: ['reactor', 'drill', 'drill', 'greenhouse', 'shield'],
  mars: ['reactor', 'greenhouse', 'drill', 'shield', 'solar']
};

function expert(opts) {
  opts = opts || {};
  return function (st) {
    var d = st.day, site = st.site;
    var act = 'science';
    if (stormToday(st)) { act = 'shelter'; }
    else if (site === 'mars' && !opts.neverClean && st.dustDays >= (opts.cleanEvery || 5)) { act = 'maintenance'; }
    var plan = { activity: act, build: [] };
    if (d === (opts.buildDay || 1)) { plan.build = (opts.builds || BUILDS[site]).slice(); }
    plan.ration = opts.ration || rationChoice(st, plan.build);
    return chooseSystems(st, plan, { reserve: site === 'mars' ? 5 : 0 });
  };
}

function nothing() {
  return function () { return { oxygen: false, lab: false, greenhouse: 0, drill: 0, activity: 'shelter', ration: 'full', build: [] }; };
}

// Naive: everything on, only solar arrays, never shelters, never cleans.
function naive() {
  return function (st) {
    var plan = { oxygen: true, lab: true, greenhouse: st.mod.greenhouse, drill: st.mod.drill, activity: 'science', ration: 'full', build: [] };
    if (st.day === 1) { plan.build = ['solar', 'solar', 'solar', 'solar', 'solar', 'solar', 'solar']; }
    return plan;
  };
}

// Decent: sensible but imperfect. A random mix of reasonable plans and human slips.
function decent(seed, site) {
  var r = sim.mulberry32(seed * 31 + 12345);
  var T = site === 'moon' ? [
    { w: 4, b: ['reactor', 'drill', 'drill', 'greenhouse', 'shield'] },
    { w: 3, b: ['reactor', 'drill', 'greenhouse', 'shield', 'shield'] },
    { w: 1, b: ['reactor', 'greenhouse', 'shield', 'shield', 'solar'] },
    { w: 1, b: ['reactor', 'drill', 'drill', 'solar', 'solar'] },
    { w: 1, b: ['solar', 'solar', 'battery', 'battery', 'shield', 'shield', 'drill'] }
  ] : [
    { w: 3, b: ['reactor', 'greenhouse', 'drill', 'shield', 'solar'] },
    { w: 2, b: ['reactor', 'greenhouse', 'greenhouse', 'drill'] },
    { w: 2, b: ['solar', 'solar', 'battery', 'greenhouse', 'drill', 'shield'] },
    { w: 2, b: ['greenhouse', 'drill', 'shield', 'shield', 'solar'] },
    { w: 1, b: ['shield', 'shield', 'shield', 'greenhouse', 'drill'] }
  ];
  var tot = T.reduce(function (a, t) { return a + t.w; }, 0), x = r() * tot, pick = T[0];
  for (var i = 0; i < T.length; i++) { x -= T[i].w; if (x <= 0) { pick = T[i]; break; } }
  var pShelterMiss = 0.2, pCleanMiss = 0.25, pAllOn = 0.12, fullAlways = r() < 0.35;
  return function (st) {
    var d = st.day, act = 'science';
    if (stormToday(st) && r() > pShelterMiss) { act = 'shelter'; }
    else if (st.site === 'mars' && st.dustDays >= 6 && r() > pCleanMiss) { act = 'maintenance'; }
    var plan = { activity: act, build: [] };
    if (d === 1) { plan.build = pick.b.slice(); }
    plan.ration = fullAlways ? 'full' : rationChoice(st, plan.build);
    if (r() < pAllOn) {
      return Object.assign(plan, { oxygen: true, lab: true, greenhouse: st.mod.greenhouse, drill: st.mod.drill });
    }
    return chooseSystems(st, plan, { reserve: st.site === 'mars' ? 5 : 0 });
  };
}

function play(site, seed, player) {
  var st = sim.newGame(site, seed);
  var guard = 0;
  while (!st.over && guard++ < 40) { sim.runDay(st, player(st)); }
  return st;
}

module.exports = { expert: expert, nothing: nothing, naive: naive, decent: decent, play: play, BUILDS: BUILDS, chooseSystems: chooseSystems, rationChoice: rationChoice };
