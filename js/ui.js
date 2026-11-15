/* Pioneer Base - ui.js
 * Draws the game and connects buttons to the simulation. No network requests.
 */
(function () {
  'use strict';
  var PB = window.PB, sim = PB.sim, V = PB.val, DB = PB.debrief;
  var G = null;            // current game
  var prevScreen = 'brief';

  var SYS_NAME = { lab: 'Science lab', drill: 'Ice drill', greenhouse: 'Greenhouse', oxygen: 'Oxygen plant', habitat: 'Habitat core' };
  var MOD_LINES = {
    solar: function (st) { return '+' + (st.site === 'moon' ? V('solar_moon') : V('solar_mars')) + ' ⚡ a day in sunlight'; },
    battery: function () { return '+' + V('battery_per') + ' ⚡ of storage'; },
    reactor: function () { return '+' + V('reactor_out') + ' ⚡ a day, day and night (only 1)'; },
    greenhouse: function () { return V('gh_yield') + ' kg of food every ' + V('gh_cycle') + ' lit days'; },
    drill: function () { return 'Melts ice: +' + V('drill_out') + ' L of water a day'; },
    shield: function () { return 'Lets through ' + Math.round(V('shield_factor') * 100) + '% of radiation per layer'; }
    4: { focus: 'sec-preview', text: function (st) {
      return '<strong>Mission Control:</strong> Our radio messages take ' + (st.site === 'moon' ? 'about 1.3 seconds' : '3 to 22 minutes') + ' to reach you. ' + (st.site === 'moon' ? 'We can help quickly.' : 'In an emergency we cannot answer in time, so you decide alone. Plan ahead.'); } }
  };
  var MOD_COST_ID = { solar: 'solar_cost', battery: 'battery_cost', reactor: 'reactor_cost', greenhouse: 'gh_cost', drill: 'drill_cost', shield: 'shield_cost' };

  function $(id) { return document.getElementById(id); }
  function esc(s) { return String(s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
  function n1(x) { return String(Math.round(x * 10) / 10); }
  function n0(x) { return String(Math.round(x)); }
  function signed(x, d) { var v = d ? n1(x) : n0(x); return (x > 0 ? '+' : (x < 0 ? '−' : '')) + v.replace('-', ''); }
  function plural(n, w) { return n + ' ' + w + (n === 1 ? '' : 's'); }

  function badge(id) {
    var t = PB.tag(id), T = PB.DATA.tags[t];
    return '<button type="button" class="tag tag-' + t.toLowerCase() + '" data-tag-id="' + id + '" aria-label="' + T.word + ' number. Show where it comes from."><span class="tag-icon" aria-hidden="true">' + T.icon + '</span> ' + T.word + '</button>';
  }

  /* ---------------- screens ---------------- */
  function show(name) {
    ['brief', 'game', 'debrief', 'teacher'].forEach(function (s) { $('screen-' + s).hidden = (s !== name); });
    var inGame = name === 'game';
    $('hud').hidden = !inGame;
    $('strip-wrap').hidden = !inGame;
    $('btn-restart').hidden = !(name === 'game' || name === 'debrief');
    if (name !== 'teacher') { prevScreen = name; }
    window.scrollTo(0, 0);
  }

  /* ---------------- game setup ---------------- */
  function startGame(site) {
    var seed = (Date.now() % 100000) + 1;
    G = {
      site: site, st: sim.newGame(site, seed), seed: seed,
      plan: { oxygen: true, lab: true, gh: [], dr: [], activity: 'science', ration: 'full', repair: false, emergency: false },
      cart: {}, reports: [], mythSeen: {}, hintsOff: false, hintDismissedDay: 0
      4: { focus: 'sec-preview', text: function (st) {
      return '<strong>Mission Control:</strong> Our radio messages take ' + (st.site === 'moon' ? 'about 1.3 seconds' : '3 to 22 minutes') + ' to reach you. ' + (st.site === 'moon' ? 'We can help quickly.' : 'In an emergency we cannot answer in time, so you decide alone. Plan ahead.'); } }
  };
    show('game');
    renderAll();
    $('main').focus({ preventScroll: true });
  }

  function syncPlan() {
    var st = G.st, p = G.plan;
    while (p.gh.length < st.mod.greenhouse) { p.gh.push(true); }
    while (p.dr.length < st.mod.drill) { p.dr.push(true); }
  }
  function count(a) { return a.filter(Boolean).length; }
  function curPlan() {
    var p = G.plan, st = G.st, build = [];
    Object.keys(G.cart).forEach(function (t) { for (var i = 0; i < G.cart[t]; i++) { build.push(t); } });
    return {
      oxygen: p.oxygen, lab: p.lab,
      greenhouse: count(p.gh.slice(0, st.mod.greenhouse)), drill: count(p.dr.slice(0, st.mod.drill)),
      activity: p.activity, ration: p.ration, build: build, repair: p.repair, emergency: p.emergency
      4: { focus: 'sec-preview', text: function (st) {
      return '<strong>Mission Control:</strong> Our radio messages take ' + (st.site === 'moon' ? 'about 1.3 seconds' : '3 to 22 minutes') + ' to reach you. ' + (st.site === 'moon' ? 'We can help quickly.' : 'In an emergency we cannot answer in time, so you decide alone. Plan ahead.'); } }
  };
  }
  function cartCost() {
    var c = 0;
    Object.keys(G.cart).forEach(function (t) { c += G.cart[t] * sim.typeInfo(t).cost; });
    return c;
  }

  function renderAll() {
    syncPlan();
    var st = G.st;
    $('hud-site').textContent = st.site === 'moon' ? '☾ Moon outpost' : '♂ Mars outpost';
    $('hud-day').textContent = 'Day ' + st.day + ' of ' + sim.MAX_DAY;
    renderStrip();
    renderControls();
    renderReport();
    refresh();
    renderHint();
  }

  /* ---------------- calendar strip ---------------- */
  function renderStrip() {
    var st = G.st, sc = PB.DATA.scenarios[st.site], html = '';
    for (var d = 1; d <= sim.MAX_DAY; d++) {
      var env = sim.envFor(st, d), cls = ['sd'], icon = '☀', word = 'daylight', ev = '';
      if (env.night) { cls.push('night'); icon = '☾'; word = 'night'; } else { cls.push('day'); }
      st.events.storms.forEach(function (s) {
        if (s.day === d && st.day >= s.day - s.alert) { icon = '☢'; ev = 'solar storm'; cls.push('event'); }
      });
      var du = st.events.dust;
      if (du && d >= du.start && d <= du.end && st.day >= du.start - du.alert) { icon = '≋'; ev = 'dust storm'; cls.push('event'); }
      if (d < st.day) { cls.push('past'); }
      if (d === st.day) { cls.push('today'); }
      html += '<li class="' + cls.join(' ') + '" aria-label="Day ' + d + ', ' + word + (ev ? ', ' + ev : '') + (d === st.day ? ', today' : '') + '"><b>' + d + '</b><i aria-hidden="true">' + icon + '</i></li>';
    }
    $('strip').innerHTML = html;
  }

  /* ---------------- hints ---------------- */
  var HINTS = {
    1: { focus: 'sec-build', text: function (st) {
      return '<strong>Mission Control:</strong> Welcome, Commander! You have ' + st.crates + ' crates. Build new modules in step 4. They start working tomorrow. ' +
        (st.site === 'moon' ? 'Watch the calendar: a 15-day night starts on day 11, and solar arrays make no power in the dark.' : 'Watch the calendar: a dust storm is coming around day 12, and dust also builds up on your solar arrays.'); } },
    2: { focus: 'sec-preview', text: function () {
      return '<strong>Mission Control:</strong> Look at "Energy today". Power made must be at least the power used. If it is not, systems shut down, starting with the science lab.'; } },
    3: { focus: 'sec-ration', text: function () {
      return '<strong>Mission Control:</strong> Check the "runs out in" numbers. If one is small, fix it now: build a drill or greenhouse, or choose smaller rations. Smaller rations cost the crew some health.'; } }
    4: { focus: 'sec-preview', text: function (st) {
      return '<strong>Mission Control:</strong> Our radio messages take ' + (st.site === 'moon' ? 'about 1.3 seconds' : '3 to 22 minutes') + ' to reach you. ' + (st.site === 'moon' ? 'We can help quickly.' : 'In an emergency we cannot answer in time, so you decide alone. Plan ahead.'); } }
  };
  function renderHint() {
    var st = G.st, h = HINTS[st.day], box = $('hint');
    ['sec-build', 'sec-preview', 'sec-ration'].forEach(function (id) { $(id).classList.remove('hint-focus'); });
    if (!h || G.hintsOff || G.hintDismissedDay === st.day) { box.hidden = true; return; }
    box.hidden = false;
    box.innerHTML = '<div class="hint-body">' + h.text(st) + '</div><div><button type="button" class="btn btn-small" id="hint-ok">Got it</button> <button type="button" class="btn btn-small" id="hint-off">Hide all hints</button></div>';
    $(h.focus).classList.add('hint-focus');
  }

  /* ---------------- controls ---------------- */
  function powerBadge(id) { return badge(id); }
  function sw(id, label, checked, disabled) {
    return '<label class="switch"><input type="checkbox" role="switch" data-sw="' + id + '" aria-label="' + esc(label) + '"' + (checked ? ' checked' : '') + (disabled ? ' disabled' : '') + '><span class="track" aria-hidden="true"></span><span class="sw-label">' + (checked ? 'On' : 'Off') + '</span></label>';
  }

  var FNAME = { oxygen: 'Oxygen plant', lab: 'Science lab', drill: 'Ice drill', greenhouse: 'Greenhouse' };
  function brokenCount() {
    var b = { oxygen: 0, lab: 0, drill: 0, greenhouse: 0 };
    G.st.faults.forEach(function (f) { b[f.type]++; });
    return b;
  }
  function brokenRow(name) {
    return '<div class="row"><div class="row-text"><strong>' + name + '</strong><small>⚠ Broken. It is offline until it is repaired (step 5).</small></div><span class="chip chip-broken">⚠ Broken</span></div>';
  }

  function renderControls() {
    var st = G.st, p = G.plan, site = st.site, h = '', brk = brokenCount();
    var o2pPow = site === 'moon' ? 'o2p_power_moon' : 'o2p_power_mars';
    // systems
    h += '<div class="row"><div class="row-text"><strong>Habitat core</strong> <small>Always on. ' + V('hab_power') + ' ⚡ ' + badge('hab_power') + ' It recycles ' + Math.round(V('hab_recycle') * 100) + '% of the crew\'s water ' + badge('hab_recycle') + '</small></div><span class="chip">Always on</span></div>';
    if (brk.oxygen) { h += brokenRow('Oxygen plant'); } else
    h += '<div class="row"><div class="row-text"><strong>Oxygen plant</strong><small>' + V(o2pPow) + ' ⚡ ' + badge(o2pPow) + ' makes ' + V('o2p_out') + ' kg of oxygen. ' + (site === 'moon' ? 'Splits water: needs ' + V('o2p_water_moon') + ' L ' + badge('o2p_water_moon') : 'Takes oxygen from the air (like MOXIE). No water needed.') + '</small></div>' + sw('oxygen', 'Oxygen plant', p.oxygen) + '</div>';
    if (brk.lab) { h += brokenRow('Science lab'); } else
    h += '<div class="row"><div class="row-text"><strong>Science lab</strong><small>' + V('lab_power') + ' ⚡ ' + badge('lab_power') + ' +' + V('lab_science') + ' science when the crew does Science</small></div>' + sw('lab', 'Science lab', p.lab) + '</div>';
    for (var g = 0; g < st.mod.greenhouse; g++) {
      if (g >= st.mod.greenhouse - brk.greenhouse) { h += brokenRow('Greenhouse ' + (g + 1)); continue; }
      var prog = st.gh[g] || 0;
      h += '<div class="row"><div class="row-text"><strong>Greenhouse ' + (g + 1) + '</strong><small>' + V('gh_power') + ' ⚡ ' + badge('gh_power') + ' + ' + V('gh_water') + ' L water. Crop: ' + prog + ' of ' + V('gh_cycle') + ' days ' + badge('gh_cycle') + '</small></div>' + sw('gh' + g, 'Greenhouse ' + (g + 1), p.gh[g]) + '</div>';
    }
    for (var d = 0; d < st.mod.drill; d++) {
      if (d >= st.mod.drill - brk.drill) { h += brokenRow('Ice drill ' + (d + 1)); continue; }
      h += '<div class="row"><div class="row-text"><strong>Ice drill ' + (d + 1) + '</strong><small>' + V('drill_power') + ' ⚡ ' + badge('drill_power') + ' gives +' + V('drill_out') + ' L water ' + badge('drill_out') + '</small></div>' + sw('dr' + d, 'Ice drill ' + (d + 1), p.dr[d]) + '</div>';
    }
    if (!st.mod.greenhouse && !st.mod.drill) { h += '<p class="muted small">No greenhouse or ice drill yet. You can build them in step 4.</p>'; }
    $('systems').innerHTML = h;

    // activity
    var acts = [
      ['science', 'Science', '+' + V('lab_science') + ' points if the lab is on'],
      ['maintenance', 'Maintenance', (site === 'mars' ? 'Cleans the dust off the arrays. ' : '') + 'Three days in a row fix a broken module.'],
      ['shelter', 'Shelter', 'Storm dose ÷10. No science.']
    ];
    $('activity').innerHTML = '<div class="choices" role="radiogroup" aria-label="Crew activity">' + acts.map(function (a) {
      return '<label class="choice"><input type="radio" name="act" value="' + a[0] + '"' + (p.activity === a[0] ? ' checked' : '') + '><span class="box"><b>' + a[1] + '</b><small>' + a[2] + '</small></span></label>';
    }).join('') + '</div>';

    // rations
    var fp = V('food_person') * 4;
    var rats = [
      ['full', 'Full', n1(fp) + ' kg food a day', 'Health +' + V('ration_full') + ' if all needs are met'],
      ['lean', 'Lean (75%)', n1(fp * V('lean_factor')) + ' kg food a day', 'Health ' + signed(V('ration_lean')) + ' a day'],
      ['half', 'Half (50%)', n1(fp * V('half_factor')) + ' kg food a day', 'Health ' + signed(V('ration_half')) + ' a day']
    ];
    $('ration').innerHTML = '<div class="choices" role="radiogroup" aria-label="Rations">' + rats.map(function (a) {
      return '<label class="choice"><input type="radio" name="ration" value="' + a[0] + '"' + (p.ration === a[0] ? ' checked' : '') + '><span class="box"><b>' + a[1] + '</b><small>' + a[2] + ' ' + badge('food_person') + '</small><small>' + a[3] + ' ' + badge(a[0] === 'full' ? 'ration_full' : (a[0] === 'lean' ? 'ration_lean' : 'ration_half')) + '</small></span></label>';
    }).join('') + '</div>';

    renderBuild();
    renderExtra();
  }

  function renderExtra() {
    var st = G.st, p = G.plan, h = '';
    if (st.faults.length) {
      var f = st.faults[0];
      h += '<p><strong>⚠ The ' + FNAME[f.type].toLowerCase() + ' is broken.</strong> Repair progress: ' + f.mDays + ' of ' + V('repair_days') + ' Maintenance days.</p>';
      h += '<div class="row"><div class="row-text"><strong>Repair it now with ' + V('repair_crates') + ' crate</strong><small>' + (st.crates >= V('repair_crates') ? 'You have ' + st.crates + ' crates. The module works again today.' : 'You have no crates. Choose Maintenance for ' + V('repair_days') + ' days instead.') + ' ' + badge('repair_crates') + '</small></div>' + sw('repair', 'Repair the broken module with a crate', p.repair && st.crates >= V('repair_crates'), st.crates < V('repair_crates')) + '</div>';
    } else { h += '<p class="muted">Nothing is broken. A module can break once during the mission.</p>'; }
    h += '<div class="row"><div class="row-text"><strong>Emergency: turn rocket fuel into water</strong><small>' + (st.emergencyUsed ? 'Already used on day ' + st.emergencyDay + '.' : 'One time only. +' + V('emerg_water') + ' L of water. But there is a ' + Math.round(V('emerg_risk') * 100) + '% chance that one module breaks. ') + ' ' + badge('emerg_water') + '</small></div>' + sw('emergency', 'Use the emergency fuel-to-water action today', p.emergency && !st.emergencyUsed, st.emergencyUsed) + '</div>';
    $('extra').innerHTML = h;
  }

  function renderBuild(focusSel) {
    var st = G.st, h = '', left = st.crates - cartCost();
    sim.TYPES.forEach(function (t) {
      var info = sim.typeInfo(t), inCart = G.cart[t] || 0;
      var ownedAll = sim.owned(st, t), have = st.mod[t], building = ownedAll - have;
      var canAdd = left >= info.cost && ownedAll + inCart < info.max;
      var canRemove = inCart > 0;
      var maxText = info.max < 50 ? ' (most ' + info.max + ')' : '';
      h += '<div class="row"><div class="row-text"><strong>' + esc(info.name.charAt(0).toUpperCase() + info.name.slice(1)) + '</strong> <small>' + MOD_LINES[t](st) + maxText + '. Cost: ' + info.cost + ' crates ' + badge(MOD_COST_ID[t]) + '<br>You have ' + have + (building ? ' (+' + building + ' being built)' : '') + '</small></div>' +
        '<div class="stepper" role="group" aria-label="Build ' + esc(info.name) + '"><button type="button" data-build="' + t + '" data-dir="-1" aria-label="Build one fewer ' + esc(info.name) + '"' + (canRemove ? '' : ' disabled') + '>−</button><output aria-live="polite">' + inCart + '</output><button type="button" data-build="' + t + '" data-dir="1" aria-label="Build one more ' + esc(info.name) + '"' + (canAdd ? '' : ' disabled') + '>+</button></div></div>';
    });
    h += '<p class="crates-line">Crates: ' + st.crates + (cartCost() ? ' → ' + left + ' after building' : '') + ' ' + badge('crates_start') + '</p>';
    $('build').innerHTML = h;
    if (focusSel) { var f = $('build').querySelector(focusSel); if (f && !f.disabled) { f.focus(); } else { var any = $('build').querySelector('button:not([disabled])'); if (any) { any.focus(); } } }
  }

  /* ---------------- preview, gauges, outpost ---------------- */
  var lastPv = null;
  function refresh() {
    var st = G.st, pv = sim.preview(st, curPlan());
    lastPv = pv;
    renderAlerts(pv);
    renderPreview(pv);
    renderGauges(pv);
    $('outpost').innerHTML = outpostSVG(st, pv);
    renderChips(pv);
    $('btn-run').textContent = (st.day === sim.MAX_DAY ? 'Run the last day (30)' : 'Run day ' + st.day) + ' ▶';
  }

  function renderAlerts(pv) {
    var st = G.st, out = sim.alertsFor(st, st.day), h = '';
    out.forEach(function (a) {
      var icon = a.type === 'solar' ? '☢' : (a.type === 'dust' ? '≋' : '☾');
      var cls = a.daysLeft === 0 && a.type !== 'night' ? 'danger' : (a.type === 'night' ? 'info' : '');
      var extra = a.type === 'solar' && a.real ? '<small>Based on the real solar storm of ' + esc(a.real) + ' (<a href="' + PB.DATA.links.donki + '" target="_blank" rel="noopener noreferrer">NASA DONKI</a>). Details are in the data panel.</small>' : '';
      var title = a.type === 'solar' ? 'Solar storm alert.' : (a.type === 'dust' ? 'Dust storm alert.' : 'Night alert.');
      h += '<div class="alert ' + cls + '"><span class="alert-icon" aria-hidden="true">' + icon + '</span><div><strong>' + title + '</strong> ' + esc(a.text) + extra + '</div></div>';
    });
    [['o2', 'Oxygen'], ['water', 'Water'], ['food', 'Food']].forEach(function (r) {
      var n = pv.runout[r[0]];
      if (n !== null && n <= 4) { h += '<div class="alert danger"><span class="alert-icon" aria-hidden="true">⚠</span><div><strong>' + r[1] + ' is running low.</strong> At today\'s plan it runs out in ' + plural(n, 'day') + '.</div></div>'; }
    });
    st.faults.forEach(function (f) { h += '<div class="alert danger"><span class="alert-icon" aria-hidden="true">⚠</span><div><strong>Equipment fault:</strong> the ' + FNAME[f.type].toLowerCase() + ' broke on day ' + f.day + '. Repair it in step 5.</div></div>'; });
    if (!pv.habOn) { h += '<div class="alert danger"><span class="alert-icon" aria-hidden="true">⚠</span><div><strong>The habitat core would lose power!</strong> The crew would lose health and water recycling would stop.</div></div>'; }
    $('alerts').innerHTML = h;
  }

  function renderPreview(pv) {
    var st = G.st, rec = pv.rec, h = '';
    var capNow = V('battery_per') * st.mod.battery;
    var cls = pv.shut.length ? 'pv-bad' : (pv.supply < pv.demand ? 'pv-warn' : 'pv-ok');
    var icon = pv.shut.length ? '⚠' : (pv.supply < pv.demand ? '!' : '✔');
    h += '<p class="pv-line ' + cls + '"><strong>' + icon + ' Energy today:</strong> made ' + n1(pv.supply) + ' ⚡ (solar ' + n1(rec.solar) + ' + reactor ' + n1(rec.reactor) + '), needed ' + n1(pv.demand) + ' ⚡. Battery ' + n1(st.battery) + ' → ' + n1(st.battery + pv.net.battery) + ' of ' + capNow + ' ⚡.</p>';
    h += '<ul class="pv-list">';
    var seen = {};
    pv.shut.forEach(function (s) { seen[s] = (seen[s] || 0) + 1; });
    Object.keys(seen).forEach(function (s) {
      h += '<li class="pv-bad"><span aria-hidden="true">⚠</span> ' + SYS_NAME[s] + (seen[s] > 1 ? ' (×' + seen[s] + ')' : '') + ' would shut down: not enough power.</li>';
    });
    if (pv.idle.indexOf('oxygen') >= 0) { h += '<li class="pv-bad"><span aria-hidden="true">⚠</span> The oxygen plant could not run: not enough water.</li>'; }
    if (pv.idle.indexOf('greenhouse') >= 0) { h += '<li class="pv-warn"><span aria-hidden="true">!</span> A greenhouse would pause: not enough water.</li>'; }
    if (!pv.shut.length && !pv.idle.length) { h += '<li class="pv-ok"><span aria-hidden="true">✔</span> Everything you switched on can run.</li>'; }
    [['o2', 'Oxygen', 'kg'], ['water', 'Water', 'L'], ['food', 'Food', 'kg']].forEach(function (r) {
      var n = pv.runout[r[0]], rate = pv.net[r[0]];
      if (n === null) { h += '<li class="pv-ok"><span aria-hidden="true">✔</span> ' + r[1] + ': steady (' + signed(rate, 1) + ' ' + r[2] + ' a day).</li>'; }
      else { h += '<li class="' + (n <= 4 ? 'pv-bad' : (n <= 10 ? 'pv-warn' : '')) + '"><span aria-hidden="true">' + (n <= 4 ? '⚠' : (n <= 10 ? '!' : '•')) + '</span> ' + r[1] + ' runs out in ' + plural(n, 'day') + ' at this rate (' + signed(rate, 1) + ' ' + r[2] + ' a day).</li>'; }
    });
    h += '</ul>';
    h += '<p class="small muted">The "runs out" numbers use today\'s rate. Night, storms and new modules can change them. Greenhouse food is counted as an average.</p>';
    $('preview').innerHTML = h;
  }

  function gauge(o) {
    var pct = Math.max(0, Math.min(100, o.pct));
    var word = o.state === 'danger' ? 'Danger' : (o.state === 'warn' ? 'Watch' : 'OK');
    var icon = o.state === 'danger' ? '⚠ ' : (o.state === 'warn' ? '! ' : '✔ ');
    return '<div class="g ' + o.state + '"><div class="g-head"><span><span aria-hidden="true">' + o.icon + '</span> ' + o.name + '</span><span class="g-state">' + icon + word + '</span></div>' +
      '<div class="g-main">' + o.main + ' ' + o.mainBadge + '</div>' +
      '<div class="bar" role="img" aria-label="' + o.name + ' ' + n0(pct) + ' percent"><span style="width:' + pct + '%"></span></div>' +
      '<div class="g-rate">' + o.rate + ' ' + (o.rateBadge || '') + '</div></div>';
  }
  function stockState(stock, rn, pct) {
    if (rn !== null && rn <= 3) { return 'danger'; }
    if (pct < 12) { return 'danger'; }
    if (rn !== null && rn <= 8) { return 'warn'; }
    if (pct < 25) { return 'warn'; }
    return 'ok';
  }

  function renderGauges(pv) {
    var st = G.st, site = st.site, rec = pv.rec, h = '';
    var cap = V('battery_per') * st.mod.battery;
    // Energy
    var estate = pv.shut.length ? 'danger' : (pv.supply < pv.demand ? 'warn' : 'ok');
    h += gauge({ icon: '⚡', name: 'Energy today', state: estate, pct: pv.demand ? pv.supply / pv.demand * 100 : 100,
      main: n1(pv.supply) + ' <small>made / ' + n1(pv.demand) + ' needed ⚡</small>', mainBadge: badge(site === 'moon' ? 'solar_moon' : 'solar_mars'),
      rate: pv.shut.length ? 'Short! ' + pv.shut.length + ' system(s) would shut down.' : (pv.supply < pv.demand ? 'Short, but the battery covers it today.' : 'Enough power. Spare: ' + n1(pv.supply - pv.used) + ' ⚡'), rateBadge: badge('hab_power') });
    h += gauge({ icon: '▮', name: 'Battery', state: stockState(st.battery, pv.runout.battery, st.battery / cap * 100), pct: cap ? st.battery / cap * 100 : 0,
      main: n1(st.battery) + ' <small>/ ' + cap + ' ⚡</small>', mainBadge: badge('battery_per'),
      rate: pv.runout.battery !== null ? signed(pv.net.battery, 1) + ' ⚡ today. Empty in ' + plural(pv.runout.battery, 'day') + '.' : signed(pv.net.battery, 1) + ' ⚡ today. Steady or charging.', rateBadge: badge('battery_start') });
    function res(key, name, icon, unit, capId, rateId, rateText) {
      var rn = pv.runout[key], stock = st[key], pct = stock / V(capId) * 100;
      h += gauge({ icon: icon, name: name, state: stockState(stock, rn, pct), pct: pct,
        main: n1(stock) + ' <small>/ ' + V(capId) + ' ' + unit + '</small>', mainBadge: badge(capId),
        rate: rateText + ' ' + (rn !== null ? 'Runs out in ' + plural(rn, 'day') + '.' : 'Steady.'), rateBadge: badge(rateId) });
    }
    res('o2', 'Oxygen', '○', 'kg', 'o2_cap', 'o2_person', 'Crew breathe ' + n1(4 * V('o2_person')) + ' kg a day. Net ' + signed(pv.net.o2, 1) + '.');
    res('water', 'Water', '◍', 'L', 'water_cap', 'crew_water', 'Net ' + signed(pv.net.water, 1) + ' L a day.');
    res('food', 'Food', '▤', 'kg', 'food_cap', 'food_person', 'Net ' + signed(pv.net.food, 1) + ' kg a day.');
    // Dose
    var dtoday = pv.doseToday, limit = V('dose_limit'), left = limit - st.dose;
    var daysLeft = dtoday > 0 ? Math.floor(left / dtoday) : 99;
    var dstate = (daysLeft < sim.MAX_DAY - st.day + 1) ? (daysLeft < 5 ? 'danger' : 'warn') : 'ok';
    if (st.dose / limit > 0.9) { dstate = 'danger'; }
    h += gauge({ icon: '☢', name: 'Radiation dose', state: dstate, pct: st.dose / limit * 100,
      main: n1(st.dose) + ' <small>/ ' + limit + ' mSv</small>', mainBadge: badge('dose_limit'),
      rate: '+' + n1(dtoday) + ' mSv today' + (daysLeft < sim.MAX_DAY - st.day + 1 ? '. At this rate the limit is passed in ' + plural(daysLeft + 1, 'day') + '.' : '.') + ' NASA\'s career limit is ' + V('career_limit') + ' mSv.', rateBadge: badge(site === 'moon' ? 'dose_moon' : 'dose_mars') });
    var hd = pv.healthDelta;
    h += gauge({ icon: '♥', name: 'Crew health', state: st.health < 30 || hd <= -15 ? 'danger' : (st.health < 60 || hd < 0 ? 'warn' : 'ok'), pct: st.health,
      main: n0(st.health) + ' <small>/ 100</small>', mainBadge: badge('health_start'),
      rate: 'Today: ' + (hd > 0 ? '+' : (hd < 0 ? '−' : '')) + Math.abs(hd) + ' health.', rateBadge: badge('pen_o2') });
    h += gauge({ icon: '✦', name: 'Science', state: 'ok', pct: st.science / V('star3_science') * 100,
      main: n0(st.science) + ' <small>points</small>', mainBadge: badge('lab_science'),
      rate: 'Today: +' + rec.science + '. Goals: ' + V('star2_science') + ' for ★★, ' + V('star3_science') + ' for ★★★.', rateBadge: badge('star2_science') });
    $('gauges').innerHTML = h;
  }

  function renderChips(pv) {
    var st = G.st, rec = pv.rec, h = '';
    function chip(on, text) { return '<li class="sc ' + (on ? 'on' : 'off') + '"><span aria-hidden="true">' + (on ? '●' : '○') + '</span> ' + text + ': ' + (on ? 'on' : 'off') + '</li>'; }
    h += chip(rec.habOn, 'Habitat core');
    h += chip(rec.oxOn, 'Oxygen plant');
    h += chip(rec.labOn, 'Science lab');
    h += '<li class="sc ' + (rec.solar > 0 ? 'on' : 'off') + '"><span aria-hidden="true">' + (rec.solar > 0 ? '●' : '○') + '</span> Solar arrays ×' + st.mod.solar + ': ' + n1(rec.solar) + ' ⚡' + (st.site === 'mars' ? ' (' + Math.round(rec.dustFactor * 100) + '% clean' + (rec.env.dustStorm ? ', dust storm' : '') + ')' : (rec.env.night ? ' (night)' : '')) + '</li>';
    if (st.mod.reactor) { h += '<li class="sc on"><span aria-hidden="true">●</span> Reactor: ' + V('reactor_out') + ' ⚡</li>'; }
    if (st.mod.greenhouse) { h += '<li class="sc ' + (rec.litGh ? 'on' : 'off') + '"><span aria-hidden="true">' + (rec.litGh ? '●' : '○') + '</span> Greenhouses lit: ' + rec.litGh + ' of ' + st.mod.greenhouse + '</li>'; }
    if (st.mod.drill) {
      var dr = curPlan().drill - pv.shut.filter(function (s) { return s === 'drill'; }).length;
      h += '<li class="sc ' + (dr ? 'on' : 'off') + '"><span aria-hidden="true">' + (dr ? '●' : '○') + '</span> Ice drills running: ' + dr + ' of ' + st.mod.drill + '</li>';
    }
    if (st.mod.shield) { h += '<li class="sc on"><span aria-hidden="true">●</span> Shield layers: ' + st.mod.shield + '</li>'; }
    st.pending.forEach(function (p) { h += '<li class="sc build"><span aria-hidden="true">◌</span> Building: ' + sim.typeInfo(p.type).name + ' (ready day ' + p.day + ')</li>'; });
    $('status-chips').innerHTML = h;
  }

  /* ---------------- outpost drawing (inline SVG) ---------------- */
  function outpostSVG(st, pv) {
    var rec = pv.rec, env = rec.env, site = st.site, night = env.night, dustS = env.dustStorm;
    var gy = 222, fy = 284, plan = curPlan();
    var sky = site === 'moon' ? (night ? '#03050d' : '#0a1230') : (dustS ? '#7b5238' : '#c98a64');
    var ground = site === 'moon' ? '#858a98' : '#97482f', ground2 = site === 'moon' ? '#6d7280' : '#7e3a26';
    var s = '<svg viewBox="0 0 700 300" role="img" aria-label="Picture of your outpost: ' + (rec.habOn ? 'habitat powered' : 'habitat dark') + ', ' + st.mod.solar + ' solar arrays, ' + st.mod.shield + ' shield layers">';
    s += '<defs><radialGradient id="glow"><stop offset="0" stop-color="#ffe9a0" stop-opacity=".9"/><stop offset="1" stop-color="#ffe9a0" stop-opacity="0"/></radialGradient>' +
      '<linearGradient id="dome" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#e8eefc"/><stop offset="1" stop-color="#9fb0d6"/></linearGradient></defs>';
    s += '<rect width="700" height="300" fill="' + sky + '"/>';
    if (site === 'moon' || night) {
      for (var i = 0; i < 36; i++) { s += '<circle cx="' + ((i * 97 + 31) % 690 + 5) + '" cy="' + ((i * 53 + 17) % 150 + 6) + '" r="' + (i % 5 === 0 ? 1.6 : 1) + '" fill="#fff" opacity="' + (night ? .9 : .55) + '"/>'; }
    }
    if (!night && !dustS) { s += '<circle cx="620" cy="52" r="46" fill="url(#glow)"/><circle cx="620" cy="52" r="20" fill="#ffe27a"/>'; }
    if (site === 'moon' && !night) { s += '<circle cx="90" cy="60" r="16" fill="#4d7fd6"/><circle cx="85" cy="57" r="7" fill="#5fb878" opacity=".8"/>'; }
    if (night) { s += '<path d="M600 40 a22 22 0 1 0 22 28 a18 18 0 1 1 -22 -28z" fill="#e8ecff" opacity=".85"/>'; }
    s += '<rect y="' + gy + '" width="700" height="80" fill="' + ground + '"/><rect y="' + (gy + 40) + '" width="700" height="40" fill="' + ground2 + '"/>';
    for (var r = 0; r < 7; r++) { s += '<ellipse cx="' + (60 + r * 105) + '" cy="' + (gy + 14 + (r % 3) * 12) + '" rx="' + (16 + (r % 4) * 4) + '" ry="4" fill="' + ground2 + '" opacity=".7"/>'; }

    // shield layers
    for (var k = 1; k <= st.mod.shield; k++) {
      var rr = 78 + k * 11;
      s += '<path d="M' + (350 - rr) + ',' + gy + ' A' + rr + ',' + rr + ' 0 0 1 ' + (350 + rr) + ',' + gy + '" fill="none" stroke="#62d6ff" stroke-width="3" stroke-dasharray="9 5" opacity=".85"/>';
    }
    // habitat
    var hab = rec.habOn;
    s += '<path d="M278,' + gy + ' A72,72 0 0 1 422,' + gy + ' Z" fill="url(#dome)" stroke="#6c7aa6" stroke-width="3"/>';
    s += '<rect x="318" y="' + (gy - 38) + '" width="64" height="38" rx="4" fill="#5c6a94"/>';
    [[310, 150], [350, 138], [390, 150]].forEach(function (w, wi) { s += '<circle cx="' + w[0] + '" cy="' + (gy - 34 - (wi === 1 ? 14 : 0) + 8) + '" r="9" fill="' + (hab ? '#ffe27a' : '#2a3254') + '" ' + (hab ? 'class="lit"' : '') + ' stroke="#33406e"/>'; });
    s += '<rect x="340" y="' + (gy - 26) + '" width="20" height="26" rx="3" fill="' + (hab ? '#ffe27a' : '#222a4a') + '"/>';
    if (!hab) { s += '<text x="350" y="' + (gy - 70) + '" text-anchor="middle" font-size="16" fill="#ffd0d6" font-weight="700">NO POWER</text>'; }

    // solar arrays
    var sunLit = rec.solar > 0;
    for (var a = 0; a < sim_max('solar'); a++) {
      var x = 22 + a * 40;
      if (a < st.mod.solar) {
        s += '<line x1="' + (x + 15) + '" y1="' + (gy - 14) + '" x2="' + (x + 15) + '" y2="' + gy + '" stroke="#9aa3c4" stroke-width="3"/>';
        s += '<polygon points="' + x + ',' + (gy - 14) + ' ' + (x + 32) + ',' + (gy - 14) + ' ' + (x + 28) + ',' + (gy - 50) + ' ' + (x - 4) + ',' + (gy - 50) + '" fill="' + (sunLit ? '#ffd24a' : '#454d78') + '" stroke="#eef" stroke-width="1.5" ' + (sunLit ? 'class="lit"' : '') + '/>';
        s += '<line x1="' + (x + 14) + '" y1="' + (gy - 14) + '" x2="' + (x + 12) + '" y2="' + (gy - 50) + '" stroke="#fff" stroke-opacity=".4"/><line x1="' + (x - 2) + '" y1="' + (gy - 32) + '" x2="' + (x + 30) + '" y2="' + (gy - 32) + '" stroke="#fff" stroke-opacity=".4"/>';
        if (site === 'mars' && rec.dustFactor < 1) { s += '<polygon points="' + x + ',' + (gy - 14) + ' ' + (x + 32) + ',' + (gy - 14) + ' ' + (x + 28) + ',' + (gy - 50) + ' ' + (x - 4) + ',' + (gy - 50) + '" fill="#b98a5e" opacity="' + (1 - rec.dustFactor).toFixed(2) + '"/>'; }
      } else if (a < sim_owned('solar')) {
        s += ghost(x, gy - 50, 32, 36);
      }
    }
    // reactor
    var rx = 450;
    if (st.mod.reactor) {
      s += '<rect x="' + rx + '" y="' + (gy - 50) + '" width="46" height="50" rx="6" fill="#6b7596" stroke="#cfd6f0" stroke-width="2"/><circle cx="' + (rx + 23) + '" cy="' + (gy - 28) + '" r="12" fill="' + '#7dffb0' + '" class="lit"/><circle cx="' + (rx + 23) + '" cy="' + (gy - 28) + '" r="5" fill="#0a3a1d"/>';
    } else if (sim_owned('reactor')) { s += ghost(rx, gy - 50, 46, 50); }
    // oxygen plant
    var ox = 512, oxOn = rec.oxOn;
    s += '<rect x="' + ox + '" y="' + (gy - 56) + '" width="28" height="56" rx="8" fill="' + (oxOn ? '#62d6ff' : '#3a4670') + '" stroke="#cfd6f0" stroke-width="2" ' + (oxOn ? 'class="lit"' : '') + '/><text x="' + (ox + 14) + '" y="' + (gy - 22) + '" text-anchor="middle" font-size="15" font-weight="800" fill="' + (oxOn ? '#04222d' : '#9aa6d6') + '">O₂</text>';
    s += '<line x1="' + (ox) + '" y1="' + (gy - 12) + '" x2="422" y2="' + (gy - 12) + '" stroke="#9aa3c4" stroke-width="4"/>';
    // lab
    var lx = 562, labOn = rec.labOn;
    s += '<rect x="' + lx + '" y="' + (gy - 38) + '" width="58" height="38" rx="5" fill="#8793b8" stroke="#cfd6f0" stroke-width="2"/><rect x="' + (lx + 8) + '" y="' + (gy - 30) + '" width="22" height="14" rx="2" fill="' + (labOn ? '#ffe27a' : '#2a3254') + '" ' + (labOn ? 'class="lit"' : '') + '/><line x1="' + (lx + 44) + '" y1="' + (gy - 38) + '" x2="' + (lx + 44) + '" y2="' + (gy - 58) + '" stroke="#cfd6f0" stroke-width="3"/><circle cx="' + (lx + 44) + '" cy="' + (gy - 60) + '" r="5" fill="' + (labOn ? '#ffe27a' : '#5a6490') + '"/>';
    // front row: batteries
    for (var b = 0; b < 4; b++) {
      var bx = 24 + b * 30;
      if (b < st.mod.battery) {
        var cap = V('battery_per') * st.mod.battery, lvl = cap ? st.battery / cap : 0;
        s += '<rect x="' + bx + '" y="' + (fy - 30) + '" width="22" height="30" rx="4" fill="#202849" stroke="#cfd6f0" stroke-width="2"/><rect x="' + (bx + 3) + '" y="' + (fy - 3 - 24 * lvl) + '" width="16" height="' + (24 * lvl) + '" fill="' + (lvl > .25 ? '#5ee08a' : '#ff7a8a') + '"/>';
      } else if (b < sim_owned('battery')) { s += ghost(bx, fy - 30, 22, 30); }
    }
    // greenhouses
    for (var g = 0; g < 3; g++) {
      var gx = 200 + g * 64, lit = g < st.mod.greenhouse && g < plan.greenhouse && rec.litGh > g;
      if (g < st.mod.greenhouse) {
        s += '<path d="M' + (gx - 26) + ',' + fy + ' A26,26 0 0 1 ' + (gx + 26) + ',' + fy + ' Z" fill="' + (lit ? '#4ade80' : '#2c4a3a') + '" fill-opacity=".85" stroke="#cfd6f0" stroke-width="2" ' + (lit ? 'class="lit"' : '') + '/><path d="M' + gx + ',' + fy + ' l0,-14 m-8,14 l0,-9 m16,9 l0,-9" stroke="#0e5a2a" stroke-width="3" stroke-linecap="round"/>';
      } else if (g < sim_owned('greenhouse')) { s += ghost(gx - 26, fy - 26, 52, 26); }
    }
    // drills
    var drOn = plan.drill - pv.shut.filter(function (q) { return q === 'drill'; }).length;
    for (var d = 0; d < 3; d++) {
      var dx = 440 + d * 52;
      if (d < st.mod.drill) {
        var on = d < drOn;
        s += '<polygon points="' + dx + ',' + fy + ' ' + (dx + 36) + ',' + fy + ' ' + (dx + 18) + ',' + (fy - 44) + '" fill="none" stroke="' + (on ? '#ffe27a' : '#7a84ac') + '" stroke-width="3" ' + (on ? 'class="lit"' : '') + '/><line x1="' + (dx + 18) + '" y1="' + (fy - 44) + '" x2="' + (dx + 18) + '" y2="' + fy + '" stroke="' + (on ? '#ffe27a' : '#7a84ac') + '" stroke-width="3"/><circle cx="' + (dx + 18) + '" cy="' + (fy - 46) + '" r="4" fill="' + (on ? '#ffe27a' : '#7a84ac') + '"/>';
      } else if (d < sim_owned('drill')) { s += ghost(dx, fy - 44, 36, 44); }
    }
    // dust haze
    if (site === 'mars' && dustS) { s += '<rect width="700" height="300" fill="#a8703f" opacity=".35"/>'; }
    s += '</svg>';
    return s;
    function ghost(x, y, w, h) { return '<rect x="' + x + '" y="' + y + '" width="' + w + '" height="' + h + '" rx="4" fill="none" stroke="#62d6ff" stroke-width="2" stroke-dasharray="5 4"/>'; }
    function sim_owned(t) { return sim.owned(st, t); }
    function sim_max(t) { return Math.max(st.mod[t], sim_owned(t)); }
  }

  /* ---------------- run a day ---------------- */
  function runToday() {
    var st = G.st, plan = curPlan();
    var rec = sim.runDay(st, plan);
    G.reports.push(rec);
    G.cart = {};
    G.plan.repair = false; G.plan.emergency = false;
    if (st.over) { showDebrief(); return; }
    var card = PB.DATA.cards.filter(function (c) { return c.afterDay === rec.day && !G.mythSeen[c.id]; })[0];
    renderAll();
    var rep = $('sec-report');
    rep.scrollIntoView({ block: 'start', behavior: 'auto' });
    rep.focus({ preventScroll: true });
    if (card) { G.mythSeen[card.id] = true; showMyth(card); }
  }

  function renderReport() {
    var rec = G.reports[G.reports.length - 1], box = $('report');
    if (!rec) { box.innerHTML = '<p class="muted">Your first report will show up here after day 1.</p>'; return; }
    var st = G.st, h = '<h3>Day ' + rec.day + ' report</h3><ul>';
    rec.events.forEach(function (e) { h += '<li><strong>' + esc(e) + '</strong></li>'; });
    h += '<li>Power: made ' + n1(rec.supply) + ' ⚡, used ' + n1(rec.used) + ' ⚡. Battery now ' + n1(st.battery) + ' ⚡.</li>';
    if (rec.shut.length) {
      var seen = {}; rec.shut.forEach(function (x) { seen[x] = (seen[x] || 0) + 1; });
      h += '<li>⚠ Not enough power. Shut down: ' + Object.keys(seen).map(function (x) { return SYS_NAME[x] + (seen[x] > 1 ? ' ×' + seen[x] : ''); }).join(', ') + '.</li>';
    }
    if (rec.noWater) { h += '<li>⚠ The oxygen plant could not run: no water.</li>'; }
    if (rec.o2Short) { h += '<li>⚠ The oxygen ran out! The crew lost health.</li>'; }
    if (rec.waterEmpty) { h += '<li>⚠ The water ran out! The crew lost health.</li>'; }
    if (rec.foodEmpty) { h += '<li>⚠ The food ran out! The crew lost health.</li>'; }
    if (!rec.habOn) { h += '<li>⚠ The habitat core had no power. Water was not recycled.</li>'; }
    if (rec.faultNew) { h += '<li>⚠ <strong>Equipment fault:</strong> the ' + FNAME[rec.faultNew].toLowerCase() + ' broke!</li>'; }
    if (rec.emergency) { h += '<li>The crew turned rocket fuel into water: +' + V('emerg_water') + ' L.' + (rec.emergencyDamage ? ' ⚠ Accident! The ' + FNAME[rec.emergencyDamage].toLowerCase() + ' broke.' : ' No accident.') + '</li>'; }
    if (rec.repaired) { h += '<li>✔ The ' + FNAME[rec.repaired].toLowerCase() + ' was repaired.</li>'; }
    if (rec.harvest) { h += '<li>🌱 Harvest: +' + rec.harvest + ' kg of food.</li>'; }
    if (rec.cleaned) { h += '<li>The crew cleaned the dust off the arrays.</li>'; }
    if (rec.activated.length) { h += '<li>Now working: ' + rec.activated.map(function (t) { return sim.typeInfo(t).name; }).join(', ') + '.</li>'; }
    if (rec.builtToday.length) { h += '<li>Started building: ' + rec.builtToday.map(function (t) { return sim.typeInfo(t).name; }).join(', ') + ' (ready tomorrow).</li>'; }
    h += '<li>Crew health ' + (rec.healthDelta >= 0 ? '+' : '−') + Math.abs(rec.healthDelta) + ' (now ' + n0(st.health) + '). Science ' + (rec.science ? '+' + rec.science : '+0') + ' (now ' + st.science + '). Radiation +' + n1(rec.doseToday) + ' mSv.</li>';
    h += '</ul>';
    box.innerHTML = h;
  }

  /* ---------------- Myth or Reality ---------------- */
  function showMyth(c) {
    var d = $('dlg-myth'), b = $('myth-body');
    b.innerHTML = '<h2 id="myth-title">Myth or Reality?</h2><p class="myth-q">' + esc(c.text) + '</p><div class="myth-btns" id="myth-btns"><button type="button" class="btn btn-primary" data-myth="myth">Myth</button><button type="button" class="btn btn-primary" data-myth="reality">Reality</button></div><div id="myth-out" aria-live="polite"></div>';
    d.setAttribute('aria-labelledby', 'myth-title');
    d._card = c;
    d.showModal();
  }
  function answerMyth(choice) {
    var d = $('dlg-myth'), c = d._card, right = (choice === 'myth') === c.myth;
    $('myth-btns').hidden = true;
    $('myth-out').innerHTML = '<div class="myth-ans"><p><strong>' + (right ? '✔ You got it!' : '✖ Not quite.') + '</strong> ' + esc(c.why) + '</p><p class="small">Source: <a href="' + c.url + '" target="_blank" rel="noopener noreferrer">' + esc(c.source) + '</a></p></div><button class="btn btn-primary" value="ok">Back to the mission</button>';
    $('myth-out').querySelector('button').focus();
  }

  /* ---------------- debrief ---------------- */
  var SERIES = [
    { k: 'battery', name: 'Energy (battery)', col: '#ffc94a', dash: '' },
    { k: 'o2', name: 'Oxygen', col: '#62d6ff', dash: '8 4' },
    { k: 'water', name: 'Water', col: '#7d9bff', dash: '2 4' },
    { k: 'food', name: 'Food', col: '#5ee08a', dash: '12 4 2 4' },
    { k: 'health', name: 'Health', col: '#ff8fb1', dash: '' },
    { k: 'dose', name: 'Radiation dose (limit = 100%)', col: '#d49bff', dash: '5 3' }
  ];
  function pctOf(h, k) {
    var v;
    if (k === 'battery') { v = h.cap ? h.battery / h.cap * 100 : 0; }
    else if (k === 'o2') { v = h.o2 / V('o2_cap') * 100; }
    else if (k === 'water') { v = h.water / V('water_cap') * 100; }
    else if (k === 'food') { v = h.food / V('food_start') * 100; }
    else if (k === 'health') { v = h.health; }
    else { v = h.dose / V('dose_limit') * 100; }
    return Math.max(0, Math.min(100, v));
  }
  function chartSVG(st) {
    var H = st.history, n = H.length, W = 760, HT = 340, L = 48, R = 16, T = 30, B = 44;
    var x = function (i) { return L + (W - L - R) * (n <= 1 ? 0 : i / (n - 1)); };
    var y = function (p) { return T + (HT - T - B) * (1 - p / 100); };
    var s = '<svg viewBox="0 0 ' + W + ' ' + HT + '" role="img" aria-label="Line chart of energy, oxygen, water, food, health and radiation dose over ' + n + ' days. A table with the same numbers is below.">';
    [0, 25, 50, 75, 100].forEach(function (p) {
      s += '<line x1="' + L + '" y1="' + y(p) + '" x2="' + (W - R) + '" y2="' + y(p) + '" stroke="#2c3768" stroke-width="1"/><text x="' + (L - 6) + '" y="' + (y(p) + 4) + '" text-anchor="end" font-size="12" fill="#b5bfe0">' + p + '%</text>';
    });
    for (var i = 0; i < n; i++) {
      if (H[i].day === 1 || H[i].day % 5 === 0 || i === n - 1) { s += '<text x="' + x(i) + '" y="' + (HT - B + 18) + '" text-anchor="middle" font-size="12" fill="#b5bfe0">' + H[i].day + '</text>'; }
    }
    s += '<text x="' + ((L + W - R) / 2) + '" y="' + (HT - 6) + '" text-anchor="middle" font-size="12" fill="#b5bfe0">Mission day</text>';
    // events
    for (var j = 0; j < n; j++) {
      var h = H[j], prev = H[j - 1], sym = null;
      if (h.night && !(prev && prev.night)) { sym = '☾'; }
      else if (prev && prev.night && !h.night) { sym = '☀'; }
      else if (h.solarStorm) { sym = '☢'; }
      else if (h.dustStorm && !(prev && prev.dustStorm)) { sym = '≋'; }
      else if (!h.dustStorm && prev && prev.dustStorm) { sym = '✓'; }
      if (sym) { s += '<line x1="' + x(j) + '" y1="' + T + '" x2="' + x(j) + '" y2="' + (HT - B) + '" stroke="#8a95c9" stroke-dasharray="3 4" stroke-width="1"/><text x="' + x(j) + '" y="' + (T - 8) + '" text-anchor="middle" font-size="15" fill="#fff">' + sym + '</text>'; }
    }
    SERIES.forEach(function (sr) {
      var d = H.map(function (h, idx) { return (idx ? 'L' : 'M') + x(idx).toFixed(1) + ',' + y(pctOf(h, sr.k)).toFixed(1); }).join(' ');
      s += '<path d="' + d + '" fill="none" stroke="' + sr.col + '" stroke-width="' + (sr.k === 'health' ? 3.5 : 2.5) + '"' + (sr.dash ? ' stroke-dasharray="' + sr.dash + '"' : '') + ' stroke-linejoin="round"/>';
    });
    var last = H[n - 1];
    s += '<circle cx="' + x(n - 1) + '" cy="' + y(pctOf(last, 'health')) + '" r="5" fill="#ff8fb1" stroke="#fff"/>';
    s += '</svg>';
    var lg = '<div class="legend">' + SERIES.map(function (sr) { return '<span><svg viewBox="0 0 36 10" aria-hidden="true"><line x1="0" y1="5" x2="36" y2="5" stroke="' + sr.col + '" stroke-width="3"' + (sr.dash ? ' stroke-dasharray="' + sr.dash + '"' : '') + '/></svg>' + sr.name + '</span>'; }).join('') +
      '<span>☾ night starts · ☀ sunrise · ☢ solar storm · ≋ dust storm</span></div>';
    return s + lg;
  }
  function tableHTML(st) {
    var h = '<table><thead><tr><th>Day</th><th>Battery ⚡</th><th>Oxygen kg</th><th>Water L</th><th>Food kg</th><th>Health</th><th>Dose mSv</th><th>Event</th></tr></thead><tbody>';
    st.history.forEach(function (r) {
      h += '<tr><td>' + r.day + '</td><td>' + n1(r.battery) + '</td><td>' + n1(r.o2) + '</td><td>' + n1(r.water) + '</td><td>' + n1(r.food) + '</td><td>' + n0(r.health) + '</td><td>' + n1(r.dose) + '</td><td style="text-align:left">' + esc(r.events.join(' ')) + '</td></tr>';
    });
    return h + '</tbody></table>';
  }

  function showDebrief() {
    var st = G.st, a = DB.analyze(st), win = a.ok;
    show('debrief');
    var stars = '';
    if (win) { stars = '<div class="stars" role="img" aria-label="' + a.stars + ' of 3 stars">' + [1, 2, 3].map(function (i) { return '<span class="' + (i <= a.stars ? '' : 'off') + '">' + (i <= a.stars ? '★' : '☆') + '</span>'; }).join('') + '</div><p class="small muted">★ finish 30 days · ★★ science ' + V('star2_science') + '+ · ★★★ science ' + V('star3_science') + '+, health ' + V('star3_health') + '+, dose at most ' + (st.site === 'moon' ? V('star3_dose_moon') : V('star3_dose_mars')) + ' mSv ' + badge('star3_science') + '</p>'; }
    $('deb-head').className = 'deb-head ' + (win ? 'win' : 'loss');
    $('deb-head').innerHTML = '<h1 id="deb-title" tabindex="-1">' + (win ? '✔ ' : '⚑ ') + esc(a.headline) + '</h1>' + stars +
      '<p>' + (st.site === 'moon' ? 'Moon' : 'Mars') + ' mission · final health ' + n0(st.health) + ' · science ' + st.science + ' · dose ' + n1(st.dose) + ' mSv</p>';
    $('chain').innerHTML = a.chain.map(function (c) { return '<li><span class="day">Day ' + c.day + '</span>' + esc(c.text) + '</li>'; }).join('');
    $('chart').innerHTML = chartSVG(st);
    $('chart-table').innerHTML = tableHTML(st);
    $('tradeoff').textContent = a.tradeoff;
    $('tip').textContent = a.tip;
    $('btn-other').textContent = 'Try the ' + (st.site === 'moon' ? 'Mars' : 'Moon') + ' outpost';
    $('btn-other').dataset.site = st.site === 'moon' ? 'mars' : 'moon';
    $('btn-again').dataset.site = st.site;
    var t = $('deb-title'); if (t) { t.focus({ preventScroll: true }); }
  }

  /* ---------------- data panel ---------------- */
  function renderData(filter, focusId) {
    filter = filter || 'All';
    var groups = {}, order = [];
    PB.DATA.values.forEach(function (v) {
      if (filter !== 'All' && v.tag !== filter) { return; }
      if (!groups[v.group]) { groups[v.group] = []; order.push(v.group); }
      groups[v.group].push(v);
    });
    var h = '';
    order.forEach(function (g) {
      h += '<h3>' + esc(g) + '</h3><table class="dt"><thead><tr><th>Number</th><th>Tag</th><th>What it means</th><th>Source</th></tr></thead><tbody>';
      groups[g].forEach(function (v) {
        var T = PB.DATA.tags[v.tag];
        var src = v.url ? '<a href="' + v.url + '" target="_blank" rel="noopener noreferrer">' + esc(v.source) + ' ↗</a>' : esc(v.source);
        h += '<tr data-row-id="' + v.id + '"><td><strong>' + esc(v.label) + '</strong><br>' + esc(String(v.value)) + ' ' + esc(v.unit) + '</td><td><span class="tag tag-' + v.tag.toLowerCase() + '"><span class="tag-icon" aria-hidden="true">' + T.icon + '</span> ' + T.word + '</span></td><td>' + esc(v.explain) + '</td><td>' + src + '</td></tr>';
      });
      h += '</tbody></table>';
    });
    $('data-body').innerHTML = h;
    $('data-note').textContent = 'Solar storm days come from two real storms (11 May 2024 and 3 October 2024, NASA DONKI). The game does not show flare IDs or classes because they were not confirmed when the game was built. Moon radiation comes from a Chinese-led mission (Chang\'e 4), not NASA.';
    document.querySelectorAll('#dlg-data .filters .btn').forEach(function (b) { var on = b.dataset.filter === filter; b.classList.toggle('is-on', on); b.setAttribute('aria-pressed', on ? 'true' : 'false'); });
    if (focusId) {
      var row = $('data-body').querySelector('[data-row-id="' + focusId + '"]');
      if (row) { row.classList.add('flash'); setTimeout(function () { row.scrollIntoView({ block: 'center' }); }, 30); }
    }
  }
  function openData(focusId) {
    renderData('All', focusId);
    var d = $('dlg-data');
    if (!d.open) { d.showModal(); }
  }

  /* ---------------- events ---------------- */
  document.addEventListener('click', function (e) {
    var t = e.target;
    var tag = t.closest && t.closest('[data-tag-id]');
    if (tag) { openData(tag.dataset.tagId); return; }
    var start = t.closest && t.closest('[data-start]');
    if (start) { startGame(start.dataset.start); return; }
    var bld = t.closest && t.closest('[data-build]');
    if (bld && !bld.disabled) {
      var ty = bld.dataset.build, dir = +bld.dataset.dir;
      G.cart[ty] = Math.max(0, (G.cart[ty] || 0) + dir);
      if (!G.cart[ty]) { delete G.cart[ty]; }
      renderBuild('[data-build="' + ty + '"][data-dir="' + dir + '"]');
      refresh();
      return;
    }
    var my = t.closest && t.closest('[data-myth]');
    if (my) { answerMyth(my.dataset.myth); return; }
    var fl = t.closest && t.closest('[data-filter]');
    if (fl) { renderData(fl.dataset.filter); return; }
    switch (t.id) {
      case 'btn-run': runToday(); break;
      case 'hint-ok': G.hintDismissedDay = G.st.day; renderHint(); break;
      case 'hint-off': G.hintsOff = true; renderHint(); break;
      case 'btn-data': case 'btn-deb-data': openData(); break;
      case 'btn-data-close': $('dlg-data').close(); break;
      case 'btn-teacher': show('teacher'); break;
      case 'btn-teacher-back': show(prevScreen === 'teacher' ? 'brief' : prevScreen); if (prevScreen === 'game') { renderStrip(); } break;
      case 'btn-print': window.print(); break;
      case 'btn-restart':
        if ($('screen-game').hidden || window.confirm('Start a new mission? Your current mission will be lost.')) { G = null; show('brief'); }
        break;
      case 'btn-again': case 'btn-other': startGame(t.dataset.site); break;
    }
  });
  document.addEventListener('keydown', function (e) {
    if ((e.key === 'Enter' || e.key === ' ') && e.target.matches && e.target.matches('span.tag[data-tag-id]')) { e.preventDefault(); openData(e.target.dataset.tagId); }
  });
  document.addEventListener('change', function (e) {
    var t = e.target;
    if (!G || !t.matches) { return; }
    if (t.matches('input[data-sw]')) {
      var id = t.dataset.sw, on = t.checked;
      if (id === 'oxygen') { G.plan.oxygen = on; }
      else if (id === 'lab') { G.plan.lab = on; }
      else if (id === 'repair') { G.plan.repair = on; }
      else if (id === 'emergency') { G.plan.emergency = on; }
      else if (id.indexOf('gh') === 0) { G.plan.gh[+id.slice(2)] = on; }
      else if (id.indexOf('dr') === 0) { G.plan.dr[+id.slice(2)] = on; }
      t.parentNode.querySelector('.sw-label').textContent = on ? 'On' : 'Off';
      refresh();
    } else if (t.name === 'act') { G.plan.activity = t.value; refresh(); }
    else if (t.name === 'ration') { G.plan.ration = t.value; refresh(); }
  });
  $('dlg-myth').addEventListener('close', function () { if (G && !G.st.over) { $('sec-report').focus({ preventScroll: true }); } });
  $('dlg-data').addEventListener('click', function (e) { if (e.target === this) { this.close(); } });
  // make static tags in the briefing keyboard-friendly
  document.querySelectorAll('span.tag[data-tag-id]').forEach(function (s) { s.setAttribute('role', 'button'); s.setAttribute('tabindex', '0'); });

  // test hook (used by automated browser checks)
  window.PB.ui = { startGame: startGame, state: function () { return G; } };
})();
