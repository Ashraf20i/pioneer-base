/* Runs every check. Usage: node tests/run-all.js */
'use strict';
var cp = require('child_process'), path = require('path');
var files = ['engine.test.js', 'balance.test.js', 'data.test.js'], failed = 0;
files.forEach(function (f) {
  console.log('\n=== ' + f);
  var r = cp.spawnSync(process.execPath, [path.join(__dirname, f)], { stdio: 'inherit' });
  if (r.status !== 0) { failed++; }
});
console.log(failed ? '\n' + failed + ' test file(s) FAILED' : '\nALL TEST FILES PASSED');
process.exit(failed ? 1 : 0);
