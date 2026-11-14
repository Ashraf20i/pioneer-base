/* Pioneer Base - sim.js
 * Pure simulation engine. No DOM. Loadable in Node and in the browser.
 * One call of runDay() = one mission day, resolved in the order of the spec.
 */
(function (root) {
  'use strict';
  var PB = root.PB || (typeof require !== 'undefined' ? require('./data.js') : null);
  var V = PB.val;
  var MAX_DAY = 30;

  var COSTS = {
    solar: ['solar_cost', 'solar_max', 'Solar array'],
    battery: ['battery_cost', 'battery_max', 'Battery'],
    reactor: ['reactor_cost', null, 'Fission reactor'],
    greenhouse: ['gh_cost', 'gh_max', 'Greenhouse'],
    drill: ['drill_cost', 'drill_max', 'Ice drill'],
    shield: ['shield_cost', 'shield_max', 'Shield layer']
  };
  var TYPES = Object.keys(COSTS);

  function mulberry32(a) {
    return function () {
      a |= 0; a = a + 0x6D2B79F5 | 0;
      var t = Math.imul(a ^ a >>> 15, 1 | a);
      t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
      return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
  }
  function clamp(x, lo, hi) { return Math.max(lo, Math.min(hi, x)); }
  function clone(o) { return JSON.parse(JSON.stringify(o)); }

  function typeInfo(type) {
    var c = COSTS[type];
    return { type: type, name: c[2], cost: V(c[0]), max: c[1] ? V(c[1]) : (type === 'reactor' ? 1 : 99) };
  }
  function moduleMax(type) { return typeInfo(type).max; }
  function owned(st, type) {
    var n = st.mod[type] || 0;
    st.pending.forEach(function (p) { if (p.type === type) { n++; } });
    return n;
  }
  function canBuild(st, type) {
    var i = typeInfo(type);
    return st.crates >= i.cost && owned(st, type) < i.max;
  }

  function newGame(site, seed) {
    seed = seed | 0;
    var r = mulberry32(seed * 7919 + (site === 'mars' ? 17 : 3));
    var sc = PB.DATA.scenarios[site];
    var jit = function () { return Math.floor(r() * 3) - 1; };
    var events = { storms: sc.storms.map(function (s) { return { day: s.day + jit(), alert: s.alert, real: s.real }; }), dust: null };
    if (sc.dust) {
      var j = jit();
      events.dust = { start: sc.dust.start + j, end: sc.dust.end + j, alert: sc.dust.alert };
    }
    events.fault = { day: 8 + Math.floor(r() * 6), roll: r() };
    events.emerg = r(); events.emergPick = r();
    return {
      site: site, seed: seed, day: 1, over: false, ending: null,
      o2: V('o2_start'), water: V('water_start'), food: V('food_start'),
      crates: V('crates_start'), battery: V('battery_start'),
      health: V('health_start'), dose: 0, science: 0,
      mod: { solar: site === 'moon' ? 2 : 4, battery: 1, reactor: 0, shield: 0, greenhouse: 0, drill: 0 },
      gh: [], pending: [], dustDays: 0, events: events, faults: [], faultLog: [], emergencyUsed: false,
      history: [], builds: [], first: {}, lost: { o2: 0, hab: 0, water: 0, food: 0, ration: 0 },
      counts: { full: 0, lean: 0, half: 0, science: 0, maintenance: 0, shelter: 0, stormUnsheltered: 0, stormSheltered: 0, cleaned: 0 },
      incidents: []
    };
  }

  function envFor(st, d) {
    var site = st.site, sc = PB.DATA.scenarios[site];
    var night = site === 'moon' && d >= sc.nightStart && d <= sc.nightEnd;
    var dustStorm = !!(st.events.dust && d >= st.events.dust.start && d <= st.events.dust.end);
    var solarStorm = st.events.storms.some(function (s) { return s.day === d; });
    return { night: night, dustStorm: dustStorm, solarStorm: solarStorm };
  }

  function pctStorm() { return Math.round(V('dust_storm_mult') * 100) + '%'; }

  // Alerts the player can see for the planning screen of day d.
  function alertsFor(st, d) {
    var out = [];
    st.events.storms.forEach(function (s) {
      var left = s.day - d;
      if (left >= 0 && left <= s.alert) {
        out.push({ type: 'solar', daysLeft: left, real: s.real,
          text: left === 0 ? 'Solar storm TODAY. Radiation is high. Sheltering cuts the dose to one tenth.' :
            'Solar storm tomorrow (day ' + s.day + '). Plan to shelter.' });
      }
    });
    var du = st.events.dust;
    if (du) {
      var left2 = du.start - d;
      if (left2 >= 0 && left2 <= du.alert) {
        out.push({ type: 'dust', daysLeft: left2, text: left2 === 0 ? 'Dust storm starts today. Solar power drops to ' + pctStorm() + ' for 6 days.' :
          'Dust storm in ' + left2 + ' day' + (left2 > 1 ? 's' : '') + ' (day ' + du.start + '). Solar power will drop to ' + pctStorm() + ' for 6 days.' });
      } else if (d >= du.start && d <= du.end) {
        out.push({ type: 'dust', daysLeft: 0, text: 'Dust storm until day ' + du.end + '. Solar power is only ' + pctStorm() + ' of normal.' });
      }
    }
    var sc = PB.DATA.scenarios[st.site];
    if (st.site === 'moon') {
      if (d === sc.nightStart - 1) { out.push({ type: 'night', daysLeft: 1, text: 'Lunar night begins tomorrow. It lasts 15 days. Arrays make no power.' }); }
      if (d >= sc.nightStart && d <= sc.nightEnd) { out.push({ type: 'night', daysLeft: 0, text: 'Lunar night until day ' + sc.nightEnd + '. Arrays make no power.' }); }
    }
    return out;
  }

  function normalizePlan(st, plan) {
    plan = plan || {};
    var p = {
      oxygen: plan.oxygen !== false && plan.oxygen !== 0,
      lab: !!plan.lab,
      greenhouse: Math.max(0, Math.min(plan.greenhouse | 0, st.mod.greenhouse)),
      drill: Math.max(0, Math.min(plan.drill | 0, st.mod.drill)),
      activity: plan.activity || 'science',
      ration: plan.ration || 'full',
      build: (plan.build || []).slice(),
      repair: !!plan.repair, emergency: !!plan.emergency
    };
    if (plan.oxygen === undefined) { p.oxygen = true; }
    if (plan.lab === undefined) { p.lab = true; }
    if (['science', 'maintenance', 'shelter'].indexOf(p.activity) < 0) { p.activity = 'science'; }
    if (['full', 'lean', 'half'].indexOf(p.ration) < 0) { p.ration = 'full'; }
    return p;
  }

  function rationFactor(r) { return r === 'full' ? 1 : (r === 'lean' ? V('lean_factor') : V('half_factor')); }
  function rationHealth(r) { return r === 'full' ? V('ration_full') : (r === 'lean' ? V('ration_lean') : V('ration_half')); }

  function note(st, key, day) { if (st.first[key] === undefined) { st.first[key] = day; } }

  // Resolve one day. Mutates st. Returns the day record.
  function installed(st) {
    var c = ['oxygen', 'lab'];
    if (st.mod.drill) { c.push('drill'); }
    if (st.mod.greenhouse) { c.push('greenhouse'); }
    return c;
  }
  function fix(st, d) { var f = st.faults.shift(); f.fixed = d; return f.type; }
  function addFault(st, roll, d, why) {
    var c = installed(st), type = c[Math.min(c.length - 1, Math.floor(roll * c.length))];
    var f = { type: type, day: d, mDays: 0, why: why, fixed: null };
    st.faults.push(f); st.faultLog.push(f);
    note(st, 'fault', d);
    return type;
  }

  function runDay(st, planIn, opts) {
    opts = opts || {};
    if (st.over) { return null; }
    var site = st.site, d = st.day;
    var plan = normalizePlan(st, planIn);
    var rec = { day: d, events: [], shut: [], idle: [], builtToday: [], activated: [], harvest: 0, litGh: 0 };

    // 0. pay for builds chosen today (they start working tomorrow)
    plan.build.forEach(function (type) {
      if (TYPES.indexOf(type) >= 0 && canBuild(st, type)) {
        st.crates -= typeInfo(type).cost;
        st.pending.push({ type: type, day: d + 1 });
        st.builds.push({ day: d, type: type });
        rec.builtToday.push(type);
      }
    });

    // 1. modules built yesterday start working
    st.pending = st.pending.filter(function (p) {
      if (p.day <= d) {
        st.mod[p.type] += 1;
        if (p.type === 'greenhouse') { st.gh.push(0); }
        rec.activated.push(p.type);
        return false;
      }
      return true;
    });
    // 1b. emergency fuel-to-water, new faults, repairs (Tier 2)
    if (plan.emergency && !st.emergencyUsed) {
      st.emergencyUsed = true; st.emergencyDay = d;
      st.water = Math.min(V('water_cap'), st.water + V('emerg_water'));
      rec.emergency = true; note(st, 'emergency', d);
      if (!opts.preview && st.events.emerg < V('emerg_risk')) { rec.emergencyDamage = addFault(st, st.events.emergPick, d, 'emergency'); }
    }
    if (!opts.preview && d === st.events.fault.day) { rec.faultNew = addFault(st, st.events.fault.roll, d, 'wear'); }
    if (plan.repair && st.faults.length && st.crates >= V('repair_crates')) {
      st.crates -= V('repair_crates'); rec.repaired = fix(st, d); st.repairs = (st.repairs || 0) + 1;
    }
    var broken = { oxygen: 0, lab: 0, drill: 0, greenhouse: 0 };
    st.faults.forEach(function (f) { broken[f.type]++; });
    rec.broken = broken;
    if (broken.oxygen) { plan.oxygen = false; }
    if (broken.lab) { plan.lab = false; }
    plan.greenhouse = Math.min(plan.greenhouse, Math.max(0, st.mod.greenhouse - broken.greenhouse));
    plan.drill = Math.min(plan.drill, Math.max(0, st.mod.drill - broken.drill));
    plan.greenhouse = Math.min(plan.greenhouse, st.mod.greenhouse);
    plan.drill = Math.min(plan.drill, st.mod.drill);

    // 2. environment
    var env = envFor(st, d);
    var sc = PB.DATA.scenarios[site];
    if (site === 'moon' && d === sc.nightStart) { rec.events.push('The lunar night begins.'); note(st, 'night', d); }
    if (site === 'moon' && d === sc.nightEnd + 1) { rec.events.push('Sunrise! The lunar night is over.'); }
    if (st.events.dust && d === st.events.dust.start) { rec.events.push('A dust storm begins.'); note(st, 'dustStorm', d); }
    if (st.events.dust && d === st.events.dust.end + 1) { rec.events.push('The dust storm is over.'); }
    if (env.solarStorm) { rec.events.push('A solar storm hits.'); }
    if (site === 'mars' && plan.activity === 'maintenance') { st.dustDays = 0; st.counts.cleaned++; rec.cleaned = true; }
    var dustFactor = site === 'mars' ? Math.max(V('dust_floor'), 1 - V('dust_loss') * st.dustDays) : 1;
    var sunFactor = site === 'moon' ? (env.night ? 0 : 1) : 1;
    var stormMult = env.dustStorm ? V('dust_storm_mult') : 1;
    var perArray = site === 'moon' ? V('solar_moon') : V('solar_mars');
    var solar = st.mod.solar * perArray * sunFactor * dustFactor * stormMult;
    var reactor = st.mod.reactor * V('reactor_out');
    var supply = solar + reactor;
    rec.solar = solar; rec.reactor = reactor; rec.supply = supply; rec.dustFactor = dustFactor; rec.env = env;
    if (dustFactor < 0.7 && !st.events.dust) { /* nothing */ }

    // 3. energy: loads in shutdown order
    var o2pPower = site === 'moon' ? V('o2p_power_moon') : V('o2p_power_mars');
    var loads = [];
    if (plan.lab) { loads.push({ id: 'lab', p: V('lab_power') }); }
    for (var i = 0; i < plan.drill; i++) { loads.push({ id: 'drill', p: V('drill_power') }); }
    for (var g = 0; g < plan.greenhouse; g++) { loads.push({ id: 'greenhouse', idx: g, p: V('gh_power') }); }
    if (plan.oxygen) { loads.push({ id: 'oxygen', p: o2pPower }); }
    loads.push({ id: 'habitat', p: V('hab_power') });
    var total = 0; loads.forEach(function (l) { total += l.p; });
    rec.demand = total;
    var avail = supply + st.battery;
    var shortage = total > supply;
    while (total > avail + 1e-9 && loads.length) { var l0 = loads.shift(); total -= l0.p; rec.shut.push(l0.id); }
    var has = function (id) { return loads.filter(function (x) { return x.id === id; }).length; };
    var habOn = has('habitat') > 0;

    // 4. water
    var water = st.water + has('drill') * V('drill_out');
    var crewUse = V('crew_water') * (habOn ? (1 - V('hab_recycle')) : 1);
    var waterEmpty = false;
    if (water >= crewUse) { water -= crewUse; } else { water = 0; waterEmpty = true; }
    var oxOn = has('oxygen') > 0;
    if (oxOn && site === 'moon') {
      if (water >= V('o2p_water_moon')) { water -= V('o2p_water_moon'); } else { oxOn = false; rec.idle.push('oxygen'); rec.noWater = true; note(st, 'plantNoWater', d); }
    }
    var litGh = [];
    loads.forEach(function (l) {
      if (l.id === 'greenhouse') {
        if (water >= V('gh_water')) { water -= V('gh_water'); litGh.push(l.idx); } else { rec.idle.push('greenhouse'); }
      }
    });
    var labOn = has('lab') > 0;
    // energy actually used
    var used = 0;
    if (labOn) { used += V('lab_power'); }
    used += has('drill') * V('drill_power');
    used += litGh.length * V('gh_power');
    if (oxOn) { used += o2pPower; }
    if (habOn) { used += V('hab_power'); }
    var cap = V('battery_per') * st.mod.battery;
    var batteryBefore = st.battery;
    st.battery = clamp(st.battery + supply - used, 0, cap);
    rec.used = used; rec.batteryBefore = batteryBefore;
    water = clamp(water, 0, V('water_cap'));

    // 5. oxygen and food
    var o2prod = (oxOn ? V('o2p_out') : 0) + litGh.length * V('gh_o2');
    var o2need = 4 * V('o2_person');
    var o2 = st.o2 + o2prod;
    var o2Short = false;
    if (o2 >= o2need) { o2 -= o2need; } else { o2 = 0; o2Short = true; }
    o2 = Math.min(o2, V('o2_cap'));
    var food = st.food;
    litGh.forEach(function (idx) {
      st.gh[idx] = (st.gh[idx] || 0) + 1;
      if (st.gh[idx] >= V('gh_cycle')) { st.gh[idx] = 0; food += V('gh_yield'); rec.harvest += V('gh_yield'); }
    });
    food = Math.min(food, V('food_cap'));
    var foodNeed = 4 * V('food_person') * rationFactor(plan.ration);
    var foodEmpty = false;
    if (food >= foodNeed) { food -= foodNeed; } else { food = 0; foodEmpty = true; }
    rec.litGh = litGh.length; rec.foodNeed = foodNeed; rec.o2prod = o2prod; rec.o2need = o2need;
    rec.waterNetPlan = (has('drill') * V('drill_out')) - crewUse - (oxOn && site === 'moon' ? V('o2p_water_moon') : 0) - litGh.length * V('gh_water');

    // 6. radiation
    var base = site === 'moon' ? V('dose_moon') : V('dose_mars');
    var sheltered = plan.activity === 'shelter';
    var storm = env.solarStorm ? (site === 'moon' ? V('storm_moon') : V('storm_mars')) * (sheltered ? V('shelter_factor') : 1) : 0;
    var doseToday = (base + storm) * Math.pow(V('shield_factor'), st.mod.shield);
    rec.doseToday = doseToday; rec.sheltered = sheltered;
    if (env.solarStorm) { if (sheltered) { st.counts.stormSheltered++; } else { st.counts.stormUnsheltered++; } }

    // 7. health and science
    var delta = 0, anyShort = o2Short || !habOn || waterEmpty || foodEmpty;
    if (o2Short) { delta += V('pen_o2'); st.lost.o2 += -V('pen_o2'); note(st, 'o2Short', d); }
    if (!habOn) { delta += V('pen_hab'); st.lost.hab += -V('pen_hab'); note(st, 'habDown', d); }
    if (waterEmpty) { delta += V('pen_water'); st.lost.water += -V('pen_water'); note(st, 'waterEmpty', d); }
    if (foodEmpty) { delta += V('pen_food'); st.lost.food += -V('pen_food'); note(st, 'foodEmpty', d); }
    if (!foodEmpty) {
      var rh = rationHealth(plan.ration);
      if (plan.ration === 'full') { if (!anyShort) { delta += rh; } } else { delta += rh; st.lost.ration += -rh; }
    }
    st.health = clamp(st.health + delta, 0, V('health_start'));
    var sci = 0;
    if (labOn && plan.activity === 'science') { sci = V('lab_science'); st.science += sci; }
    rec.science = sci; rec.healthDelta = delta;

    // commit stocks
    st.o2 = o2; st.water = water; st.food = food; st.dose += doseToday;
    counts(st, plan);

    // incidents and first-time notes
    if (shortage && rec.shut.length) {
      rec.shut.forEach(function (s) { note(st, 'shut_' + s, d); });
      note(st, 'firstShutdown', d);
    }
    if (shortage && st.battery <= 0.0001) { note(st, 'batteryEmpty', d); }
    if (shortage) { note(st, 'deficit', d); }
    rec.o2Short = o2Short; rec.waterEmpty = waterEmpty; rec.foodEmpty = foodEmpty; rec.habOn = habOn; rec.labOn = labOn; rec.oxOn = oxOn;
    rec.activity = plan.activity; rec.ration = plan.ration;

    // history
    st.history.push({
      day: d, supply: supply, used: used, demand: rec.demand, battery: st.battery, cap: cap, o2: st.o2, water: st.water, food: st.food,
      health: st.health, dose: st.dose, science: st.science, events: rec.events.slice(), shut: rec.shut.slice(), activity: plan.activity, ration: plan.ration,
      dust: dustFactor, night: env.night, dustStorm: env.dustStorm, solarStorm: env.solarStorm, harvest: rec.harvest, noWater: !!rec.noWater
    });

    if (plan.activity === 'maintenance' && st.faults.length && !rec.repaired) {
      st.faults[0].mDays++;
      if (st.faults[0].mDays >= V('repair_days')) { rec.repaired = fix(st, d); rec.repairedByCrew = true; st.repairs = (st.repairs || 0) + 1; }
    }

    // 8. end conditions
    if (st.dose > V('dose_limit')) { st.over = true; st.ending = { type: 'evac', reason: 'dose', day: d }; }
    else if (st.health <= 0) { st.over = true; st.ending = { type: 'evac', reason: 'health', day: d }; }
    else if (d >= MAX_DAY) { st.over = true; st.ending = { type: 'success', day: d }; }
    if (!st.over) {
      st.day = d + 1;
      if (site === 'mars') { st.dustDays += 1; }
    }
    if (st.over && st.ending.type === 'evac') { st.ending.stars = 0; }
    if (st.over && st.ending.type === 'success') { st.ending.stars = starsFor(st); }
    return rec;
  }

  function counts(st, plan) {
    st.counts[plan.ration]++;
    st.counts[plan.activity]++;
  }

  function starsFor(st) {
    var stars = 1;
    if (st.science >= V('star2_science')) { stars = 2; }
    var doseMax = st.site === 'moon' ? V('star3_dose_moon') : V('star3_dose_mars');
    if (stars === 2 && st.science >= V('star3_science') && st.health >= V('star3_health') && st.dose <= doseMax) { stars = 3; }
    return stars;
  }

  function runout(stock, net) {
    if (net >= -1e-9) { return null; }
    return Math.max(0, Math.ceil(stock / -net - 1e-9));
  }

  // Preview what today would do, without changing the game.
  function preview(st, planIn) {
    var c = clone(st);
    var plan = normalizePlan(c, planIn);
    var before = { o2: c.o2, water: c.water, food: c.food, battery: c.battery, health: c.health };
    // Do not let the preview spend crates: builds only matter tomorrow.
    var rec = runDay(c, plan, { preview: true });
    var foodAvgGrow = rec.litGh * V('gh_yield') / V('gh_cycle');
    var net = {
      o2: c.o2 - before.o2,
      water: c.water - before.water,
      food: -rec.foodNeed + foodAvgGrow,
      battery: c.battery - before.battery
    };
    // account for a harvest day so that gauges are not misleading
    var o2net = rec.o2prod - rec.o2need;
    var waterNet = rec.waterNetPlan;
    return {
      rec: rec,
      supply: rec.supply, demand: rec.demand, used: rec.used,
      shut: rec.shut, idle: rec.idle, habOn: rec.habOn,
      doseToday: rec.doseToday,
      net: { o2: o2net, water: waterNet, food: net.food, battery: net.battery },
      runout: {
        o2: runout(before.o2, o2net),
        water: runout(before.water, waterNet),
        food: runout(before.food, net.food),
        battery: runout(before.battery, net.battery)
      },
      healthDelta: rec.healthDelta
    };
  }

  function defaultPlan(st) {
    return {
      oxygen: true, lab: true,
      greenhouse: st.mod.greenhouse, drill: st.mod.drill,
      activity: 'science', ration: 'full', build: []
    };
  }

  PB.sim = {
    MAX_DAY: MAX_DAY, TYPES: TYPES, typeInfo: typeInfo, owned: owned, canBuild: canBuild,
    newGame: newGame, runDay: runDay, preview: preview, defaultPlan: defaultPlan,
    envFor: envFor, alertsFor: alertsFor, normalizePlan: normalizePlan, starsFor: starsFor,
    clone: clone, mulberry32: mulberry32, runout: runout
  };
  if (typeof module !== 'undefined' && module.exports) { module.exports = PB.sim; }
})(typeof globalThis !== 'undefined' ? globalThis : window);
