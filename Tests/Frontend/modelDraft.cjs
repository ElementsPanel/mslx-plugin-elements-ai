const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const req = createRequire(path.join(root, 'Frontend/package.json'));
const vue = req('vue');
const ts = req('typescript');
const { parse, compileScript } = req('vue/compiler-sfc');
const filename = path.join(root, 'Frontend/src/components/AiWorkspace.vue');
const { descriptor } = parse(fs.readFileSync(filename, 'utf8'), { filename });
const compiled = compileScript(descriptor, { id: 'model-draft-test', fs: {
  fileExists: fs.existsSync, readFile: (file) => fs.readFileSync(file, 'utf8'),
} });
const code = ts.transpileModule(compiled.content, { compilerOptions: {
  module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022,
} }).outputText;
const saves = [];
const errors = [];
const api = {
  saveModel: async (input) => saves.push({ source: 'personal', input: JSON.parse(JSON.stringify(input)) }),
  savePreset: async (input) => saves.push({ source: 'preset', input: JSON.parse(JSON.stringify(input)) }),
  getStatus: async () => ({ ready: false, admin: false, models: [], preferences: { sendOnEnter: true } }),
};
const exports_ = {};
new Function('require', 'exports', 'window', code)((name) => {
  if (name === 'vue') return { ...vue, onMounted() {}, onBeforeUnmount() {} };
  if (name === './mslFrpOAuth') return { loginMslFrp() {} };
    if (name === './mslFrpLogin') return { useMslFrpLogin() {} };
    if (name === '../api') return api;
  if (name === 'tdesign-icons-vue-next') return {};
  if (name === 'tdesign-vue-next') return { DialogPlugin: {}, MessagePlugin: { success() {}, error: (error) => errors.push(error) } };
  if (name === '../composables/useTaskProgress') return { useTaskProgress: () => ({ tasks: vue.ref([]) }) };
  if (name.endsWith('.vue')) return {};
  throw new Error('Unexpected import: ' + name);
}, exports_, {});

const scope = vue.effectScope();
const form = scope.run(() => exports_.default.setup({ compact: false }, { expose() {} }));
const existing = (source) => ({ id: source + ':' + 'a'.repeat(24), name: 'Existing', source,
  endpoint: 'https://example.com/v1', model: 'existing-model', thinkingEnabled: true, thinkingEffort: 'high' });

(async () => {
  for (const previousSource of ['personal', 'preset']) {
    for (const nextSource of ['personal', 'preset']) {
      form.editModel(previousSource, existing(previousSource));
      form.modelDraft.apiKey = 'discarded-test-key';
      form.modelDraft.clearApiKey = true;
      form.modelDialog.value = false;
      form.editModel(nextSource);
      assert.equal(form.modelDraft.id, undefined, 'new model retained the edited model ID');
      assert.equal(form.modelDraft.apiKey, '', 'new model retained a key from the previous form');
      assert.equal(form.modelDraft.clearApiKey, false);
      assert.equal(form.thinkingMode.value, 'default');
      Object.assign(form.modelDraft, { name: 'New model', endpoint: 'https://example.com/v1', model: 'new-model' });
      await form.persistModel();
      const saved = saves.at(-1);
      assert.equal(saved.source, nextSource);
      assert.ok(!('id' in saved.input), 'create payload must not update an existing model');
      assert.equal(form.modelDialog.value, false);
      console.log(`PASS edit ${previousSource}, then add ${nextSource}: creates without a stale ID`);
    }
  }
  form.editModel('personal', existing('personal'));
  form.modelDraft.name = 'Renamed model';
  await form.persistModel();
  assert.equal(saves.at(-1).input.id, 'a'.repeat(24), 'editing no longer targets the original model');
  assert.deepEqual(errors, []);
  console.log('PASS editing an existing model still submits its ID');
  for (const name of ['list_msl_cores', 'list_msl_core_versions', 'list_msl_java_versions'])
    assert.ok(form.toolLabel(name).startsWith('MSL镜像源：'));
  assert.equal(form.toolLabel('wait_for_terminal_update'), '等待终端内容更新');
  assert.equal(form.toolLabel('search_resources'), '搜索资源');
  console.log('PASS tool labels identify MSL queries and terminal waiting');
})().catch((error) => { console.error(error); process.exitCode = 1; }).finally(() => scope.stop());
