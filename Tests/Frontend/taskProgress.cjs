const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const req = createRequire(path.join(root, 'Frontend/package.json'));
const ts = req('typescript');
const vue = req('vue');
const { parse, compileScript } = req('vue/compiler-sfc');
const { renderToString } = req('vue/server-renderer');
const source = fs.readFileSync(path.join(root, 'Frontend/src/composables/useTaskProgress.ts'), 'utf8');
const transpile = (text) => ts.transpileModule(text, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;

function harness(read) {
  let now = 0;
  let next = 0;
  const timers = new Map();
  const setTimer = (callback, delay) => { const id = ++next; timers.set(id, { callback, at: now + delay }); return id; };
  const exports = {};
  new Function('require', 'exports', 'setTimeout', 'clearTimeout', transpile(source))(
    (name) => name === '../api' ? {} : req(name), exports, setTimer, (id) => timers.delete(id),
  );
  const tracker = exports.useTaskProgress(read);
  return {
    tracker,
    async advance(ms = 2000) {
      now += ms;
      for (const [id, timer] of [...timers]) if (timer.at <= now) { timers.delete(id); timer.callback(); }
      for (let i = 0; i < 20; i++) await Promise.resolve();
    },
  };
}
const task = (patch = {}) => ({ taskId: 'a'.repeat(32), title: '安装 Java', state: 'running', value: 20, message: '正在下载', completed: false, success: false, ...patch });

(async () => {
  let calls = 0;
  const background = harness(async () => { calls++; return task({ value: calls === 1 ? 70 : 100, state: calls === 1 ? 'running' : 'success', completed: calls > 1, success: calls > 1 }); });
  background.tracker.track(task());
  background.tracker.beginTurn();
  assert.equal(background.tracker.tasks.value.length, 1, 'new message discarded running task');
  await background.advance();
  assert.equal(background.tracker.tasks.value[0].value, 70, 'background progress did not update');
  await background.advance();
  assert.equal(background.tracker.tasks.value[0].state, 'success');
  await background.advance();
  assert.equal(calls, 2, 'terminal task still polled');
  background.tracker.beginTurn();
  assert.equal(background.tracker.tasks.value.length, 0, 'completed task was not cleared on next message');
  background.tracker.reset();
  console.log('PASS background progress persists across messages and stops after completion');

  let resolve;
  const stale = harness(() => new Promise((done) => { resolve = done; }));
  stale.tracker.track(task());
  await stale.advance();
  stale.tracker.track(task({ state: 'success', value: 100, completed: true, success: true }));
  resolve(task({ value: 30 }));
  await stale.advance(0);
  assert.equal(stale.tracker.tasks.value[0].state, 'success', 'stale polling overwrote terminal SSE event');
  stale.tracker.reset();
  stale.tracker.track(task());
  await stale.advance();
  stale.tracker.reset();
  resolve(task({ value: 80 }));
  await stale.advance(0);
  assert.equal(stale.tracker.tasks.value.length, 0, 'request from previous account/conversation leaked after reset');
  console.log('PASS late poll responses cannot overwrite stream completion or repopulate reset views');

  let failures = 0;
  const unavailable = harness(async () => { failures++; throw new Error('expired'); });
  unavailable.tracker.restore([{ role: 'tool', content: '', taskProgress: task() }]);
  await unavailable.advance(); await unavailable.advance(); await unavailable.advance();
  assert.equal(unavailable.tracker.tasks.value[0].unavailable, true);
  assert.equal(unavailable.tracker.tasks.value[0].completed, false, 'poll failure faked completion');
  await unavailable.advance();
  assert.equal(failures, 3, 'unavailable task retried without bounds');
  unavailable.tracker.beginTurn();
  await unavailable.advance();
  assert.equal(failures, 4, 'next message could not resume temporarily unavailable task');
  unavailable.tracker.reset();
  console.log('PASS history restores tracking and polling errors preserve unknown status with bounded retries');

  const filename = path.join(root, 'Frontend/src/components/TaskProgressList.vue');
  const { descriptor } = parse(fs.readFileSync(filename, 'utf8'), { filename });
  const compiled = compileScript(descriptor, { id: 'progress-test', inlineTemplate: true, fs: { fileExists: fs.existsSync, readFile: (p) => fs.readFileSync(p, 'utf8') } });
  const exports = {};
  new Function('require', 'exports', transpile(compiled.content))(req, exports);
  const html = await renderToString(vue.createSSRApp(exports.default, { tasks: [
    task({ title: '<script>unsafe</script>', speed: '2 MB/s' }),
    task({ taskId: 'b', state: 'failed', completed: true, message: '安装失败' }),
    task({ taskId: 'c', unavailable: true }),
  ] }));
  assert.match(html, /aria-valuenow="20"/);
  assert.match(html, /2 MB\/s/);
  assert.match(html, /安装失败/);
  assert.match(html, /状态暂不可用/);
  assert.ok(html.includes('&lt;script&gt;unsafe&lt;/script&gt;') && !html.includes('<script>unsafe</script>'));
  console.log('PASS task progress renders percentages, speed, failures and escaped task text');
})().catch((error) => { console.error(error); process.exitCode = 1; });
