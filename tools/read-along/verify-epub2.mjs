import assert from 'node:assert/strict';
import { createWriteStream } from 'node:fs';
import { writeFile, readFile } from 'node:fs/promises';
import path from 'node:path';
import { ZipFile } from 'yazl';
import { MemoryAdapter } from '@storyteller-platform/epub';
import { markup, ctcAlign } from '@storyteller-platform/align';
import { prepareEpub, publicationText } from './prepare-epub.mjs';
import { convert } from './convert.mjs';
import { run } from './align.mjs';

const base=path.resolve('tools/read-along/sample');
const archive=await MemoryAdapter.init(path.join(base,'book.epub'));
const fixture=path.join(base,'book-epub2.epub');
const entries={
  mimetype:'application/epub+zip',
  'META-INF/container.xml':await archive.read('META-INF/container.xml','utf-8'),
  'chapter.xhtml':await archive.read('chapter.xhtml','utf-8'),
  'content.opf':'<package xmlns="http://www.idpf.org/2007/opf" version="2.0" unique-identifier="id"><metadata xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:identifier id="id">urn:audibly:epub2-test</dc:identifier><dc:title>The River Visit</dc:title><dc:language>en</dc:language></metadata><manifest><item id="chapter" href="chapter.xhtml" media-type="application/xhtml+xml"/><item id="ncx" href="toc.ncx" media-type="application/x-dtbncx+xml"/></manifest><spine toc="ncx"><itemref idref="chapter"/></spine></package>',
  'toc.ncx':'<ncx xmlns="http://www.daisy.org/z3986/2005/ncx/" version="2005-1"><head><meta name="dtb:uid" content="urn:audibly:epub2-test"/></head><docTitle><text>The River Visit</text></docTitle><navMap><navPoint id="chapter" playOrder="1"><navLabel><text>The River Visit</text></navLabel><content src="chapter.xhtml"/></navPoint></navMap></ncx>'
};
archive.dispose();
const zip=new ZipFile();
const completed=new Promise((resolve,reject)=>{const stream=createWriteStream(fixture);stream.on('close',resolve);stream.on('error',reject);zip.outputStream.on('error',reject);zip.outputStream.pipe(stream);});
for(const [name,value] of Object.entries(entries)) zip.addBuffer(Buffer.from(value),name,{compress:false});
zip.end();await completed;
const before=await publicationText(fixture);
const prepared=await prepareEpub(fixture,path.join(base,'epub2-preflight'));
const after=await publicationText(prepared);
assert.equal(before.version,'2.0');assert.equal(after.version,'3.0');assert.deepEqual(before.chapters,after.chapters);
assert.equal((await publicationText(fixture)).hash,before.hash);
assert.equal(await prepareEpub(fixture,path.join(base,'epub2-preflight')),prepared);
const marked=path.join(base,'epub2-marked.epub'),aligned=path.join(base,'epub2-aligned.epub'),reports=path.join(base,'epub2-report.json');
await markup(prepared,marked,{granularity:'word',primaryLocale:new Intl.Locale('en')});
// Reuse existing sample emissions; no speech inference is started by this test.
await ctcAlign(marked,aligned,path.join(base,'emissions'),path.join(base,'audio'),{granularity:'word',primaryLocale:new Intl.Locale('en'),reportsPath:reports});
const passages=await convert(prepared,marked,aligned,[{stagedName:'000000.wav',index:0}],JSON.parse(await readFile(reports,'utf8')));
assert.equal(passages.flatMap(p=>p.cues).length,202);
console.log('PASS EPUB 2: original preserved, exact text retained, converted copy cached, 202 real word cues using existing emissions');
const broken=path.join(base,'invalid.epub');await writeFile(broken,'not a zip');
await assert.rejects(()=>run({epub:broken,sources:['nonexistent-audio.mp3'],cache:path.join(base,'invalid-epub-cache')}),error=>error.code!=='ENOENT');
console.log('PASS invalid EPUB rejected before opening audio');
