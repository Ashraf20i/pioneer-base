var P=require('./players.js'),D=require('../js/debrief.js');
var shown={};
['moon','mars'].forEach(function(site){['naive','decent','expert','nothing'].forEach(function(w){for(var s=1;s<=40;s++){
 var pl=w==='naive'?P.naive():w==='expert'?P.expert():w==='nothing'?P.nothing():P.decent(s,site);
 var st=P.play(site,s,pl),a=D.analyze(st);
 var bad=false;for(var i=1;i<a.chain.length;i++)if(a.chain[i].day<a.chain[i-1].day)bad=true;
 var key=site+w+a.cause;
 if((bad||!shown[key])&&Object.keys(shown).length<40){shown[key]=1;console.log('---',site,w,s,a.cause,bad?'BAD ORDER':'');console.log(a.headline);a.chain.forEach(c=>console.log('  d'+c.day+': '+c.text));console.log(' trade:',a.tradeoff);console.log(' tip:',a.tip);}
}})});
