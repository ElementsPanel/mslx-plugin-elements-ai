const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const req = createRequire(path.join(root, 'Frontend/package.json'));
const vue = req('vue');
const ts = req('typescript');
const { parse, compileScript } = req('vue/compiler-sfc');
function compile(file, inlineTemplate = false) {
  const filename = path.join(root, 'Frontend/src/components', file);
  const { descriptor } = parse(fs.readFileSync(filename, 'utf8'), { filename });
  const compiled = compileScript(descriptor, { id: 'work-complete-test', inlineTemplate,
    fs: { fileExists: fs.existsSync, readFile: (file) => fs.readFileSync(file, 'utf8') } });
  return ts.transpileModule(compiled.content, { compilerOptions: {
    module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022,
  } }).outputText;
}

(async () => {
  const workspace = {};
  new Function('require', 'exports', 'window', compile('AiWorkspace.vue'))((name) => {
    if (name === 'vue') return vue;
    if (name === './mslFrpOAuth') return { loginMslFrp() {} };
    if (name === './mslFrpLogin') return { useMslFrpLogin() {} };
    if (name === '../api') return {};
    if (name === 'tdesign-icons-vue-next') return {};
    if (name === 'tdesign-vue-next') return {};
    if (name === '../composables/useTaskProgress') return { useTaskProgress: () => ({ tasks: vue.ref([]), track() {} }) };
    if (name.endsWith('.vue')) return {};
    throw new Error('Unexpected import: ' + name);
  }, workspace, {});
  const scope = vue.effectScope();
  const chat = scope.run(() => workspace.default.setup({ compact: false }, { expose() {} }));

  chat.applyEvent({ type: 'start', conversationId: 'test', messages: [{ role: 'assistant', content: '', pending: true }] });
  await vue.nextTick();
  assert.equal(chat.messages.value[0].workComplete, undefined, 'a pending reply claimed to be finished');

  chat.applyEvent({ type: 'message', index: 0, message: { role: 'assistant', content: '已处理完毕', pending: false, workComplete: true } });
  await vue.nextTick();
  assert.equal(chat.messages.value[0].workComplete, true, 'finished reply lost its work-complete marker');
  console.log('PASS finished assistant reply carries the work-complete marker');

  chat.applyEvent({ type: 'message', index: 0, message: { role: 'assistant', content: '', pending: false, workComplete: false } });
  await vue.nextTick();
  assert.equal(chat.messages.value[0].workComplete, false, 'a reply that still has tool calls claimed to be finished');
  console.log('PASS replies that still run tools are not marked as finished');
  scope.stop();

  const source = fs.readFileSync(path.join(root, 'Frontend/src/components/AiWorkspace.vue'), 'utf8');
  assert.ok(source.includes('工作完成'), 'work-complete text is missing from the template');
  assert.ok(/workComplete && !message\.pending/.test(source), 'work-complete marker ignores pending replies');
  assert.ok(!source.includes('work-complete-mark'), 'work-complete marker still renders an icon');
  console.log('PASS workspace template renders the work-complete marker only for finished replies');

  const dialog = fs.readFileSync(path.join(root, 'Frontend/src/views/InstanceAiDialog.vue'), 'utf8');
  assert.ok(dialog.includes('CloseIcon'), 'drawer close button icon is not imported');
  assert.ok(/aria-label="关闭侧边栏"[\s\S]*?@click="visible = false"/.test(dialog), 'close button does not dismiss the drawer');
  const actions = dialog.slice(dialog.indexOf('drawer-header-actions'));
  const closeAt = actions.indexOf('drawer-header-close');
  const settingsAt = actions.indexOf('SettingIcon');
  assert.ok(closeAt > settingsAt, 'close button must sit to the right of the existing header actions');
  console.log('PASS drawer header closes the sidebar from the right of its action row');

  const closeRule = dialog.slice(dialog.indexOf('.drawer-header-close {'));
  const closeBlock = closeRule.slice(0, closeRule.indexOf('}'));
  assert.ok(!/padding-left|border-left/.test(closeBlock), 'close button padding shifts the icon off centre');
  assert.ok(/\.drawer-header-close::before/.test(dialog), 'close button lost its outside separator');
  console.log('PASS close button keeps its square box so the icon stays centred on hover');
})().catch((error) => { console.error(error); process.exitCode = 1; });
