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
class InteractionUnavailableError extends Error { }
function setup(api, storage = memoryStorage(), user = vue.reactive({ token: 'a' })) {
  const exports = {};
  const errors = [];
  new Function('require', 'exports', 'window', code)((name) => {
    if (name === 'vue') return { ...vue, onMounted() {}, onBeforeUnmount() {} };
    if (name === '../api') return Object.assign(api, { InteractionUnavailableError });
    if (name === 'tdesign-vue-next') return { MessagePlugin: { error: (message) => errors.push(message) } };
    if (name === 'tdesign-icons-vue-next') return {};
    if (name === '../composables/useTaskProgress') return { useTaskProgress: () => ({ tasks: vue.ref([]), track() {}, reset() {}, beginTurn() {}, restore() {} }) };
    if (name.endsWith('.vue')) return {};
    throw new Error('Unexpected import: ' + name);
  }, exports, { localStorage: storage, MSLX_Stores: { getUserStore: () => user } });
  const scope = vue.effectScope();
  return { chat: scope.run(() => exports.default.setup({ compact: false }, { expose() {} })), close: () => scope.stop(), user, errors };
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

  const confirmation = (id) => ({ role: 'tool', tool: 'edit_file', content: '', pending: true, approval: { id, arguments: '{}' } });
  const stateApi = {
    getInteractionStatus: async () => ({ active: true, approvalIds: ['live'], questionIds: [] }),
    getConversation: async () => ({ id: 'conversation', messages: [{ role: 'assistant', content: '已完成' }] }),
  };
  const reopened = setup(stateApi);
  const live = reopened.chat;
  live.conversationId.value = 'conversation';
  live.loading.value = true;
  live.controller.value = new AbortController();
  live.messages.value = [confirmation('live')];
  await live.syncInteractions();
  assert.equal(live.messages.value[0].approval.id, 'live', 'reopening expired a live approval');
  assert.equal(live.controller.value.signal.aborted, false);
  let submissions = 0;
  stateApi.respondToApproval = async () => { submissions++; return true; };
  await live.decideApproval(live.messages.value[0], true);
  await live.decideApproval(live.messages.value[0], true);
  assert.equal(submissions, 1, 'successful approval could be resubmitted');
  assert.equal(live.loading.value, true, 'approval stopped the running turn');
  const oldController = live.controller.value;
  live.messages.value = [confirmation('expired')];
  stateApi.getInteractionStatus = async () => ({ active: false, approvalIds: [], questionIds: [] });
  await live.syncInteractions();
  assert.equal(oldController.signal.aborted, true);
  assert.equal(live.loading.value, false);
  assert.equal(live.messages.value[0].content, '已完成');
  assert.ok(live.messages.value.every((message) => !message.approval && !message.question));
  reopened.close();
  console.log('PASS reopening keeps live confirmations, consumes accepted buttons, and recovers an ended stream');

  const failedApi = { getInteractionStatus: async () => { throw new Error('network'); } };
  const failed = setup(failedApi);
  failed.chat.loading.value = true;
  failed.chat.messages.value = [confirmation('live')];
  await failed.chat.syncInteractions();
  assert.equal(failed.chat.messages.value[0].approval.id, 'live', 'network error discarded a valid confirmation');
  failedApi.respondToApproval = async () => { throw new Error('network'); };
  await failed.chat.decideApproval(failed.chat.messages.value[0], true);
  assert.equal(failed.chat.messages.value[0].approval.id, 'live');
  assert.deepEqual(failed.errors, ['network']);
  failed.close();
  console.log('PASS transient interaction failures preserve the confirmation for retry');

  let finishSync;
  const race = setup({ getInteractionStatus: () => new Promise((resolve) => { finishSync = resolve; }) });
  race.chat.loading.value = true;
  race.chat.messages.value = [confirmation('old')];
  const syncing = race.chat.syncInteractions();
  race.chat.applyEvent({ type: 'message', index: 0, message: confirmation('new') });
  finishSync({ active: false, approvalIds: [], questionIds: [] });
  await syncing;
  assert.equal(race.chat.messages.value[0].approval.id, 'new', 'late snapshot invalidated a newer SSE confirmation');
  assert.equal(race.chat.loading.value, true);
  const abandoned = race.chat.syncInteractions();
  race.chat.newChat();
  race.chat.messages.value = [{ role: 'user', content: 'new chat' }];
  finishSync({ active: false, approvalIds: [], questionIds: [] });
  await abandoned;
  assert.deepEqual(race.chat.messages.value, [{ role: 'user', content: 'new chat' }]);
  race.close();
  console.log('PASS stale synchronization cannot overwrite a new event or conversation');

  const expired = setup({
    respondToApproval: async () => { throw new InteractionUnavailableError('gone'); },
    respondToQuestion: async () => { throw new InteractionUnavailableError('gone'); },
    getInteractionStatus: async () => ({ active: false, approvalIds: [], questionIds: [] }),
  });
  expired.chat.loading.value = true;
  expired.chat.messages.value = [confirmation('gone')];
  await expired.chat.decideApproval(expired.chat.messages.value[0], true);
  assert.equal(expired.chat.messages.value[0].approval, undefined);
  assert.equal(expired.chat.loading.value, false);
  expired.chat.loading.value = true;
  expired.chat.messages.value = [{ role: 'tool', content: '', pending: true, question: { id: 'gone-q', question: '?', options: ['a', 'b'] } }];
  await expired.chat.answerQuestion(expired.chat.messages.value[0], 'a');
  assert.equal(expired.chat.messages.value[0].question, undefined);
  assert.equal(expired.chat.loading.value, false);
  assert.deepEqual(expired.errors, [], 'stale controls repeatedly displayed an error toast');
  expired.chat.messages.value = [{ ...confirmation('stale'), pending: false }];
  expired.chat.stop();
  assert.equal(expired.chat.messages.value[0].approval, undefined, 'stale non-pending approval remained clickable');
  expired.close();
  console.log('PASS expired approval/question controls are removed and stopped history cannot retain buttons');
})().catch((error) => { console.error(error); process.exitCode = 1; });
