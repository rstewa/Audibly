import assert from 'node:assert/strict';
import { readFile,writeFile,mkdir,stat } from 'node:fs/promises';
import path from 'node:path';
import { run,fingerprint } from './align.mjs';
import { createHash,randomUUID } from 'node:crypto';

// Pass a directory inside the packaged app's encrypted cache to reproduce its
// filesystem behavior. All inputs are the legal generated sample, not user books.
const cache=path.resolve(process.argv[2]);
const sample=path.resolve('tools/read-along/sample');
const inputDir=path.join(cache,'inputs');await mkdir(inputDir,{recursive:true});
const epub=path.join(inputDir,'book.epub'),audio=path.join(inputDir,'original.wav');
await writeFile(epub,await readFile(path.join(sample,'book.epub')));
await writeFile(audio,await readFile(path.join(sample,'audio/000000.wav')));
const inputs={epub:await fingerprint(epub),sources:[await fingerprint(audio)]};
const identity=createHash('sha256').update(JSON.stringify({engine:'0.2.16',schema:1,language:'en',inputs})).digest('hex');
const work=path.join(cache,identity);await mkdir(path.join(work,'audio'),{recursive:true});await mkdir(path.join(work,'emissions'),{recursive:true});
await writeFile(path.join(work,'audio/000000.wav'),await readFile(audio));
for(const suffix of ['emissions','identity']) await writeFile(path.join(work,'emissions/000000.'+suffix),await readFile(path.join(sample,'emissions/000000.'+suffix)));
const before=(await stat(path.join(work,'emissions/000000.emissions'))).mtimeMs;
const {target,model}=await run({audiobookId:randomUUID(),epub,sources:[audio],cache,requireCachedEmissions:true});
assert.equal(model.passages.flatMap(p=>p.cues).length,202);
assert.equal((await stat(path.join(work,'emissions/000000.emissions'))).mtimeMs,before);
assert.equal(process.env.STORYTELLER_SCRATCH_DIR,path.join(cache,'scratch'));
console.log('PASS protected app cache: 202 cues, inference forbidden, emissions untouched, scratch stays protected');
console.log('RESULT:'+target);
