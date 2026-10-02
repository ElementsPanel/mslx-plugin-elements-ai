const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const req = createRequire(path.join(root, 'Frontend/package.json'));
const ts = req('typescript');
const code = ts.transpileModule(fs.readFileSync(path.join(root, 'Frontend/src/pluginEntry.ts'), 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText;
const runtimeKey = '__MSLX_ELEMENTS_AI_RUNTIME__';
const rootId = 'mslx-elements-ai-global-root-0-1-0';
const roots = new Map();
const Sidebar = {};
let mounts = 0;
let unmounts = 0;
let legacyUnmounts = 0;
const document = {
  readyState: 'complete',
  getElementById: (id) => roots.get(id),
  querySelectorAll: (selector) => selector.startsWith('[id^=') ? [...roots.values()] : [],
  createElement: () => { const element = { id: '', remove: () => roots.delete(element.id) }; return element; },
  body: { appendChild: (element) => roots.set(element.id, element) },
};
const legacyRoot = document.createElement();
legacyRoot.id = rootId;
document.body.appendChild(legacyRoot);
const window = { [runtimeKey]: { version: '0.1.0', unmount: () => legacyUnmounts++ } };

function load(buildId) {
  const queued = [];
  const exports_ = {};
  new Function('require', 'exports', 'window', 'document', 'queueMicrotask', '__ELEMENTS_AI_BUILD_ID__', code)(
    (name) => {
      if (name === 'vue') return { createApp(component) {
        assert.equal(component, Sidebar, 'plugin entry must mount the sidebar');
        return { use() {}, mount() { mounts++; }, unmount() { unmounts++; } };
      } };
      if (name === 'tdesign-vue-next') return { default: {} };
      if (name === './views/InstanceAiDialog.vue') return { __esModule: true, default: Sidebar };
      throw new Error('Unexpected import: ' + name);
    }, exports_, window, document, (action) => queued.push(action), buildId,
  );
  queued.forEach((action) => action());
  assert.equal(exports_.pluginConfig.version, '0.1.0');
}

load('build-a');
assert.equal(legacyUnmounts, 1, 'old same-version runtime was retained');
assert.equal(mounts, 1);
load('build-a');
assert.equal(mounts, 1, 'same build mounted twice');
load('build-b');
assert.equal(mounts, 2, 'new same-version build did not replace old runtime');
assert.equal(unmounts, 1);
assert.equal(window[runtimeKey].buildId, 'build-b');
assert.equal(roots.size, 1);
roots.get(rootId).remove();
load('build-b');
assert.equal(mounts, 3, 'missing sidebar mount was not restored');
console.log('PASS plugin entry mounts the sidebar, replaces older same-version builds, and avoids duplicate launchers');
