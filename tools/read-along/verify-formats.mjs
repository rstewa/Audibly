import { spawnSync } from 'node:child_process';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import assert from 'node:assert/strict';
import { run } from './align.mjs';
const base=path.resolve('tools/read-along/sample'), formats=path.join(base,'formats');
await mkdir(formats,{recursive:true});
const original=path.join(base,'audio/000000.wav');
const ffmpeg=path.resolve('tools/read-along/node_modules/ffmpeg-static/ffmpeg.exe');
function encode(file,options) {
 const result=spawnSync(ffmpeg,['-y','-v','error','-i',original,...options,file],{encoding:'utf8'});
 if(result.status!==0)throw new Error(result.stderr);
}
const m4b=path.join(formats,'single.m4b'),m4a=path.join(formats,'single.m4a');
encode(m4b,['-c:a','aac','-b:a','64k']);encode(m4a,['-c:a','aac','-b:a','64k']);
const mp3a=path.join(formats,'track-99.mp3'),mp3b=path.join(formats,'track-01.mp3');
encode(mp3a,['-t','35','-c:a','libmp3lame','-b:a','128k']);
encode(mp3b,['-ss','35','-c:a','libmp3lame','-b:a','128k']);
const evidence=[];
for(const [name,sources] of [['m4b',[m4b]],['m4a',[m4a]],['multi-mp3',[mp3a,mp3b]]]) {
 const result=await run({audiobookId:'22222222-2222-2222-2222-222222222222',epub:path.join(base,'book.epub'),sources,cache:path.join(base,'format-cache')});
 const cues=result.model.passages.flatMap(p=>p.cues);
 assert.ok(cues.length>150);
 if(sources.length===2) {assert.ok(cues.some(c=>c.sourceIndex===0));assert.ok(cues.some(c=>c.sourceIndex===1));assert.ok(cues.filter(c=>c.sourceIndex===1).every(c=>c.startSeconds<37));}
 evidence.push({name,cues:cues.length,sources:sources.length,map:result.target});
 console.log(`PASS ${name}: ${cues.length} cues on ${sources.length} original source file(s)`);
}
await writeFile(path.join(base,'format-verification.json'),JSON.stringify(evidence,null,2));
