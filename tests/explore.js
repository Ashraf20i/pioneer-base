/* Quick balance explorer: node tests/explore.js */
'use strict';
var P = require('./players.js');
var N = 200;
function stats(site, mk) {
  var win = 0, three = 0, evac = 0, endDays = {}, reasons = {};
  for (var s = 1; s <= N; s++) {
    var st = P.play(site, s, mk(s, site));
    if (st.ending.type === 'success') { win++; if (st.ending.stars === 3) { three++; } } else { evac++; var k = st.ending.reason + '@' + st.ending.day; reasons[k] = (reasons[k] || 0) + 1; }
  }
  return { win: win / N, three: three / N, evac: evac / N, reasons: reasons };
}
['moon', 'mars'].forEach(function (site) {
  console.log('=== ' + site);
  console.log('nothing', JSON.stringify(stats(site, function () { return P.nothing(); }).evac));
  console.log('naive  ', JSON.stringify(stats(site, function () { return P.naive(); }).evac));
  var d = stats(site, function (s, si) { return P.decent(s, si); }); console.log('decent win', d.win);
  var e = stats(site, function () { return P.expert(); }); console.log('expert win', e.win, '3star', e.three, JSON.stringify(e.reasons));
});
