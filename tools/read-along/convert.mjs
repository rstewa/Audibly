// Consume verified Storyteller ID-fragment SMIL. Never display decoded speech.
import { Epub } from '@storyteller-platform/epub';
import path from 'node:path';
const elements = n => Array.from(n.getElementsByTagName('*'));
const type = n => n.getAttribute('epub:type') ?? '';
const hasType = (n, value) => type(n).split(/\s+/).some(t => t === `storyteller:${value}`);
const bodyText = doc => elements(doc).find(n => n.localName === 'body')?.textContent ?? '';
// Do not guess which word wins when the upstream matcher emits overlapping
// ranges. Remove each entire connected overlap group; keep all published text.
export function removeAmbiguousCues(passages) {
  const tracks=new Map();
  for(const passage of passages) for(const cue of passage.cues) {
    if(!tracks.has(cue.sourceIndex)) tracks.set(cue.sourceIndex,[]);
    tracks.get(cue.sourceIndex).push(cue);
  }
  const rejected=new Set();
  for(const track of tracks.values()) {
    track.sort((a,b)=>a.startSeconds-b.startSeconds);
    let group=[],end=-Infinity;
    const finish=()=>{if(group.length>1) for(const cue of group) rejected.add(cue);};
    for(const cue of track) {
      if(group.length && cue.startSeconds>=end) {finish();group=[];end=-Infinity;}
      group.push(cue);end=Math.max(end,cue.endSeconds);
    }
    finish();
  }
  for(const passage of passages) passage.cues=passage.cues.filter(c=>!rejected.has(c));
  return rejected.size;
}
function offsets(root, target) {
  let at = 0, found;
  function walk(n) {
    if (n === target) found = at;
    if (n.nodeType === 3 || n.nodeType === 4) at += n.data.length;
    else for (const child of Array.from(n.childNodes ?? [])) walk(child);
  }
  walk(root); return found;
}
function clock(value) {
  if (/^\d+(\.\d+)?s$/.test(value)) return Number(value.slice(0,-1));
  throw new Error(`Unsupported SMIL clock: ${value}`);
}
export async function convert(originalPath, markedPath, alignedPath, sources, report) {
  const original = await Epub.from(originalPath,{readonly:true});
  const marked = await Epub.from(markedPath,{readonly:true});
  const aligned = await Epub.from(alignedPath,{readonly:true});
  try {
    const passages = [], manifest = await aligned.getManifest();
    const originalSpine = await original.getSpineItems();
    const markedSpine = await marked.getSpineItems();
    const alignedSpine = await aligned.getSpineItems();
    for (let i=0;i<originalSpine.length;i++) {
      const src = originalSpine[i], item = markedSpine[i], output = alignedSpine.find(s=>s.id===item.id);
      const originalDoc = await original.readXhtmlItemContents(src.id);
      const doc = await marked.readXhtmlItemContents(item.id);
      // Markup may add spans, but is not allowed to alter any published text.
      if (bodyText(originalDoc) !== bodyText(doc)) throw new Error(`EPUB text changed during markup: ${src.href}`);
      const sentences = elements(doc).filter(n=>hasType(n,'sentence-span'));
      const byId = new Map(elements(doc).map(n=>[n.getAttribute('id'),n]));
      const cues = new Map();
      if (output?.mediaOverlay) {
        const overlay = manifest[output.mediaOverlay];
        const smil = await aligned.readXhtmlItemContents(overlay.id);
        for (const par of elements(smil).filter(n=>n.localName==='par')) {
          // Reject interpolated, unmatched, and audio-only spans and their children.
          let matched = hasType(par,'matched'), unsafe = false;
          for (let n=par;n?.getAttribute;n=n.parentNode) {
            if (/storyteller:(interpolated|unmatched|dropped|audio-only)/.test(type(n))) unsafe=true;
          }
          if (!matched || unsafe) continue;
          const text = elements(par).find(n=>n.localName==='text');
          const audio = elements(par).find(n=>n.localName==='audio');
          if (!text || !audio) continue;
          const [href,fragment] = text.getAttribute('src').split('#');
          const resolved = path.posix.normalize(path.posix.join(path.posix.dirname(overlay.href), decodeURIComponent(href)));
          if (resolved !== decodeURIComponent(output.href)) throw new Error('Unexpected cross-chapter SMIL reference');
          const element = byId.get(decodeURIComponent(fragment ?? ''));
          if (!element) throw new Error('Missing EPUB fragment');
          const track = decodeURIComponent(path.posix.basename(audio.getAttribute('src')));
          const source = sources.find(s=>s.stagedName===track);
          // We deliberately bypass Storyteller process/splitting. No inferred offsets.
          if (!source) throw new Error(`Unknown processed track: ${track}`);
          const start=clock(audio.getAttribute('clipBegin')), end=clock(audio.getAttribute('clipEnd'));
          if (!Number.isFinite(start) || end<=start || start<0) continue;
          const spineReport = report.spine.find(s=>s.index===i);
          if (!spineReport || spineReport.score===null || spineReport.score<80) continue;
          const cue = {id:`${i}:${fragment}:${source.index}:${start}`,fragment,sourceIndex:source.index,startSeconds:start,endSeconds:end,granularity:hasType(element,'word-span')?'word':'sentence',quality:spineReport.score};
          if (!cues.has(fragment)) cues.set(fragment,[]);
          cues.get(fragment).push(cue);
        }
      }
      for (const sentence of sentences) {
        const sentenceCues = [];
        for (const el of [sentence,...elements(sentence)]) {
          for (const cue of cues.get(el.getAttribute('id')) ?? []) {
            sentenceCues.push({...cue,offset:offsets(sentence,el),length:el.textContent.length});
          }
        }
        passages.push({id:`${i}:${sentence.getAttribute('id')}`,href:src.href,fragment:sentence.getAttribute('id'),text:sentence.textContent,cues:sentenceCues});
      }
    }
    removeAmbiguousCues(passages);
    return passages;
  } finally {original.discardAndClose();marked.discardAndClose();aligned.discardAndClose();}
}
