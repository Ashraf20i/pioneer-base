'use strict';
var P = require('./players.js');
function rate(site, opts){ var w=0,N=100; for (var s=1;s<=N;s++){ var st=P.play(site,s,P.expert(opts)); if(st.ending.type==='success') w++; } return w/N; }
var cases = {
 mars: [
  ['expert', {}],
  ['noreactor 2solar bat gh drill shield', {builds:['solar','solar','battery','greenhouse','drill','shield']}],
  ['noreactor 2solar gh drill', {builds:['solar','solar','greenhouse','drill']}],
  ['noreactor 2solar 2bat gh drill', {builds:['solar','solar','battery','battery','greenhouse','drill']}],
  ['reactor gh gh drill', {builds:['reactor','greenhouse','greenhouse','drill']}],
  ['reactor gh drill shield noclean', {neverClean:true}],
  ['noreactor 2solar bat gh drill noclean', {neverClean:true, builds:['solar','solar','battery','greenhouse','drill','shield']}],
  ['nothing built', {builds:[]}],
 ],
 moon: [
  ['expert', {}],
  ['reactor drill gh shield shield', {builds:['reactor','drill','greenhouse','shield','shield']}],
  ['reactor gh shield shield solar', {builds:['reactor','greenhouse','shield','shield','solar']}],
  ['reactor drill drill solar solar', {builds:['reactor','drill','drill','solar','solar']}],
  ['reactor drill drill gh (no shield)', {builds:['reactor','drill','drill','greenhouse']}],
  ['noreactor 4bat shield shield', {builds:['battery','battery','battery','battery','shield','shield']}],
  ['noreactor solar bat drill gh shield', {builds:['solar','solar','battery','battery','shield','shield','drill']}],
  ['reactor shield only', {builds:['reactor','shield']}],
 ]};
Object.keys(cases).forEach(function(site){ console.log('== '+site); cases[site].forEach(function(c){ console.log(rate(site,c[1]).toFixed(2), c[0]); }); });
