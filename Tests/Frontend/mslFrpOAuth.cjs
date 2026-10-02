const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const req = createRequire(path.resolve(__dirname, '../../Frontend/package.json'));
const code = req('typescript').transpileModule(fs.readFileSync(path.resolve(__dirname, '../../Frontend/src/components/mslFrpOAuth.ts'), 'utf8'), {
  compilerOptions: { module: req('typescript').ModuleKind.CommonJS, target: req('typescript').ScriptTarget.ES2022 },
}).outputText;
function setup(mode) {
  const exports = {}, calls = [], saved = [], timers = new Set();
  const popup = { closed: false, location: {}, focus() {}, close() { this.closed = true; } };
  new Function('exports', 'window', 'crypto', 'localStorage', 'fetch', 'setTimeout', 'clearTimeout', 'setInterval', 'clearInterval', code)(
    exports, { open: () => mode === 'blocked' ? null : popup }, require('node:crypto').webcrypto,
    { setItem: (...args) => saved.push(args) }, async (url, options) => {
      calls.push({ url, options }); options.signal.throwIfAborted();
      return { ok: true, json: async () => calls.length === 1
        ? { data: { ssid: 'session', url: mode === 'badOrigin' ? 'https://bad.test/login' : 'https://user.mslmc.net/oauth/login' } }
        : { data: { token: 'fresh-token' } } };
    }, fn => setTimeout(fn, 0), clearTimeout, fn => { timers.add(fn); return fn; }, fn => timers.delete(fn),
  );
  return { ...exports, calls, saved, popup, timers };
}
(async () => {
  const ok = setup(); await ok.loginMslFrp(new AbortController().signal);
  assert.deepEqual(ok.saved, [['msl-user-token', 'fresh-token']]);
  assert.equal(ok.calls.length, 2); assert.equal(ok.popup.closed, true); assert.equal(ok.timers.size, 0);
  for (const { options } of ok.calls) {
    assert.equal(options.credentials, 'omit');
    assert.equal(options.headers['x-user-token'], undefined);
    assert.equal(options.headers.Authorization, undefined);
  }
  const blocked = setup('blocked'); await assert.rejects(blocked.loginMslFrp(new AbortController().signal), /弹出窗口/); assert.equal(blocked.calls.length, 0);
  const bad = setup('badOrigin'); await assert.rejects(bad.loginMslFrp(new AbortController().signal), /地址无效/); assert.equal(bad.saved.length, 0); assert.equal(bad.popup.closed, true);
  const canceled = setup(); const cancel = new AbortController(); cancel.abort();
  await assert.rejects(canceled.loginMslFrp(cancel.signal), { name: 'AbortError' }); assert.equal(canceled.saved.length, 0); assert.equal(canceled.timers.size, 0);
  console.log('PASS panel OAuth flow stores browser login only, handles popup blocking, validates origin and cancels cleanly');
})().catch(error => { console.error(error); process.exitCode = 1; });
