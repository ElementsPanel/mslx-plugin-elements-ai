const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');
const root = path.resolve(__dirname, '../..');
const req = createRequire(path.join(root, 'Frontend/package.json'));
const vue = req('vue'), ts = req('typescript');
const code = ts.transpileModule(fs.readFileSync(path.join(root, 'Frontend/src/components/mslFrpLogin.ts'), 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText;
const moduleExports = {};
let cleanup, timer, token = 'expired', reads = 0;
new Function('require', 'exports', 'localStorage', 'setInterval', 'clearInterval', code)(
  () => ({ ...vue, onBeforeUnmount: fn => { cleanup = fn; } }), moduleExports,
  { getItem: () => { reads++; return token; } }, fn => { timer = fn; return 1; }, () => { timer = undefined; },
);
const messages = vue.ref([]), answers = [];
moduleExports.useMslFrpLogin(messages, async message => { answers.push(message.question.id); });
messages.value.push({ role: 'tool', question: { id: 'login', kind: 'mslfrp_login', options: [] } });
assert.equal(typeof timer, 'function');
timer(); assert.deepEqual(answers, [], 'expired token must not immediately retry');
token = 'fresh'; timer(); timer(); assert.deepEqual(answers, ['login'], 'new login resumes once');
messages.value[0].question = undefined;
assert.equal(timer, undefined, 'completed wait must stop local checks');
messages.value.push({ role: 'tool', question: { id: 'cancel', kind: 'mslfrp_login', options: [] } });
messages.value = []; assert.equal(timer, undefined, 'stop/reset must stop local checks');
cleanup();
assert.ok(reads > 0);
console.log('PASS browser login resumes once on token changes and cleans up after completion/cancellation');
