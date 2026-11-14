/* Pioneer Base - debrief.js
 * Builds the debrief from the real log of one game: result, cause chain,
 * the trade-off behind it, and one thing to try next time.
 * Pure functions, no DOM. Loadable in Node and in the browser.
 */
(function (root) {
  'use strict';
  var PB = root.PB || (typeof require !== 'undefined' ? require('./data.js') : null);
  var V = PB.val;

  var NAMES = { solar: 'solar array', battery: 'battery', reactor: 'fission reactor', greenhouse: 'greenhouse', drill: 'ice drill', shield: 'shield layer' };
  var CATS = { solar: 'power', battery: 'power', reactor: 'power', greenhouse: 'food', drill: 'water', shield: 'radiation shield' };

  function r1(x) { return Math.round(x * 10) / 10; }
  function plural(n, w) {
    if (n === 1) { return n + ' ' + w; }
    return n + ' ' + (/battery$/.test(w) ? w.replace(/y$/, 'ies') : w + 's');
  }

  function spent(st) {
    var by = { power: 0, food: 0, water: 0, 'radiation shield': 0 }, n = {};
    st.builds.forEach(function (b) {
      var cost = PB.sim.typeInfo(b.type).cost;
      by[CATS[b.type]] += cost; n[b.type] = (n[b.type] || 0) + 1;
    });
    return { by: by, n: n, total: by.power + by.food + by.water + by['radiation shield'] };
  }
  function buildText(st) {
    var s = spent(st), parts = [];
    Object.keys(s.n).forEach(function (t) { parts.push(plural(s.n[t], NAMES[t])); });
    return parts.length ? parts.join(', ') : 'nothing';
  }
  function firstBuildDay(st, type) {
    for (var i = 0; i < st.builds.length; i++) { if (st.builds[i].type === type) { return st.builds[i].day; } }
    return null;
  }
  function dayOf(st, key) { return st.first[key]; }
  function hasModule(st, t) { return (st.mod[t] || 0) > 0; }

  function dustAt(st, day) {
    for (var i = 0; i < st.history.length; i++) { if (st.history[i].day === day) { return st.history[i].dust; } }
    return null;
  }

  // Why did power run short? Returns chronological steps.
  function powerSteps(st) {
    var steps = [], dd = dayOf(st, 'deficit'), site = st.site;
    if (dd === undefined) { return steps; }
    var night = dayOf(st, 'night'), dust = dayOf(st, 'dustStorm');
    if (site === 'moon' && night !== undefined && dd >= night) {
      steps.push({ day: night, text: 'The lunar night began. The solar arrays make no power for ' + V('lunar_night') + ' days' + (hasModule(st, 'reactor') ? '.' : ', and you had no reactor.') });
    } else if (site === 'mars' && dust !== undefined && dd >= dust) {
      steps.push({ day: dust, text: 'A dust storm began. The arrays made only ' + Math.round(V('dust_storm_mult') * 100) + '% of their power' + (st.counts.cleaned === 0 ? ', and you had never cleaned the dust off them.' : '.') });
    } else if (site === 'mars') {
      var f = dustAt(st, dd);
      steps.push({ day: dd, text: 'Dust built up on the arrays' + (f !== null ? ' (they made ' + Math.round(f * 100) + '% of their power)' : '') + ', and the base needed more power than they made.' });
    } else {
      steps.push({ day: dd, text: 'The base needed more power than it made (' + st.mod.solar + ' solar arrays).' });
    }
    if (dayOf(st, 'batteryEmpty') !== undefined) { steps.push({ day: dayOf(st, 'batteryEmpty'), text: 'The battery ran empty.' }); }
    return steps;
  }
  function shutText(st, sys, label) {
    var d = dayOf(st, 'shut_' + sys);
    return d === undefined ? null : { day: d, text: label };
  }

  function causeKey(st) {
    if (st.ending.type === 'success') { return 'success'; }
    if (st.ending.reason === 'dose') { return 'dose'; }
    var l = st.lost, food = l.food + l.ration;
    var best = 'o2', bv = l.o2;
    if (l.hab > bv) { best = 'hab'; bv = l.hab; }
    if (l.water > bv) { best = 'water'; bv = l.water; }
    if (food > bv) { best = 'food'; bv = food; }
    if (bv <= 0) { best = 'o2'; }
    return best;
  }

  function chainFor(st, key) {
    var steps = [], end = st.ending, site = st.site, s;
    var endLabel = end.reason === 'dose' ? 'The crew\'s radiation dose passed the ' + V('dose_limit') + ' mSv mission limit, so they were evacuated.' : 'The crew\'s health reached zero, so they were medically evacuated.';
    if (key === 'o2') {
      var pnw = dayOf(st, 'plantNoWater'), o2d = dayOf(st, 'o2Short');
      var shutO = shutText(st, 'oxygen', 'The oxygen plant shut down because there was not enough power.');
      if (pnw !== undefined && (!shutO || pnw <= shutO.day)) {
        if (dayOf(st, 'waterEmpty') !== undefined || st.water < V('o2p_water_moon')) {
          steps.push({ day: 1, text: 'The Moon oxygen plant splits water to make oxygen: ' + V('o2p_water_moon') + ' L of water for each ' + V('o2p_out') + ' kg.' });
        }
        steps.push({ day: pnw, text: 'There was not enough water, so the oxygen plant could not run.' });
      } else {
        powerSteps(st).forEach(function (x) { steps.push(x); });
        if (shutO) { steps.push(shutO); } else if (!powerSteps(st).length) { steps.push({ day: 1, text: 'The oxygen plant made 4 kg a day, but four astronauts breathe 3.4 kg and the plant was often off.' }); }
      }
      if (o2d !== undefined) { steps.push({ day: o2d, text: 'The oxygen tank ran dry. The crew lost ' + (-V('pen_o2')) + ' health each day without air.' }); }
    } else if (key === 'hab') {
      powerSteps(st).forEach(function (x) { steps.push(x); });
      s = shutText(st, 'habitat', 'The habitat core lost power. It stopped recycling water and the crew lost ' + (-V('pen_hab')) + ' health a day.');
      if (s) { steps.push(s); }
    } else if (key === 'water') {
      steps.push({ day: 1, text: 'The crew and the oxygen plant use water every day. You started with ' + V('water_start') + ' L.' + (hasModule(st, 'drill') ? '' : ' You never built an ice drill.') });
      var sd = shutText(st, 'drill', 'The ice drill shut down when power ran short, so no new water came in.');
      if (sd) { steps.push(sd); }
      if (dayOf(st, 'habDown') !== undefined) { steps.push({ day: dayOf(st, 'habDown'), text: 'The habitat lost power, so water recycling stopped and the tank drained faster.' }); }
      if (dayOf(st, 'waterEmpty') !== undefined) { steps.push({ day: dayOf(st, 'waterEmpty'), text: 'The water ran out. The crew lost ' + (-V('pen_water')) + ' health each day.' }); }
    } else if (key === 'food') {
      steps.push({ day: 1, text: 'Four astronauts eat ' + r1(4 * V('food_person')) + ' kg a day. You started with ' + V('food_start') + ' kg.' });
      if (st.counts.lean + st.counts.half > 0) {
        steps.push({ day: 1, text: 'You used ' + plural(st.counts.lean, 'Lean day') + ' and ' + plural(st.counts.half, 'Half day') + '. Smaller rations cost health.' });
      }
      steps.push({ day: 1, text: hasModule(st, 'greenhouse') ? 'Your greenhouse crops grow in 10 days, but they need power and water to keep growing.' : 'You built no greenhouse, so no new food was grown.' });
      if (dayOf(st, 'foodEmpty') !== undefined) { steps.push({ day: dayOf(st, 'foodEmpty'), text: 'The food ran out. The crew lost ' + (-V('pen_food')) + ' health each day.' }); }
    } else if (key === 'dose') {
      var base = site === 'moon' ? V('dose_moon') : V('dose_mars');
      steps.push({ day: 1, text: (site === 'moon' ? 'The Moon has no air to block radiation: ' : 'Mars has a thin air layer, but it blocks little: ') + base + ' mSv every day.' + (st.mod.shield === 0 ? ' You built no shield layers.' : ' You had ' + plural(st.mod.shield, 'shield layer') + '.') });
      if (st.counts.stormUnsheltered > 0) {
        var sdays = st.history.filter(function (h) { return h.solarStorm && h.activity !== 'shelter'; }).map(function (h) { return h.day; });
        steps.push({ day: sdays[0], text: 'A solar storm hit and the crew did not shelter. The dose jumped.' });
      } else if (st.counts.stormSheltered > 0) {
        steps.push({ day: st.history.filter(function (h) { return h.solarStorm; })[0].day, text: 'You sheltered in the storm, but the daily dose kept adding up.' });
      }
    }
    steps.push({ day: end.day, text: endLabel });
    // Make sure there are always at least three steps.
    if (steps.length < 3) {
      steps.unshift({ day: 1, text: 'You started with ' + V('crates_start') + ' crates and built ' + buildText(st) + '.' });
    }
    if (steps.length < 3) {
      steps.splice(steps.length - 1, 0, { day: Math.max(1, end.day - 1), text: 'Health kept falling. Final health: ' + Math.round(st.health) + ' of 100.' });
    }
    return steps;
  }

  function successSteps(st) {
    var steps = [], site = st.site, s = spent(st);
    steps.push({ day: 1, text: 'You spent ' + s.total + ' of your ' + V('crates_start') + ' crates on: ' + buildText(st) + '.' });
    if (site === 'moon') {
      var nd = dayOf(st, 'night') || 11;
      steps.push({ day: nd, text: hasModule(st, 'reactor') ? 'The lunar night began. Your reactor kept the habitat running while the arrays made nothing.' : 'The lunar night began. You got through it on battery power and careful switching.' });
    } else {
      var ds = dayOf(st, 'dustStorm') || 12;
      steps.push({ day: ds, text: 'The dust storm began. ' + (hasModule(st, 'reactor') ? 'Your reactor kept power on while the arrays fell to ' + Math.round(V('dust_storm_mult') * 100) + '%.' : 'You rode it out with careful power use.') + (st.counts.cleaned ? ' You cleaned the arrays on ' + plural(st.counts.cleaned, 'day') + '.' : '') });
    }
    var storms = st.counts.stormSheltered + st.counts.stormUnsheltered;
    steps.push({ day: (st.history.filter(function (h) { return h.solarStorm; })[0] || { day: 7 }).day,
      text: 'Radiation: ' + plural(st.mod.shield, 'shield layer') + ', ' + (storms ? 'sheltered in ' + st.counts.stormSheltered + ' of ' + storms + ' storm(s)' : 'no storms hit') + '. Total dose: ' + r1(st.dose) + ' mSv of ' + V('dose_limit') + '.' });
    steps.push({ day: st.ending.day, text: 'All four astronauts finished the mission. Science: ' + st.science + '. Health: ' + Math.round(st.health) + '.' });
    return steps;
  }

  function tradeoffFor(st, key) {
    var site = st.site;
    if (key === 'success') {
      var parts = [];
      if (hasModule(st, 'reactor')) { parts.push('The reactor used ' + V('reactor_cost') + ' crates, so you could afford ' + (st.mod.greenhouse ? plural(st.mod.greenhouse, 'greenhouse') : 'no greenhouse') + ' and ' + (st.mod.drill ? plural(st.mod.drill, 'ice drill') : 'no ice drill')); }
      else { parts.push('Without a reactor, you put your crates into ' + buildText(st)); }
      if (st.counts.lean + st.counts.half === 0) { parts.push('you could feed the crew Full rations every day'); }
      else { parts.push('you ran Lean rations for ' + plural(st.counts.lean, 'day') + (st.counts.half ? ' and Half rations for ' + plural(st.counts.half, 'day') : '')); }
      parts.push('and had ' + plural(st.counts.science, 'Science day') + ', ' + plural(st.counts.maintenance, 'Maintenance day') + ' and ' + plural(st.counts.shelter, 'Shelter day'));
      return parts.join(', ') + '.';
    }
    var cost;
    if ((key === 'o2' || key === 'hab') && site === 'moon' && !hasModule(st, 'reactor')) {
      cost = 'You put your crates into ' + buildText(st) + ' instead of a power source that works at night. A reactor costs ' + V('reactor_cost') + ' crates.';
    } else if ((key === 'o2' || key === 'hab') && site === 'mars' && !hasModule(st, 'reactor')) {
      cost = 'You put your crates into ' + buildText(st) + ' and none into a reactor. Solar power falls in a dust storm. A reactor does not.';
    } else if (key === 'o2' || key === 'hab') {
      cost = 'You built ' + buildText(st) + ', but the base still ran short of power. Every system you switch on spends power you may need later.';
    } else if (key === 'dose') {
      cost = st.mod.shield === 0 ? 'You built ' + buildText(st) + ' but no shield layers. Shielding costs ' + V('shield_cost') + ' crates a layer and cuts the dose by a quarter each time.' : 'You had ' + plural(st.mod.shield, 'shield layer') + ', but ' + (st.counts.stormUnsheltered ? 'you did not shelter in the storm' : 'the daily dose still added up') + '. Shielding and sheltering work together.';
    } else if (key === 'water') {
      cost = hasModule(st, 'drill') ? 'You had an ice drill, but it could not make water faster than the crew used it.' : 'You spent your crates on ' + buildText(st) + ' and none on an ice drill. Water is used every day.';
    } else {
      cost = hasModule(st, 'greenhouse') ? 'Your greenhouse could not grow food fast enough. You need food or smaller rations, and smaller rations cost health.' : 'You built no greenhouse. Without one the only choice is to cut rations, and that costs health.';
    }
    return cost;
  }

  function tipFor(st, key) {
    var site = st.site;
    if (key === 'success') {
      if (st.ending.stars < 3) {
        if (st.science < V('star3_science')) { return 'Try for three stars: do more Science days while the lab has power (you got ' + st.science + ' of 45 points).'; }
        if (st.health < V('star3_health')) { return 'Try for three stars: keep health above 70. Use Full rations when the food store can afford it.'; }
        return 'Try for three stars: add a shield layer or shelter in every storm to keep the dose under ' + (site === 'moon' ? 35 : 22) + ' mSv.';
      }
      return 'Great mission! Now try the other site. ' + (site === 'moon' ? 'Mars has dust instead of night.' : 'The Moon has a 15-day night.');
    }
    if (key === 'dose') { return st.mod.shield < 2 ? 'Build at least one shield layer on day 1 and press Shelter on every storm day.' : 'Press Shelter on storm days: it cuts the storm dose to one tenth.'; }
    if (site === 'moon' && (key === 'o2' || key === 'hab') && !hasModule(st, 'reactor')) { return 'Build the fission reactor on day 1. It works through the 15-day night.'; }
    if (site === 'mars' && (key === 'o2' || key === 'hab')) { return st.counts.cleaned === 0 ? 'Use a Maintenance day to clean the arrays every few days, and before the dust storm on day 12.' : 'Add a reactor, and keep cleaning the arrays. In the dust storm, switch off the lab and greenhouse to save power.'; }
    if (key === 'o2' || key === 'hab') { return 'Watch the "Energy today" line. When power is short, switch off the lab first and keep the oxygen plant on.'; }
    if (key === 'water') { return 'Build an ice drill early and keep it switched on while you have spare power.'; }
    return 'Build a greenhouse early (crops take 10 days) and use Lean rations at the start, then Full rations when the store is safe.';
  }

  function sortSteps(steps) {
    return steps.map(function (x, i) { return { x: x, i: i }; })
      .sort(function (a, b) { return a.x.day - b.x.day || a.i - b.i; })
      .map(function (o) { return o.x; });
  }

  function analyze(st) {
    var key = causeKey(st), end = st.ending, ok = end.type === 'success';
    var headline;
    if (ok) {
      headline = 'Mission complete! All four astronauts finished 30 days' + (end.stars > 1 ? ' and earned ' + end.stars + ' stars.' : '.');
    } else if (end.reason === 'dose') {
      headline = 'Emergency evacuation on day ' + end.day + '. The crew got too much radiation.';
    } else {
      headline = 'Medical evacuation on day ' + end.day + '. The crew\'s health ran out.';
    }
    return {
      ok: ok, stars: end.stars || 0, cause: key, headline: headline,
      chain: sortSteps(ok ? successSteps(st) : chainFor(st, key)),
      tradeoff: tradeoffFor(st, key),
      tip: tipFor(st, key)
    };
  }

  PB.debrief = { analyze: analyze, spent: spent, buildText: buildText, causeKey: causeKey };
  if (typeof module !== 'undefined' && module.exports) { module.exports = PB.debrief; }
})(typeof globalThis !== 'undefined' ? globalThis : window);
