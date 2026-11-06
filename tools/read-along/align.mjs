import { emit, markup, ctcAlign } from '@storyteller-platform/align';
import { readFile, writeFile, mkdir, copyFile, rename, stat, rm } from 'node:fs/promises';
import { createReadStream } from 'node:fs';
import { createHash, randomUUID } from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { convert } from './convert.mjs';
const root=path.dirname(fileURLToPath(import.meta.url));
process.env.PATH=[path.join(root,'node_modules/ffmpeg-static'),path.join(root,'node_modules/ffprobe-static/bin/win32/x64'),process.env.PATH].join(path.delimiter);
export async function fingerprint(file) {
  const hash=createHash('sha256');
  for await (const chunk of createReadStream(file)) hash.update(chunk);
  return {path:path.resolve(file),size:(await stat(file)).size,sha256:hash.digest('hex')};
}
export async function run(job) {
  const language=job.language ?? 'en';
  const inputs={epub:await fingerprint(job.epub),sources:await Promise.all(job.sources.map(fingerprint))};
  const identity=createHash('sha256').update(JSON.stringify({engine:'0.2.16',schema:1,language,inputs})).digest('hex');
  const work=path.join(job.cache,identity);
  await mkdir(path.join(work,'audio'),{recursive:true});
  const target=path.join(work,'alignment.json');
  try {const cached=JSON.parse(await readFile(target,'utf8')); if(cached.schemaVersion===1 && cached.audiobookId===job.audiobookId) {console.log('Using cached alignment');return {target,model:cached};}} catch(e) {if(e.code!=='ENOENT' && !(e instanceof SyntaxError)) throw e;}
  const sources=[];
  for(let i=0;i<job.sources.length;i++) {
    const stagedName=String(i).padStart(6,'0')+path.extname(job.sources[i]).toLowerCase();
    if(!['.m4b','.m4a','.mp3','.wav'].includes(path.extname(stagedName))) throw new Error('Unsupported audio format');
    const staged=path.join(work,'audio',stagedName);
    let valid=false;
    try {valid=(await fingerprint(staged)).sha256===inputs.sources[i].sha256;} catch(e) {if(e.code!=='ENOENT') throw e;}
    if(!valid) {
      const temp=staged+'.'+randomUUID()+'.tmp';
      await copyFile(job.sources[i],temp);await rename(temp,staged);
    }
    sources.push({...inputs.sources[i],index:i,stagedName});
  }
  const progress=stage=>p=>console.log(`${stage}: ${Math.round(p*100)}%`);
  const emissions=path.join(work,'emissions'), marked=path.join(work,'marked.epub'), aligned=path.join(work,'aligned.epub'), reports=path.join(work,'report.json');
  await emit(path.join(work,'audio'),emissions,{model:'quartznet15x5',device:'cpu',language:new Intl.Locale(language),onProgress:progress('Analysing audio')});
  await markup(job.epub,marked,{granularity:'word',primaryLocale:new Intl.Locale(language)});
  await ctcAlign(marked,aligned,emissions,path.join(work,'audio'),{granularity:'word',primaryLocale:new Intl.Locale(language),reportsPath:reports,onProgress:progress('Aligning EPUB')});
  const report=JSON.parse(await readFile(reports,'utf8'));
  const passages=await convert(job.epub,marked,aligned,sources,report);
  if(!passages.some(p=>p.cues.length)) throw new Error('No reliable matching passages found. Check the book edition and language.');
  // Detect source modifications during processing; never publish a stale result.
  const after={epub:await fingerprint(job.epub),sources:await Promise.all(job.sources.map(fingerprint))};
  if(JSON.stringify(inputs)!==JSON.stringify(after)) throw new Error('Inputs changed while aligning; please retry.');
  const model={schemaVersion:1,audiobookId:job.audiobookId,engine:'@storyteller-platform/align@0.2.16;quartznet15x5;cpu',status:'ready',language,epub:inputs.epub,sources,passages};
  const temporary=target+'.'+randomUUID()+'.tmp';
  await writeFile(temporary,JSON.stringify(model));await rename(temporary,target);
  // Audio is already in the original library; retain only emissions, markup and maps.
  await rm(aligned,{force:true});
  return {target,model};
}
if(process.argv[1] && path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  try {
    const job=JSON.parse(await readFile(process.argv[2],'utf8'));
    const {target}=await run(job);
    console.log(`RESULT:${target}`);
  } catch(e) {console.error(e.stack ?? String(e));process.exitCode=1;}
}
