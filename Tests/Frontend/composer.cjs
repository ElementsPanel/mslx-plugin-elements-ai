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
const compiled = compileScript(descriptor, { id: 'composer-test', fs: {
  fileExists: fs.existsSync, readFile: (file) => fs.readFileSync(file, 'utf8'),
} });
const code = ts.transpileModule(compiled.content, { compilerOptions: {
  module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022,
} }).outputText;
const choices = [{ id: 'personal:first' }, { id: 'preset:second' }];
const status = (userId, models = choices) => ({ userId, ready: true, admin: false, models, preferences: { sendOnEnter: true } });
const flush = async () => { for (let i = 0; i < 8; i++) await vue.nextTick(); };
function memoryStorage() {
  const data = new Map();
  return { getItem: (key) => data.get(key) ?? null, setItem: (key, value) => data.set(key, value) };
}
function setup(api, storage = memoryStorage(), user = vue.reactive({ token: 'a' })) {
  const exports = {};
  new Function('require', 'exports', 'window', code)((name) => {
    if (name === 'vue') return { ...vue, onMounted() {}, onBeforeUnmount() {} };
    if (name === '../api') return api;
    if (name === 'tdesign-vue-next' || name === 'tdesign-icons-vue-next') return {};
    if (name === '../composables/useTaskProgress') return { useTaskProgress: () => ({ tasks: vue.ref([]), track() {}, reset() {}, beginTurn() {} }) };
    if (name.endsWith('.vue')) return {};
    throw new Error('Unexpected import: ' + name);
  }, exports, { localStorage: storage, MSLX_Stores: { getUserStore: () => user } });
  const scope = vue.effectScope();
  return { chat: scope.run(() => exports.default.setup({ compact: false }, { expose() {} })), close: () => scope.stop(), user };
}

(async () => {
  const storage = memoryStorage();
  const api = { getStatus: async () => status('alice') };
  const first = setup(api, storage);
  await first.chat.refreshStatus();
  first.chat.selectedModel.value = 'preset:second';
  first.chat.permissionMode.value = 'full';
  await flush();
  first.chat.newChat();
  assert.equal(first.chat.selectedModel.value, 'preset:second');
  first.close();
  const restored = setup(api, storage);
  await restored.chat.refreshStatus();
  assert.equal(restored.chat.selectedModel.value, 'preset:second');
  assert.equal(restored.chat.permissionMode.value, 'full');
  api.getStatus = async () => status('bob');
  restored.user.token = 'b';
  await flush();
  assert.equal(restored.chat.status.value.userId, 'bob');
  assert.equal(restored.chat.permissionMode.value, 'default', 'full permission mode leaked to another account');
  assert.equal(restored.chat.selectedModel.value, 'personal:first');
  restored.chat.permissionMode.value = 'full';
  await flush();
  api.getStatus = async () => status('alice', [choices[0]]);
  restored.user.token = 'a';
  await flush();
  assert.equal(restored.chat.selectedModel.value, 'personal:first', 'removed model remained selected');
  assert.equal(restored.chat.permissionMode.value, 'full');
  restored.close();
  console.log('PASS selections survive remount and new chats, remain per account, and handle removed models');

  storage.setItem('mslx-elements-ai:selection:alice', '{broken');
  const malformed = setup({ getStatus: async () => status('alice') }, storage);
  await malformed.chat.refreshStatus();
  assert.equal(malformed.chat.permissionMode.value, 'default');
  assert.equal(malformed.chat.error.value, '');
  malformed.close();
  const blocked = setup({ getStatus: async () => status('alice') }, {
    getItem() { throw new Error('blocked'); }, setItem() { throw new Error('quota'); },
  });
  await blocked.chat.refreshStatus();
  blocked.chat.permissionMode.value = 'full';
  await flush();
  assert.equal(blocked.chat.error.value, '');
  blocked.close();
  console.log('PASS invalid or unavailable localStorage does not break model selection');

  let resolveOldStatus;
  const staleApi = { getStatus: () => new Promise((resolve) => { resolveOldStatus = resolve; }) };
  const stale = setup(staleApi);
  const oldStatus = stale.chat.refreshStatus();
  staleApi.getStatus = async () => status('bob');
  stale.user.token = 'b';
  await flush();
  resolveOldStatus(status('alice'));
  await oldStatus;
  assert.equal(stale.chat.status.value.userId, 'bob', 'late status response restored the previous account');
  stale.close();
  console.log('PASS late status responses cannot overwrite a newly logged-in account');

  const requests = [];
  const streaming = setup({
    getStatus: async () => status('alice'),
    sendMessage: (...args) => new Promise((resolve, reject) => {
      const signal = args[5];
      const emit = args[6];
      requests.push({ signal, emit, resolve, reject });
      emit({ type: 'start', conversationId: 'test', messages: [{ role: 'assistant', content: '', pending: true }] });
    }),
  });
  const chat = streaming.chat;
  await chat.refreshStatus();
  chat.draft.value = 'first';
  const firstSend = chat.send();
  assert.equal(chat.messages.value[0].pending, true);
  chat.messages.value.push({ role: 'tool', content: '', pending: true, approval: { id: 'approval', arguments: '{}' } });
  chat.stop();
  assert.ok(requests[0].signal.aborted);
  assert.equal(chat.loading.value, false);
  assert.ok(chat.messages.value.every((message) => !message.pending), 'working indicator remained pending');
  assert.equal(chat.messages.value[1].approval, undefined);
  requests[0].emit({ type: 'message', index: 0, message: { role: 'assistant', content: 'late', pending: true } });
  assert.equal(chat.messages.value[0].content, '', 'late stream event resurrected stopped work');
  chat.draft.value = 'second';
  const secondSend = chat.send();
  requests[0].reject(new Error('aborted'));
  await firstSend;
  assert.equal(chat.loading.value, true, 'old request cleanup stopped a newer request');
  assert.equal(chat.messages.value[0].pending, true);
  requests[1].reject(new Error('stream disconnected'));
  await secondSend;
  assert.equal(chat.loading.value, false);
  assert.ok(chat.messages.value.every((message) => !message.pending), 'disconnected stream left working indicator behind');
  streaming.close();
  console.log('PASS stopping clears working indicators immediately, ignores late events and preserves newer requests');
})().catch((error) => { console.error(error); process.exitCode = 1; });
