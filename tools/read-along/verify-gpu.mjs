// Explicit Windows GPU smoke test. Do not run beside an active user alignment.
import assert from 'node:assert/strict';
import { mkdtemp } from 'node:fs/promises';
import path from 'node:path';
import { run } from './align.mjs';

assert.equal(process.platform, 'win32', 'This test verifies Windows DirectML');
const sample = path.resolve('tools/read-along/sample');
// Fresh emissions force actual model loading and inference, not a cache hit.
const cache = await mkdtemp(path.join(sample, 'gpu-test-'));
const { model } = await run({
  audiobookId: '44444444-4444-4444-4444-444444444444',
  epub: path.join(sample, 'book.epub'),
  sources: [path.join(sample, 'formats/single.m4a')],
  cache, language: 'en', device: 'dml',
});
assert.equal(model.requestedDevice, 'dml');
assert.equal(model.actualDevice, 'dml', 'GPU request must not silently pass with CPU fallback');
assert.equal(model.passages.flatMap(p => p.cues).length, 202);
console.log('PASS fresh M4A GPU inference: DirectML loaded, 202 aligned words');
