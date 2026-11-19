import { emit, markup, ctcAlign } from '@storyteller-platform/align';
import { readFile, writeFile, mkdir, copyFile, rename, stat, rm } from 'node:fs/promises';
import { createReadStream } from 'node:fs';
import { createHash, randomUUID } from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { convert, removeAmbiguousCues } from './convert.mjs';
import pino from 'pino';
import { prepareEpub } from './prepare-epub.mjs';
const root=path.dirname(fileURLToPath(import.meta.url));
process.env.PATH=[path.join(root,'node_modules/ffmpeg-static'),path.join(root,'node_modules/ffprobe-static/bin/win32/x64'),process.env.PATH].join(path.delimiter);
export async function fingerprint(file) {
  const hash=createHash('sha256');
  for await (const chunk of createReadStream(file)) hash.update(chunk);
  return {path:path.resolve(file),size:(await stat(file)).size,sha256:hash.digest('hex')};
}
export async function run(job) {
  // Keep all upstream scratch files in the same protected Windows cache.
  // CopyFile cannot move EFS-encrypted LocalState files to unencrypted %TEMP%.
  process.env.STORYTELLER_SCRATCH_DIR=path.join(path.resolve(job.cache),'scratch');
  await mkdir(process.env.STORYTELLER_SCRATCH_DIR,{recursive:true});
  const language=job.language ?? 'en';
  const device=job.device ?? 'cpu';
  if(!['cpu','dml'].includes(device)) throw new Error('Unsupported acceleration setting');
  let actualDevice=null, lastProgress='';
  const reportProgress=(stage,fraction=null,detail=null)=>{
    const value={stage,fraction: fraction===null ? null : Math.max(0,Math.min(1,Math.round(fraction*100)/100)),detail};
    const encoded=JSON.stringify(value);
    if(encoded!==lastProgress) {console.log('PROGRESS:'+encoded);lastProgress=encoded;}
  };
  reportProgress('Checking ebook and audio files');
  // EPUB validation/upgrade must happen before expensive audio work.
  const preparedEpub=await prepareEpub(job.epub,path.join(job.cache,'epub-preflight'));
  const inputs={epub:await fingerprint(job.epub),sources:await Promise.all(job.sources.map(fingerprint))};
  // Preserve the existing CPU cache identity, including the user's current job.
  const identity=createHash('sha256').update(JSON.stringify({engine:'0.2.16',schema:1,language,inputs,...(device==='cpu'?{}:{device})})).digest('hex');
  const work=path.join(job.cache,identity);
  await mkdir(path.join(work,'audio'),{recursive:true});
  const target=path.join(work,'alignment.json');
  try {
    const cached=JSON.parse(await readFile(target,'utf8'));
    if(cached.schemaVersion===1 && cached.audiobookId===job.audiobookId) {
      if(cached.adapterVersion!==2) {
        cached.suppressedAmbiguousCues=removeAmbiguousCues(cached.passages);cached.adapterVersion=2;
        if(!cached.passages.some(p=>p.cues.length)) throw new Error('No unambiguous matching words were found.');
        const migrated=target+'.'+randomUUID()+'.tmp';await writeFile(migrated,JSON.stringify(cached));await rename(migrated,target);
      }
      console.log('Using cached alignment');return {target,model:cached};
    }
  } catch(e) {if(e.code!=='ENOENT' && !(e instanceof SyntaxError)) throw e;}
  reportProgress('Preparing ebook text');
  const emissions=path.join(work,'emissions'), marked=path.join(work,'marked.epub'), aligned=path.join(work,'aligned.epub'), reports=path.join(work,'report.json');
  await markup(preparedEpub,marked,{granularity:'word',primaryLocale:new Intl.Locale(language)});
  const sources=[];
  for(let i=0;i<job.sources.length;i++) {
    reportProgress('Preparing audio files',null,`File ${i+1} of ${job.sources.length}`);
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
  const deviceDescription=()=>actualDevice ? (actualDevice==='cpu' && device!=='cpu' ? 'CPU · GPU unavailable, using CPU fallback' : actualDevice==='cpu'?'CPU':'GPU · DirectML') : device==='cpu'?'CPU':'GPU requested · DirectML (preview)';
  const progress=stage=>p=>reportProgress(stage,p>0?p:null,deviceDescription());
  const logger=pino({level:'info'}, {write(chunk){
    try {
      const message=JSON.parse(chunk).msg ?? '';
      const match=message.match(/Model load .*\bon (cpu|dml|webgpu|cuda|mlx)\b/);
      if(match) {actualDevice=match[1];reportProgress('Analysing audio',null,deviceDescription());}
    } catch { /* Non-progress logs do not change user-visible state. */ }
  }});
  reportProgress('Loading speech model',null,deviceDescription()+' · first use may download the model');
  await emit(path.join(work,'audio'),emissions,{model:'quartznet15x5',device,language:new Intl.Locale(language),logger,onProgress:p=>{
    // Automated recovery checks can forbid expensive inference: upstream reports
    // cached/planned before it loads a model or starts any analysis.
    if(job.requireCachedEmissions && p<1) throw new Error('Completed audio analysis is unavailable; recovery test stopped without starting inference.');
    progress('Analysing audio')(p);
  }});
  reportProgress('Matching words to audio');
  await ctcAlign(marked,aligned,emissions,path.join(work,'audio'),{granularity:'word',primaryLocale:new Intl.Locale(language),reportsPath:reports,onProgress:progress('Matching words to audio')});
  reportProgress('Saving and checking alignment');
  const report=JSON.parse(await readFile(reports,'utf8'));
  const passages=await convert(preparedEpub,marked,aligned,sources,report);
  if(!passages.some(p=>p.cues.length)) throw new Error('No reliable matching passages found. Check the book edition and language.');
  // Detect source modifications during processing; never publish a stale result.
  const after={epub:await fingerprint(job.epub),sources:await Promise.all(job.sources.map(fingerprint))};
  if(JSON.stringify(inputs)!==JSON.stringify(after)) throw new Error('Inputs changed while aligning; please retry.');
  const model={schemaVersion:1,adapterVersion:2,audiobookId:job.audiobookId,engine:`@storyteller-platform/align@0.2.16;quartznet15x5;${actualDevice ?? device+'-requested'}`,requestedDevice:device,actualDevice,status:'ready',language,epub:inputs.epub,sources,passages};
  const temporary=target+'.'+randomUUID()+'.tmp';
  await writeFile(temporary,JSON.stringify(model));await rename(temporary,target);
  // Audio is already in the original library; retain only emissions, markup and maps.
  await rm(aligned,{force:true});
  reportProgress('Read Along is ready',1);
  return {target,model};
}
if(process.argv[1] && path.resolve(process.argv[1])===fileURLToPath(import.meta.url)) {
  try {
    const job=JSON.parse(await readFile(process.argv[2],'utf8'));
    const {target}=await run(job);
    console.log(`RESULT:${target}`);
  } catch(e) {console.error('ERROR:'+JSON.stringify({message:e.message ?? String(e)}));console.error(e.stack ?? String(e));process.exitCode=1;}
}
