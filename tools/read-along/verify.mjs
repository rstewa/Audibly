import assert from 'node:assert/strict';
import { readFile, writeFile, copyFile, stat } from 'node:fs/promises';
import path from 'node:path';
import { Epub } from '@storyteller-platform/epub';
import { markup, ctcAlign } from '@storyteller-platform/align';
import { run, fingerprint } from './align.mjs';
import { convert } from './convert.mjs';
const base=path.resolve('tools/read-along/sample');
const job={audiobookId:'11111111-1111-1111-1111-111111111111',epub:path.join(base,'book.epub'),sources:[path.join(base,'audio/000000.wav')],cache:path.join(base,'cache')};
const {target,model}=await run(job);
const before=(await stat(target)).mtimeMs;
assert.equal((await run(job)).target,target);
assert.equal((await stat(target)).mtimeMs,before);
const words=model.passages.flatMap(p=>p.cues.map(c=>({text:p.text.slice(c.offset,c.offset+c.length),...c})));
assert.ok(words.length>150);assert.equal(words[0].text,'The');assert.equal(words[1].text,'morning');
assert.equal(words[0].startSeconds,0.06);
assert.equal(model.passages[0].cues.length,0); // unspoken heading
assert.ok(words.every(c=>c.granularity==='word' && c.endSeconds>c.startSeconds));
console.log(`PASS production map: ${words.length} words; exact EPUB text; source-local timestamps; cache reuse`);
const report=JSON.parse(await readFile(path.join(base,'reports'),'utf8'));
const shifted=await convert(job.epub,path.join(base,'marked.epub'),path.join(base,'aligned.epub'),[{stagedName:'000000.wav',index:1}],report);
assert.ok(shifted.flatMap(p=>p.cues).every(c=>c.sourceIndex===1));
await assert.rejects(()=>convert(job.epub,path.join(base,'marked.epub'),path.join(base,'aligned.epub'),[],report),/Unknown processed track/);
const low=structuredClone(report);low.spine[0].score=50;
assert.equal((await convert(job.epub,path.join(base,'marked.epub'),path.join(base,'aligned.epub'),[{stagedName:'000000.wav',index:0}],low)).flatMap(p=>p.cues).length,0);
console.log('PASS source mapping, unknown tracks rejected, low quality left unhighlighted');
const sentenceMarked=path.join(base,'sentence-marked.epub'),sentenceAligned=path.join(base,'sentence-aligned.epub');
await markup(job.epub,sentenceMarked,{granularity:'sentence',primaryLocale:new Intl.Locale('en')});
await ctcAlign(sentenceMarked,sentenceAligned,path.join(base,'emissions'),path.join(base,'audio'),{granularity:'sentence',primaryLocale:new Intl.Locale('en'),reportsPath:path.join(base,'sentence-report.json')});
const sentenceReport=JSON.parse(await readFile(path.join(base,'sentence-report.json'),'utf8'));
const sentencePassages=await convert(job.epub,sentenceMarked,sentenceAligned,[{stagedName:'000000.wav',index:0}],sentenceReport);
const sentenceCues=sentencePassages.flatMap(p=>p.cues);
assert.equal(sentenceCues.length,17);assert.ok(sentenceCues.every(c=>c.granularity==='sentence'));
console.log('PASS actual sentence-level SMIL fallback (17 sentences, labelled sentence)');
// A valid word reference nested within an estimated sentence must still be refused.
const untrusted=path.join(base,'interpolated.epub');await copyFile(path.join(base,'aligned.epub'),untrusted);
const estimated=await Epub.from(untrusted);
try {
 const manifest=await estimated.getManifest();
 for(const overlay of Object.values(manifest).filter(i=>i.mediaType==='application/smil+xml')) {
  const doc=await estimated.readXhtmlItemContents(overlay.id);
  for(const node of Array.from(doc.getElementsByTagName('*'))) if(node.localName==='seq' && node.getAttribute('epub:type')?.includes('storyteller:matched')) node.setAttribute('epub:type','storyteller:interpolated');
  await estimated.writeXhtmlItemContents(overlay.id,doc);
 }
 await estimated.saveAndClose();
} catch(error) {estimated.discardAndClose();throw error;}
assert.equal((await convert(job.epub,path.join(base,'marked.epub'),untrusted,[{stagedName:'000000.wav',index:0}],report)).flatMap(p=>p.cues).length,0);
console.log('PASS interpolated ancestor suppresses word highlighting');
const mismatch=path.join(base,'mismatch.epub');await copyFile(job.epub,mismatch);
const e=await Epub.from(mismatch);
try {
  const doc=await e.readXhtmlItemContents('chapter');
  const body=Array.from(doc.getElementsByTagName('*')).find(n=>n.localName==='body');
  while(body.firstChild)body.removeChild(body.firstChild);
  const p=doc.createElementNS('http://www.w3.org/1999/xhtml','p');
  p.textContent='Volcanic minerals crystallize at immense pressure beneath the continental shelf. Laboratory measurements describe complicated chemical reactions among silicon and oxygen. Astronomers photograph distant galaxies through powerful telescopes while satellites orbit the planet. A mathematical theorem requires a rigorous proof before publication. Experiments investigate magnetic fields and electrical conductivity in manufactured materials.';
  body.appendChild(p);
  await e.writeXhtmlItemContents('chapter',doc);await e.saveAndClose();
} catch(error) {e.discardAndClose();throw error;}
const marked=path.join(base,'mismatch-marked.epub'),aligned=path.join(base,'mismatch-aligned.epub'),reports=path.join(base,'mismatch-report.json');
await markup(mismatch,marked,{granularity:'word',primaryLocale:new Intl.Locale('en')});
await ctcAlign(marked,aligned,path.join(base,'emissions'),path.join(base,'audio'),{granularity:'word',primaryLocale:new Intl.Locale('en'),reportsPath:reports});
const mismatchReport=JSON.parse(await readFile(reports,'utf8'));
const unmatched=await convert(mismatch,marked,aligned,[{stagedName:'000000.wav',index:0}],mismatchReport);
assert.equal(unmatched.flatMap(p=>p.cues).length,0);
console.log('PASS real deliberately mismatched EPUB/audio: zero accepted cues');
await writeFile(path.join(base,'verification.json'),JSON.stringify({words:words.length,firstWords:words.slice(0,4),reportTotals:report.totals,mismatchTotals:mismatchReport.totals,map:target},null,2));
