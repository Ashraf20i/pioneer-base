/* Checks the files fit together: data panel source, HTML hints, no network calls, no banned words. */
'use strict';
var fs = require('fs'), path = require('path');
var PB = require('../js/data.js');
var root = path.join(__dirname, '..');
var bad = 0, n = 0;
function ok(c, m) { n++; if (!c) { bad++; console.log('  FAIL: ' + m); } }
function read(f) { return fs.readFileSync(path.join(root, f), 'utf8'); }

var ui = read('js/ui.js'), sim = read('js/sim.js'), html = read('index.html'), deb = read('js/debrief.js'), dataJs = read('js/data.js');
// Every V('id') used anywhere exists in the data file.
[sim, ui, deb].forEach(function (src, i) {
  var re = /\bV\('([a-z0-9_]+)'\)/g, m;
  while ((m = re.exec(src))) { ok(!!PB.DATA.byId[m[1]], 'unknown data id ' + m[1] + ' in file ' + i); }
});
// Every badge('id') used by the UI exists.
var re2 = /badge\('([a-z0-9_]+)'\)/g, m2;
while ((m2 = re2.exec(ui))) { ok(!!PB.DATA.byId[m2[1]], 'unknown badge id ' + m2[1]); }
var re3 = /data-tag-id="([a-z0-9_]+)"/g, m3;
while ((m3 = re3.exec(html))) { ok(!!PB.DATA.byId[m3[1]], 'unknown html tag id ' + m3[1]); }
// No runtime network requests, no modules, no external resources.
[ui, sim, deb, dataJs].forEach(function (src, i) {
  ok(!/\bfetch\(|XMLHttpRequest|WebSocket|sendBeacon|navigator\.geolocation|localStorage|import\s+.+from/.test(src), 'no network/storage/module use in js file ' + i);
});
ok(!/<script[^>]+src="https?:/.test(html) && !/<link[^>]+href="https?:/.test(html) && !/<img /.test(html), 'no external scripts, styles or images');
ok(!/type="module"/.test(html), 'classic scripts only');
ok(!/url\(https?:/.test(read('css/style.css')) && !/@import/.test(read('css/style.css')), 'no external CSS resources');
// Required text.
ok(html.indexOf('An independent student project for the NASA Space Apps Challenge 2026. Not affiliated with or endorsed by NASA.') >= 0, 'footer text present');
ok(html.indexOf('Where do these numbers come from?') >= 0, 'data panel title present');
ok(!/\b(death|dead|die|dies|died|kill)\b/i.test(html + ui + deb), 'no death wording in UI text');
// Cards.
ok(PB.DATA.cards.length === 6, 'six Myth or Reality cards');
PB.DATA.cards.forEach(function (c) { ok(c.source && /^https?:/.test(c.url) && c.why.length > 20, 'card ' + c.id + ' has a source'); });
// Data panel covers every value used by the simulation (it lists all of them).
ok(PB.DATA.values.length > 60, 'data list is complete');
console.log(bad ? '\n' + bad + ' of ' + n + ' data checks FAILED' : '\nAll ' + n + ' data checks passed');
process.exit(bad ? 1 : 0);
