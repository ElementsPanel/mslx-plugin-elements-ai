const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const req = createRequire(path.join(root, 'Frontend/package.json'));
const ts = req('typescript');
const code = ts.transpileModule(fs.readFileSync(path.join(root, 'Frontend/src/api.ts'), 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText;

async function run(isAdmin, mslToken, storageBlocked = false) {
  const calls = [];
  const events = [];
  const exports_ = {};
  const storage = { getItem(key) {
    if (storageBlocked) throw new Error('Storage unavailable');
    return key === 'msl-user-token' ? mslToken : key === 'ACTIVE_NODE_ID' ? 'worker-1' : null;
  } };
  const store = { token: 'panel-secret', isAdmin };
  new Function('require', 'exports', 'window', 'localStorage', 'fetch', code)(
    (name) => { assert.equal(name, 'mslx-request'); return {}; },
    exports_, { MSLX_Stores: { getUserStore: () => store } }, storage,
    async (url, init) => {
      calls.push({ url, init });
      return new Response('data: {"type":"done"}\n\n', { headers: { 'content-type': 'text/event-stream' } });
    },
  );
  await exports_.sendMessage('create a tunnel', undefined, 'model', 'default', undefined,
    new AbortController().signal, (event) => events.push(event));
  assert.equal(calls.length, 1);
  const { url, init } = calls[0];
  assert.equal(url, '/api/plugins/mslx-plugin-elements-ai/ai/chat');
  assert.equal(init.headers['x-user-token'], 'panel-secret');
  assert.equal(init.headers['x-mslfrp-token'], isAdmin && !storageBlocked && mslToken ? mslToken : undefined);
  assert.equal(JSON.parse(init.body).currentNodeId, storageBlocked ? 'local' : 'worker-1');
  assert.ok(!init.body.includes('secret'), 'credentials must not enter the chat/model payload');
  assert.deepEqual(events, [{ type: 'done' }]);
}

(async () => {
  await run(true, 'msl-secret');
  await run(false, 'msl-secret');
  await run(true, null);
  await run(true, 'msl-secret', true);
  console.log('PASS MSL login is passed only in admin chat headers, never in model payloads; missing/blocked storage still permits chat');
})().catch((error) => { console.error(error); process.exitCode = 1; });
