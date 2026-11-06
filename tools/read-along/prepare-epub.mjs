import { Epub, MemoryAdapter, parse } from '@storyteller-platform/epub';
import { readFile, writeFile, mkdir, rename, rm } from 'node:fs/promises';
import { createHash, randomUUID } from 'node:crypto';
import path from 'node:path';

const descendants = n => Array.from(n.getElementsByTagName('*'));
function archivePath(href, relativeTo = '') {
  const result = path.posix.normalize(path.posix.join(path.posix.dirname(relativeTo), decodeURIComponent(href.split('#')[0])));
  if (result.startsWith('../') || result.startsWith('/') || /^[a-z]+:/i.test(result)) throw new Error('Unsupported EPUB content path.');
  return result;
}
// Read the original archive through the upstream low-level adapter: no version
// assertion, mutation, speech transcript or custom alignment algorithm involved.
export async function publicationText(file) {
  const bytes = await readFile(file);
  const archive = await MemoryAdapter.init(bytes);
  try {
    const container = parse(await archive.read('META-INF/container.xml','utf-8'),'application/xml');
    const packagePath = descendants(container).find(n=>n.localName==='rootfile')?.getAttribute('full-path');
    if (!packagePath) throw new Error('EPUB has no package document.');
    const pkg = parse(await archive.read(packagePath,'utf-8'),'application/xml');
    const items = new Map(descendants(pkg).filter(n=>n.localName==='item').map(n=>[n.getAttribute('id'),n.getAttribute('href')]));
    const chapters = [];
    for (const reference of descendants(pkg).filter(n=>n.localName==='itemref')) {
      const href=items.get(reference.getAttribute('idref'));
      if(!href) throw new Error('EPUB has a missing reading-order item.');
      const doc=parse(await archive.read(archivePath(href,packagePath),'utf-8'),'application/xhtml+xml');
      const body=descendants(doc).find(n=>n.localName==='body');
      if(!body) throw new Error('EPUB contains an unsupported reading-order document.');
      chapters.push({href,text:body.textContent});
    }
    if(!chapters.length) throw new Error('EPUB has no readable chapters.');
    return {version:pkg.documentElement.getAttribute('version'),chapters,hash:createHash('sha256').update(bytes).digest('hex')};
  } finally {archive.dispose();}
}

export async function prepareEpub(input, directory) {
  const original=await publicationText(input);
  if(original.version?.startsWith('3')) {
    const epub=await Epub.from(input,{readonly:true});epub.discardAndClose();return input;
  }
  if(!original.version?.startsWith('2')) throw new Error('This ebook uses an unsupported EPUB version.');
  await mkdir(directory,{recursive:true});
  const output=path.resolve(directory,original.hash+'.epub');
  const verify=async file=>{
    const result=await publicationText(file);
    if(!result.version?.startsWith('3') || JSON.stringify(original.chapters)!==JSON.stringify(result.chapters))
      throw new Error('EPUB conversion changed the published text. Preparation stopped to protect the original book.');
  };
  try {await verify(output);return output;} catch(e) {if(e.code!=='ENOENT') throw e;}
  const temporary=output+'.'+randomUUID()+'.epub';
  try {
    // Upgrade a byte copy in place; never give the upgrader the user's original.
    await writeFile(temporary,await readFile(input));
    const upgraded=await Epub.upgrade(temporary);
    try {await upgraded.saveAndClose();} catch(error) {upgraded.discardAndClose();throw error;}
    await verify(temporary);
    await rename(temporary,output);
    return output;
  } finally {await rm(temporary,{force:true});}
}
