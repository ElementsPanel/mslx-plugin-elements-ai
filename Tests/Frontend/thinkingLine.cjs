const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const req = createRequire(path.join(root, 'Frontend/package.json'));
const vue = req('vue');
const ts = req('typescript');
const { parse, compileScript } = req('vue/compiler-sfc');
const { renderToString } = req('vue/server-renderer');
function compile(file, inlineTemplate = false) {
  const filename = path.join(root, 'Frontend/src/components', file);
  const { descriptor } = parse(fs.readFileSync(filename, 'utf8'), { filename });
  const compiled = compileScript(descriptor, { id: 'thinking-test', inlineTemplate,
    fs: { fileExists: fs.existsSync, readFile: (file) => fs.readFileSync(file, 'utf8') } });
  return ts.transpileModule(compiled.content, { compilerOptions: {
    module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022,
  } }).outputText;
}

(async () => {
  const mounts = [];
  const unmounts = [];
  const hooks = { ...vue, onMounted: (fn) => mounts.push(fn), onBeforeUnmount: (fn) => unmounts.push(fn) };
  let resize;
  let disconnected = false;
  class Observer {
    constructor(callback) { resize = callback; }
    observe() {}
    disconnect() { disconnected = true; }
  }
  const exports = {};
  new Function('require', 'exports', 'ResizeObserver', compile('ThinkingLine.vue'))(
    (name) => name === 'vue' ? hooks : req(name), exports, Observer,
  );
  const scope = vue.effectScope();
  const props = vue.reactive({ content: '最初的思考', pending: true });
  const line = scope.run(() => exports.default.setup(props, { expose() {} }));
  const element = { scrollLeft: 0, scrollWidth: 800 };
  line.contentElement.value = element;
  mounts.forEach((fn) => fn());
  assert.equal(element.scrollLeft, 800, 'initial thinking does not show its tail');
  element.scrollWidth = 1400;
  props.content += ' 新的思考内容';
  await vue.nextTick();
  assert.equal(element.scrollLeft, 1400, 'streamed reasoning did not scroll to newest text');
  element.scrollWidth = 1600;
  resize();
  assert.equal(element.scrollLeft, 1600, 'resize stopped following newest text');
  unmounts.forEach((fn) => fn());
  scope.stop();
  assert.ok(disconnected, 'resize observer remained attached');
  console.log('PASS thinking follows new text and resizing, and cleans up on unmount');

  const component = {};
  new Function('require', 'exports', compile('ThinkingLine.vue', true))(req, component);
  const html = await renderToString(vue.createSSRApp(component.default, { content: '最新 <script>内容</script>', pending: true }));
  assert.ok(html.includes('正在思考') && html.includes('&lt;script&gt;'));
  assert.ok(!html.includes('<details') && !html.includes('<summary'), 'thinking became expandable');
  console.log('PASS thinking remains a single non-expandable line with escaped text');

  const workspace = {};
  new Function('require', 'exports', 'window', compile('AiWorkspace.vue'))((name) => {
    if (name === 'vue') return hooks;
    if (name === './mslFrpOAuth') return { loginMslFrp() {} };
    if (name === './mslFrpLogin') return { useMslFrpLogin() {} };
    if (name === '../api') return {};
    if (name === 'tdesign-icons-vue-next') return {};
    if (name === 'tdesign-vue-next') return {};
    if (name === '../composables/useTaskProgress') return { useTaskProgress: () => ({ tasks: vue.ref([]), track() {} }) };
    if (name.endsWith('.vue')) return {};
    throw new Error('Unexpected import: ' + name);
  }, workspace, {});
  const workspaceScope = vue.effectScope();
  const chat = workspaceScope.run(() => workspace.default.setup({ compact: false }, { expose() {} }));
  const list = { scrollTop: 0, scrollHeight: 1200 };
  chat.list.value = list;
  chat.applyEvent({ type: 'start', conversationId: 'test', messages: [{ role: 'assistant', content: '', pending: true }] });
  await vue.nextTick();
  list.scrollHeight = 1600;
  chat.applyEvent({ type: 'reasoning', index: 0, content: '最新思考' });
  await vue.nextTick();
  assert.equal(list.scrollTop, 1600, 'chat did not follow the latest reasoning event');
  assert.equal(chat.messages.value[0].reasoning, '最新思考');
  workspaceScope.stop();
  console.log('PASS chat scroll follows streamed reasoning to the latest message');
})().catch((error) => { console.error(error); process.exitCode = 1; });
