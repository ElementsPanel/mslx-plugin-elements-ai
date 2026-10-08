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
const api = {};
let result;
let captured;
new Function('require', 'exports', 'window', 'fetch', code)(
  () => ({}), api, { MSLX_Stores: { getUserStore: () => ({ token: 'panel-token' }) } },
  async (url, init) => { captured = { url, init }; return result; },
);
const response = (status, data, code = status) => new Response(JSON.stringify({ code, data, message: 'response' }), {
  status, headers: { 'content-type': 'application/json' },
});

(async () => {
  result = response(200, true);
  assert.equal(await api.respondToApproval('approval', true), true);
  assert.equal(captured.url, '/api/plugin/mslx-plugin-elements-ai/ai/approvals/approval');
  assert.equal(captured.init.headers['x-user-token'], 'panel-token');
  assert.deepEqual(JSON.parse(captured.init.body), { approved: true });
  result = response(404, false);
  await assert.rejects(api.respondToApproval('gone', true), api.InteractionUnavailableError);
  result = response(410, false);
  await assert.rejects(api.respondToQuestion('gone', 'answer'), api.InteractionUnavailableError);
  for (const status of [403, 500]) {
    result = response(status, false);
    await assert.rejects(api.respondToApproval('live', true), (error) => !(error instanceof api.InteractionUnavailableError));
  }
  const state = { active: false, approvalIds: [], questionIds: [] };
  result = response(200, state);
  assert.deepEqual(await api.getInteractionStatus(), state);
  assert.equal(captured.init.cache, 'no-store');
  result = response(200, {});
  await assert.rejects(api.getInteractionStatus());
  console.log('PASS interaction transport distinguishes expired confirmations from permission/network failures and never caches live status');
})().catch((error) => { console.error(error); process.exitCode = 1; });
