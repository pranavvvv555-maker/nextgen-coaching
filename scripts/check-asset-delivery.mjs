import assert from 'node:assert/strict';
import http from 'node:http';
import https from 'node:https';
import {brotliDecompressSync, gunzipSync} from 'node:zlib';

const base = new URL(process.argv[2] || 'http://localhost:5102');
function request(path, headers = {}) {
  return new Promise((resolve, reject) => {
    const url = new URL(path, base), transport = url.protocol === 'https:' ? https : http;
    const started = performance.now();
    const req = transport.get(url, {headers}, response => {
      const parts = [];
      response.on('data', chunk => parts.push(chunk));
      response.on('end', () => resolve({status: response.statusCode, headers: response.headers, body: Buffer.concat(parts), ms: performance.now() - started}));
    });
    req.on('error', reject);
    req.setTimeout(10000, () => req.destroy(new Error(`Timed out: ${path}`)));
  });
}
const home = await request('/');
assert.equal(home.status, 200);
const html = home.body.toString();
const stylesheet = html.match(/href="(\/css\/app.output.css\?v=[A-F0-9]+)"/)[1];
const motion = html.match(/src="(\/js\/nextgen-motion.js\?v=[A-F0-9]+)"/)[1];
const results = [];
for (const path of [stylesheet, motion, '/js/vendor/motion-14.1.0.js?v=14.1.0']) {
  const plain = await request(path, {'Accept-Encoding':'identity'});
  assert.equal(plain.status, 200);
  assert.match(plain.headers['cache-control'], /max-age=31536000,immutable/);
  for (const encoding of ['br', 'gzip']) {
    const compressed = await request(path, {'Accept-Encoding':encoding});
    assert.equal(compressed.status, 200);
    assert.equal(compressed.headers['content-encoding'], encoding);
    assert.match(compressed.headers.vary, /Accept-Encoding/i);
    assert.deepEqual(encoding === 'br' ? brotliDecompressSync(compressed.body) : gunzipSync(compressed.body), plain.body);
    assert.ok(compressed.body.length < plain.body.length * .6, `Insufficient compression: ${path}`);
    results.push({path, encoding, before:plain.body.length, after:compressed.body.length, saving:Math.round((1-compressed.body.length/plain.body.length)*100)});
  }
  const unchanged = await request(path, {'If-None-Match':plain.headers.etag});
  assert.equal(unchanged.status, 304);
  assert.equal(unchanged.body.length, 0);
}
for (const path of ['/', '/login', '/enroll?course=data-science', '/health/ready']) {
  const response = await request(path, {'Accept-Encoding':'br'});
  assert.equal(response.status, 200);
  assert.equal(response.headers['content-encoding'], undefined, `Dynamic content must stay outside asset compression: ${path}`);
  assert.ok(!/public|immutable/.test(response.headers['cache-control'] || ''), `Dynamic response marked public: ${path}`);
}
const image = await request('/images/optimized/colour-pop-ai-480.webp', {'Accept-Encoding':'br'});
assert.equal(image.status, 200);
assert.match(image.headers['cache-control'], /max-age=3600/);
assert.ok(!image.headers['cache-control'].includes('immutable'));
assert.equal(image.headers['content-encoding'], undefined, 'Do not recompress WebP images.');
const versionedImage = await request('/images/optimized/colour-pop-ai-480.webp?v=delivery-check');
assert.match(versionedImage.headers['cache-control'], /max-age=31536000,immutable/);
assert.deepEqual(versionedImage.body, image.body);
console.log(JSON.stringify({passed: true, origin: base.origin, assets: results}, null, 2));
