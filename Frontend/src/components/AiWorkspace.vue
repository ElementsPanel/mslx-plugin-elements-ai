<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue';
import { DialogPlugin, MessagePlugin } from 'tdesign-vue-next';
import { loginMslFrp } from './mslFrpOAuth';
import { useMslFrpLogin } from './mslFrpLogin';
import { SendIcon, StopIcon } from 'tdesign-icons-vue-next';
import {
  deleteConversations,
  deleteModel,
  deletePreset,
  getConversation,
  getInteractionStatus,
  InteractionUnavailableError,
  getStatus,
  listConversations,
  listPresets,
  respondToApproval,
  respondToQuestion,
  saveModel,
  savePreferences,
  savePreset,
  sendMessage,
} from '../api';
import type {
  AiStatus,
  ChatEvent,
  ChatMessage,
  ConversationSummary,
  ModelInput,
  ModelOption,
  PermissionMode,
} from '../types/ai';
import FileDiffView from './FileDiff.vue';
import MarkdownMessage from './MarkdownMessage.vue';
import ToolReceipt from './ToolReceipt.vue';
import ThinkingLine from './ThinkingLine.vue';
import TaskProgressList from './TaskProgressList.vue';
import { useTaskProgress } from '../composables/useTaskProgress';

const props = withDefaults(defineProps<{ currentInstanceId?: number; compact?: boolean }>(), {
  compact: false,
});

const hostStores = (window as any).MSLX_Stores;
const hostUserStore = hostStores?.getUserStore?.() || hostStores?.useUserStore?.();
const authToken = computed(() => hostUserStore?.token || '');
const status = ref<AiStatus>();
const selectedModel = ref('');
const permissionMode = ref<PermissionMode>('default');
let selectionUserId = '';
let selectionToken = '';
let statusSequence = 0;
let interactionSequence = 0;
let eventRevision = 0;
const messages = ref<ChatMessage[]>([]);
const conversationId = ref<string>();
const draft = ref('');
const loading = ref(false);
const checking = ref(false);
const error = ref('');
const retryText = ref('');
const taskProgress = useTaskProgress();
const activeTasks = taskProgress.tasks;
const view = ref<'chat' | 'history' | 'settings'>('chat');
const list = ref<HTMLElement>();
const controller = ref<AbortController>();
const approvalSubmitting = ref('');
const questionSubmitting = ref('');
const questionAnswers = reactive<Record<string, string>>({});

const histories = ref<ConversationSummary[]>([]);
const historyLoading = ref(false);
const selectedHistory = ref<string[]>([]);

const presets = ref<ModelOption[]>([]);
const settingsLoading = ref(false);
const savingPreferences = ref(false);
const sendOnEnter = ref(true);
const modelDialog = ref(false);
const modelSource = ref<'personal' | 'preset'>('personal');
const thinkingMode = ref<'default' | 'on' | 'off'>('default');
const modelDraft = reactive<ModelInput>(emptyModel());
const modelSaving = ref(false);

const models = computed(() => status.value?.models || []);
const personalModels = computed(() => models.value.filter((model) => model.source === 'personal'));
const canSend = computed(() =>
  Boolean(status.value?.ready && selectedModel.value && draft.value.trim() && !loading.value),
);
const currentInstanceLabel = computed(() => props.currentInstanceId ? `实例 #${props.currentInstanceId}` : '全局助手');
const modelValid = computed(() =>
  Boolean(modelDraft.name.trim() && modelDraft.endpoint.trim() && modelDraft.model.trim()),
);

function emptyModel(): ModelInput {
  return {
    id: undefined,
    name: '', endpoint: '', model: '', apiKey: '', clearApiKey: false,
    thinkingEnabled: null, thinkingEffort: 'medium',
  };
}

function reset(clearDraft = false) {
  interactionSequence++;
  controller.value?.abort();
  controller.value = undefined;
  messages.value = [];
  conversationId.value = undefined;
  loading.value = false;
  error.value = '';
  retryText.value = '';
  taskProgress.reset();
  approvalSubmitting.value = '';
  questionSubmitting.value = '';
  if (clearDraft) draft.value = '';
}

function newChat() {
  reset(true);
  view.value = 'chat';
}

async function refreshStatus() {
  const sequence = ++statusSequence;
  const requestToken = authToken.value;
  checking.value = true;
  error.value = '';
  try {
    const next = await getStatus();
    if (sequence !== statusSequence || requestToken !== authToken.value) return;
    status.value = next;
    sendOnEnter.value = next.preferences?.sendOnEnter ?? true;
    if (selectionUserId !== next.userId || selectionToken !== requestToken) {
      let saved: { modelId?: unknown; permissionMode?: unknown } | null = null;
      try { saved = JSON.parse(window.localStorage.getItem(selectionKey(next.userId)) || 'null'); } catch { }
      selectedModel.value = typeof saved?.modelId === 'string' ? saved.modelId : '';
      permissionMode.value = saved?.permissionMode === 'full' ? 'full' : 'default';
      selectionUserId = next.userId;
      selectionToken = requestToken;
    }
    if (!next.models.some((model) => model.id === selectedModel.value))
      selectedModel.value = next.models[0]?.id || '';
    rememberSelection();
  } catch (err) {
    if (sequence === statusSequence && requestToken === authToken.value) error.value = messageOf(err);
  } finally {
    if (sequence === statusSequence) checking.value = false;
  }
}

function selectionKey(userId: string) {
  return `mslx-elements-ai:selection:${encodeURIComponent(userId)}`;
}

function rememberSelection() {
  if (!selectionUserId || status.value?.userId !== selectionUserId || selectionToken !== authToken.value) return;
  try {
    window.localStorage.setItem(selectionKey(selectionUserId), JSON.stringify({
      modelId: selectedModel.value, permissionMode: permissionMode.value,
    }));
  } catch { /* Browser storage restrictions must not prevent chatting. */ }
}

watch([selectedModel, permissionMode], rememberSelection);

async function scrollToEnd() {
  await nextTick();
  if (list.value) list.value.scrollTop = list.value.scrollHeight;
}

function applyEvent(event: ChatEvent) {
  eventRevision++;
  if (event.type === 'start') {
    conversationId.value = event.conversationId;
    messages.value = event.messages;
  } else if (event.type === 'message') {
    while (messages.value.length <= event.index)
      messages.value.push({ role: 'assistant', content: '', pending: true });
    messages.value[event.index] = event.message;
    taskProgress.track(event.message.taskProgress);
  } else if (event.type === 'delta') {
    const message = messages.value[event.index];
    if (message) message.content += event.content;
  } else if (event.type === 'reasoning') {
    const message = messages.value[event.index];
    if (message) message.reasoning = (message.reasoning || '') + event.content;
  } else if (event.type === 'retry') {
    retryText.value = `模型连接失败，正在进行第 ${event.attempt}/${event.maxAttempts} 次重试…`;
  } else if (event.type === 'progress') {
    taskProgress.track(event.progress);
  } else if (event.type === 'done') {
    conversationId.value = event.conversationId;
    retryText.value = '';
  }
  void scrollToEnd();
}

async function send() {
  if (!canSend.value) return;
  interactionSequence++;
  const text = draft.value.trim();
  draft.value = '';
  loading.value = true;
  error.value = '';
  retryText.value = '';
  taskProgress.beginTurn();
  const active = new AbortController();
  controller.value = active;
  try {
    await sendMessage(
      text,
      conversationId.value,
      selectedModel.value,
      permissionMode.value,
      props.currentInstanceId,
      active.signal,
      (event) => { if (controller.value === active && !active.signal.aborted) applyEvent(event); },
    );
  } catch (err) {
    if (controller.value === active && !active.signal.aborted) {
      error.value = messageOf(err);
      messages.value.push({ role: 'error', content: error.value });
    }
  } finally {
    if (controller.value === active) {
      clearPendingMessages();
      controller.value = undefined;
      loading.value = false;
      retryText.value = '';
      await scrollToEnd();
    }
  }
}

function clearPendingMessages() {
  for (const message of messages.value) {
    message.approval = undefined;
    message.question = undefined;
    if (!message.pending) continue;
    message.pending = false;
    if (message.role === 'tool') {
      message.ok = false;
      if (!message.content) message.content = '请求已停止。';
    }
  }
  approvalSubmitting.value = '';
  questionSubmitting.value = '';
}

function stop() {
  interactionSequence++;
  controller.value?.abort();
  controller.value = undefined;
  loading.value = false;
  retryText.value = '';
  clearPendingMessages();
}

function keydown(event: KeyboardEvent) {
  if (event.key !== 'Enter' || event.isComposing) return;
  const shouldSend = sendOnEnter.value ? !event.shiftKey : event.ctrlKey || event.metaKey;
  if (!shouldSend) return;
  event.preventDefault();
  void send();
}

async function decideApproval(message: ChatMessage, approved: boolean) {
  if (!message.approval || approvalSubmitting.value) return;
  const id = message.approval.id;
  const requestToken = authToken.value;
  approvalSubmitting.value = id;
  try {
    await respondToApproval(id, approved);
    if (requestToken === authToken.value && message.approval?.id === id) message.approval = undefined;
  } catch (err) {
    if (requestToken !== authToken.value || !messages.value.includes(message) || message.approval?.id !== id) return;
    if (err instanceof InteractionUnavailableError) {
      message.approval = undefined;
      await syncInteractions();
    } else MessagePlugin.error(messageOf(err));
  } finally {
    if (approvalSubmitting.value === id) approvalSubmitting.value = '';
  }
}

async function answerQuestion(message: ChatMessage, selected?: string) {
  if (!message.question || questionSubmitting.value) return;
  const answer = (selected ?? questionAnswers[message.question.id] ?? '').trim();
  if (!answer) return;
  const id = message.question.id;
  const requestToken = authToken.value;
  questionSubmitting.value = id;
  try {
    await respondToQuestion(id, answer, message.question.kind === 'mslfrp_login');
    if (requestToken === authToken.value && message.question?.id === id) message.question = undefined;
  } catch (err) {
    if (requestToken !== authToken.value || !messages.value.includes(message) || message.question?.id !== id) return;
    if (err instanceof InteractionUnavailableError) {
      message.question = undefined;
      await syncInteractions();
    } else MessagePlugin.error(messageOf(err));
  } finally {
    if (questionSubmitting.value === id) questionSubmitting.value = '';
  }
}

const loginSubmitting = ref('');
let loginController: AbortController | undefined;
async function openMslFrpLogin(message: ChatMessage) {
  if (!message.question || loginSubmitting.value) return;
  const id = message.question.id;
  const active = new AbortController();
  loginController = active;
  loginSubmitting.value = id;
  try {
    await loginMslFrp(active.signal);
    if (!active.signal.aborted && message.question?.id === id) await answerQuestion(message, '继续');
  } catch (error) {
    if (!active.signal.aborted && !(error instanceof DOMException && error.name === 'AbortError'))
      MessagePlugin.error(messageOf(error));
  } finally {
    if (loginController === active) { loginSubmitting.value = ''; loginController = undefined; }
  }
}
watch(() => messages.value.some(m => m.question?.id === loginSubmitting.value), pending => {
  if (!pending) loginController?.abort();
});
onBeforeUnmount(() => loginController?.abort());

useMslFrpLogin(messages, message => answerQuestion(message, '继续'));

async function syncInteractions() {
  if (!loading.value && !messages.value.some((message) => message.approval || message.question)) return;
  const sequence = ++interactionSequence;
  const revision = eventRevision;
  const active = controller.value;
  const requestToken = authToken.value;
  const id = conversationId.value;
  const current = () => sequence === interactionSequence && revision === eventRevision
    && active === controller.value && requestToken === authToken.value && id === conversationId.value;
  try {
    const state = await getInteractionStatus();
    if (!current()) return;
    for (const message of messages.value) {
      if (message.approval && !state.approvalIds.includes(message.approval.id)) message.approval = undefined;
      if (message.question && !state.questionIds.includes(message.question.id)) message.question = undefined;
    }
    if (!state.active) {
      stop();
      const recoverySequence = interactionSequence;
      if (!id) return;
      const saved = await getConversation(id);
      if (recoverySequence !== interactionSequence || requestToken !== authToken.value || id !== conversationId.value) return;
      messages.value = saved.messages;
      clearPendingMessages();
      taskProgress.restore(saved.messages);
      await scrollToEnd();
    }
  } catch { /* A failed status request must not invalidate a still-live approval. */ }
}

async function openHistory() {
  view.value = 'history';
  historyLoading.value = true;
  selectedHistory.value = [];
  try {
    histories.value = await listConversations();
  } catch (err) {
    error.value = messageOf(err);
  } finally {
    historyLoading.value = false;
  }
}

async function openConversationItem(id: string) {
  historyLoading.value = true;
  try {
    const conversation = await getConversation(id);
    messages.value = conversation.messages;
    taskProgress.restore(conversation.messages);
    conversationId.value = conversation.id;
    if (models.value.some((model) => model.id === conversation.modelId))
      selectedModel.value = conversation.modelId;
    else if (!models.value.some((model) => model.id === selectedModel.value))
      selectedModel.value = models.value[0]?.id || '';
    view.value = 'chat';
    await scrollToEnd();
  } catch (err) {
    MessagePlugin.error(messageOf(err));
  } finally {
    historyLoading.value = false;
  }
}

function toggleHistory(id: string, checked: boolean) {
  selectedHistory.value = checked
    ? [...new Set([...selectedHistory.value, id])]
    : selectedHistory.value.filter((value) => value !== id);
}

function confirmDeleteHistory() {
  if (!selectedHistory.value.length) return;
  const dialog = DialogPlugin.confirm({
    header: '删除对话',
    body: `确定删除已选择的 ${selectedHistory.value.length} 个对话吗？`,
    theme: 'danger',
    confirmBtn: '删除',
    cancelBtn: '取消',
    onConfirm: async () => {
      try {
        await deleteConversations(selectedHistory.value);
        if (conversationId.value && selectedHistory.value.includes(conversationId.value)) reset();
        await openHistory();
        MessagePlugin.success('对话已删除');
      } catch (err) {
        MessagePlugin.error(messageOf(err));
      } finally {
        dialog.destroy();
      }
    },
    onClose: () => dialog.destroy(),
    onCancel: () => dialog.destroy(),
  });
}

async function openSettings() {
  view.value = 'settings';
  settingsLoading.value = true;
  try {
    if (status.value?.admin) presets.value = await listPresets();
  } catch (err) {
    error.value = messageOf(err);
  } finally {
    settingsLoading.value = false;
  }
}

defineExpose({ newChat, openHistory, openSettings, syncInteractions });

async function persistPreferences() {
  if (!status.value) return;
  savingPreferences.value = true;
  try {
    await savePreferences({ sendOnEnter: sendOnEnter.value });
    status.value.preferences.sendOnEnter = sendOnEnter.value;
    MessagePlugin.success('偏好已保存');
  } catch (err) {
    MessagePlugin.error(messageOf(err));
  } finally {
    savingPreferences.value = false;
  }
}

function editModel(source: 'personal' | 'preset', model?: ModelOption) {
  modelSource.value = source;
  Object.assign(modelDraft, emptyModel(), model ? {
    id: model.id.split(':', 2)[1],
    name: model.name,
    endpoint: model.endpoint || '',
    model: model.model,
    thinkingEnabled: model.thinkingEnabled,
    thinkingEffort: model.thinkingEffort,
  } : {});
  thinkingMode.value = modelDraft.thinkingEnabled === null ? 'default' : modelDraft.thinkingEnabled ? 'on' : 'off';
  modelDialog.value = true;
}

async function persistModel() {
  if (!modelValid.value) return;
  modelSaving.value = true;
  modelDraft.thinkingEnabled = thinkingMode.value === 'default' ? null : thinkingMode.value === 'on';
  try {
    if (modelSource.value === 'preset') await savePreset({ ...modelDraft });
    else await saveModel({ ...modelDraft });
    modelDialog.value = false;
    await refreshStatus();
    if (status.value?.admin) presets.value = await listPresets();
    MessagePlugin.success('模型已保存');
  } catch (err) {
    MessagePlugin.error(messageOf(err));
  } finally {
    modelSaving.value = false;
  }
}

function confirmDeleteModel(source: 'personal' | 'preset', model: ModelOption) {
  const dialog = DialogPlugin.confirm({
    header: '删除模型',
    body: `确定删除“${model.name}”吗？`,
    theme: 'danger', confirmBtn: '删除', cancelBtn: '取消',
    onConfirm: async () => {
      try {
        const id = model.id.split(':', 2)[1];
        if (source === 'preset') await deletePreset(id);
        else await deleteModel(id);
        await refreshStatus();
        if (status.value?.admin) presets.value = await listPresets();
        MessagePlugin.success('模型已删除');
      } catch (err) {
        MessagePlugin.error(messageOf(err));
      } finally {
        dialog.destroy();
      }
    },
    onClose: () => dialog.destroy(),
    onCancel: () => dialog.destroy(),
  });
}

function toolLabel(name?: string) {
  const labels: Record<string, string> = {
    list_tunnels: '查询面板隧道', start_tunnel: '启动隧道', delete_tunnel: '删除面板隧道', delete_mslfrp_tunnel: '删除 MSLFRP 云端隧道',
    select_mslfrp_node: '让用户选择节点', list_mslfrp_nodes: 'MSLFRP：查询节点', list_mslfrp_tunnels: 'MSLFRP：查询隧道',
    create_mslfrp_tunnel: '创建 MSLFRP 隧道', import_mslfrp_tunnel: '导入 MSLFRP 隧道',
    list_nodes: '查询节点', execute_node_command: '执行节点命令',
    ask_user: '询问用户', list_instances: '查询实例', get_instance: '读取实例', read_terminal: '读取终端',
    control_instance: '控制实例', send_command: '发送命令', update_instance: '更新实例', create_instance: '创建实例',
    delete_instance: '删除实例', list_files: '列出文件', read_file: '读取文件', edit_file: '编辑文件',
    create_file: '创建文件', delete_file: '删除文件', search_resources: '搜索资源',
    list_resource_versions: '查询资源版本', download_resource: '下载资源', list_msl_cores: 'MSL镜像源：查询核心',
    list_msl_core_versions: 'MSL镜像源：查询核心版本', list_msl_java_versions: 'MSL镜像源：查询 Java 版本',
    wait_for_task: '等待任务完成',
    wait_for_terminal_update: '等待终端内容更新',
  };
  return name ? labels[name] || name : '工具';
}

function formatDate(value: number) {
  return new Intl.DateTimeFormat('zh-CN', { dateStyle: 'medium', timeStyle: 'short' }).format(value);
}

function messageOf(value: unknown) {
  if (value instanceof Error) return value.message;
  if (typeof value === 'object' && value && 'message' in value) return String(value.message);
  return '请求失败，请稍后重试。';
}

watch(authToken, (next, previous) => {
  if (next === previous) return;
  reset(true);
  statusSequence++;
  selectionUserId = '';
  selectionToken = '';
  status.value = undefined;
  selectedModel.value = '';
  permissionMode.value = 'default';
  checking.value = false;
  if (next) void refreshStatus();
});

onMounted(refreshStatus);
function syncWhenVisible() {
  if (document.visibilityState === 'visible') void syncInteractions();
}
onMounted(() => {
  window.addEventListener('focus', syncWhenVisible);
  document.addEventListener('visibilitychange', syncWhenVisible);
});
onBeforeUnmount(() => {
  statusSequence++;
  interactionSequence++;
  controller.value?.abort();
  taskProgress.reset();
  window.removeEventListener('focus', syncWhenVisible);
  document.removeEventListener('visibilitychange', syncWhenVisible);
});
</script>

<template>
  <section class="ai-workspace" :class="{ compact }">
    <header v-if="!compact" class="ai-header">
      <div>
        <div class="ai-title-row">
          <h1>Elements AI</h1>
          <span class="instance-badge">{{ currentInstanceLabel }}</span>
        </div>
        <p>通过安全工具管理 MSLX 实例、终端、文件与模组资源</p>
      </div>
      <div class="header-actions">
        <t-button size="small" variant="outline" @click="newChat">新对话</t-button>
        <t-button size="small" variant="outline" @click="openHistory">历史</t-button>
        <t-button size="small" variant="outline" @click="openSettings">设置</t-button>
      </div>
    </header>

    <div v-if="checking" class="center-state"><t-loading /> 正在连接 Elements AI…</div>

    <template v-else-if="view === 'chat'">
      <main ref="list" class="message-list">
        <div v-if="!messages.length" class="empty-state">
          <div class="empty-mark">✦</div>
          <h2>有什么可以帮你？</h2>
          <p v-if="status?.ready">我可以检查实例状态、分析终端、修改配置文件，以及搜索和下载模组。</p>
          <p v-else>尚未配置模型。请先在“设置”中添加个人模型，或让管理员添加预设模型。</p>
          <div v-if="currentInstanceId" class="context-hint">当前上下文会优先使用实例 #{{ currentInstanceId }}</div>
        </div>

        <article v-for="(message, index) in messages" :key="index" class="message-row" :class="message.role">
          <div v-if="message.role === 'user'" class="message-bubble user-bubble">{{ message.content }}</div>

          <div v-else-if="message.role === 'assistant'" class="assistant-message">
            <ThinkingLine v-if="message.reasoning" :content="message.reasoning" :pending="message.pending" />
            <MarkdownMessage v-if="message.content" :content="message.content" />
            <div v-else-if="message.pending && !message.reasoning" class="working-indicator" role="status">
              <span class="braille-spinner" aria-hidden="true"></span><span>工作中</span>
            </div>
          </div>

          <div v-else-if="message.role === 'tool'" class="tool-row" :class="{ failed: message.ok === false }">
            <ToolReceipt :message="message" :label="toolLabel(message.tool)" />
            <div v-if="message.approval" class="approval-box">
              <p>该操作需要你的确认：</p>
              <pre>{{ message.approval.arguments }}</pre>
              <div class="inline-actions">
                <t-button size="small" theme="danger" :loading="approvalSubmitting === message.approval.id" @click="decideApproval(message, true)">允许</t-button>
                <t-button size="small" variant="outline" :disabled="Boolean(approvalSubmitting)" @click="decideApproval(message, false)">拒绝</t-button>
              </div>
            </div>
            <div v-if="message.question" class="question-box">
              <p v-if="message.question.kind !== 'mslfrp_login'">{{ message.question.question }}</p>
              <div class="option-list">
                <t-button v-for="option in message.question.options" :key="option" size="small" variant="outline" :disabled="Boolean(questionSubmitting)" @click="answerQuestion(message, option)">{{ option }}</t-button>
              </div>
              <t-button v-if="message.question.kind === 'mslfrp_login'" size="small" :loading="loginSubmitting === message.question.id" @click="openMslFrpLogin(message)">登录 MSLFRP</t-button>
              <div v-if="!message.question.kind || message.question.kind === 'question'" class="custom-answer">
                <input v-model="questionAnswers[message.question.id]" maxlength="500" placeholder="或输入自定义回答" @keydown.enter.prevent="answerQuestion(message)" />
                <t-button size="small" :loading="questionSubmitting === message.question.id" @click="answerQuestion(message)">提交</t-button>
              </div>
            </div>
            <FileDiffView v-if="message.diff" :diff="message.diff" />
          </div>

          <div v-else class="error-message">{{ message.content }}</div>
        </article>
      </main>

      <div v-if="retryText" class="activity-strip">
        <span>{{ retryText }}</span>
      </div>
      <div v-if="error" class="error-strip">{{ error }}</div>
      <TaskProgressList :tasks="activeTasks" />

      <footer class="composer">
        <div class="input-shell">
          <textarea v-model="draft" maxlength="4000" rows="3" :disabled="loading || !status?.ready" placeholder="输入你的需求；Shift+Enter 换行" @keydown="keydown"></textarea>
          <div class="input-footer">
            <span>{{ draft.length }} / 4000</span>
            <t-button v-if="loading" class="composer-action" theme="danger" variant="text" size="small" shape="square" aria-label="停止" title="停止" @click="stop"><StopIcon size="18px" /></t-button>
            <t-button v-else class="composer-action" variant="text" size="small" shape="square" aria-label="发送" title="发送" :disabled="!canSend" @click="send"><SendIcon size="18px" /></t-button>
          </div>
        </div>
        <div class="composer-controls">
          <label>
            <span>操作模式</span>
            <select v-model="permissionMode" :disabled="loading">
              <option value="default">默认确认</option>
              <option value="full">完整操作</option>
            </select>
          </label>
          <label class="model-select">
            <span>模型</span>
            <select v-model="selectedModel" :disabled="loading || !models.length">
              <option value="" disabled>请选择模型</option>
              <option v-for="model in models" :key="model.id" :value="model.id">{{ model.name }} · {{ model.model }}</option>
            </select>
          </label>
        </div>
      </footer>
    </template>

    <main v-else-if="view === 'history'" class="panel-view">
      <div class="panel-heading">
        <div><h2>对话历史</h2></div>
        <div class="inline-actions">
          <t-button size="small" variant="outline" @click="view = 'chat'">返回聊天</t-button>
          <t-button size="small" variant="outline" :disabled="historyLoading || !histories.length" @click="selectedHistory = selectedHistory.length === histories.length ? [] : histories.map(item => item.id)">{{ histories.length && selectedHistory.length === histories.length ? '取消全选' : '全选' }}</t-button>
          <t-button size="small" theme="danger" variant="outline" :disabled="!selectedHistory.length" @click="confirmDeleteHistory">删除所选</t-button>
        </div>
      </div>
      <div v-if="historyLoading" class="center-state"><t-loading /> 正在读取历史…</div>
      <div v-else-if="!histories.length" class="center-state">还没有保存的对话。</div>
      <div v-else class="history-list">
        <article v-for="item in histories" :key="item.id" class="history-item">
          <input type="checkbox" :checked="selectedHistory.includes(item.id)" @change="toggleHistory(item.id, ($event.target as HTMLInputElement).checked)" />
          <button type="button" @click="openConversationItem(item.id)">
            <strong>{{ item.title || '未命名对话' }}</strong>
            <span>{{ item.modelName }} · {{ formatDate(item.updatedAt) }}</span>
          </button>
        </article>
      </div>
    </main>

    <main v-else class="panel-view settings-view">
      <div class="panel-heading">
        <div><h2>Elements AI 设置</h2></div>
        <t-button size="small" variant="outline" @click="view = 'chat'">返回聊天</t-button>
      </div>
      <div v-if="settingsLoading" class="center-state"><t-loading /> 正在读取设置…</div>
      <template v-else>
        <section class="settings-card">
          <div><h3>发送偏好</h3><p>开启后按 Enter 发送，Shift+Enter 换行；关闭后使用 Ctrl/⌘+Enter 发送。</p></div>
          <div class="settings-action"><t-switch v-model="sendOnEnter" /><t-button size="small" :loading="savingPreferences" @click="persistPreferences">保存</t-button></div>
        </section>

        <section class="model-section">
          <div class="section-heading"><div><h3>个人模型</h3></div><t-button size="small" @click="editModel('personal')">添加模型</t-button></div>
          <div v-if="!personalModels.length" class="muted-box">暂无个人模型。</div>
          <div v-else class="model-grid">
            <article v-for="model in personalModels" :key="model.id" class="model-card">
              <div><strong>{{ model.name }}</strong><span>{{ model.model }}</span><small>{{ model.endpoint }}</small></div>
              <div class="inline-actions"><t-button size="small" variant="outline" @click="editModel('personal', model)">编辑</t-button><t-button size="small" theme="danger" variant="text" @click="confirmDeleteModel('personal', model)">删除</t-button></div>
            </article>
          </div>
        </section>

        <section v-if="status?.admin" class="model-section">
          <div class="section-heading"><div><h3>管理员预设</h3></div><t-button size="small" @click="editModel('preset')">添加预设</t-button></div>
          <div v-if="!presets.length" class="muted-box">暂无管理员预设。</div>
          <div v-else class="model-grid">
            <article v-for="model in presets" :key="model.id" class="model-card">
              <div><strong>{{ model.name }}</strong><span>{{ model.model }}</span><small>{{ model.endpoint }}</small></div>
              <div class="inline-actions"><t-button size="small" variant="outline" @click="editModel('preset', model)">编辑</t-button><t-button size="small" theme="danger" variant="text" @click="confirmDeleteModel('preset', model)">删除</t-button></div>
            </article>
          </div>
        </section>
      </template>
    </main>

    <t-dialog v-model:visible="modelDialog" :header="modelDraft.id ? '编辑模型' : '添加模型'" attach="body" width="560px" :confirm-btn="null" :cancel-btn="null">
      <form class="model-form" @submit.prevent="persistModel">
        <label><span>显示名称</span><input v-model="modelDraft.name" maxlength="100" required placeholder="例如：GPT-5" /></label>
        <label><span>接口基础地址</span><input v-model="modelDraft.endpoint" maxlength="2048" required placeholder="https://api.example.com/v1" /></label>
        <label><span>模型标识</span><input v-model="modelDraft.model" maxlength="200" required placeholder="模型名称" /></label>
        <label><span>API Key</span><input v-model="modelDraft.apiKey" maxlength="4096" type="password" :placeholder="modelDraft.id ? '留空以保留现有密钥' : '可留空'" /></label>
        <label v-if="modelDraft.id && modelDraft.apiKey === ''" class="check-line"><input v-model="modelDraft.clearApiKey" type="checkbox" /> 清除已保存的 API Key</label>
        <div class="form-grid">
          <label><span>思考模式</span><select v-model="thinkingMode"><option value="default">服务默认</option><option value="on">开启</option><option value="off">关闭</option></select></label>
          <label><span>思考强度</span><select v-model="modelDraft.thinkingEffort" :disabled="thinkingMode !== 'on'"><option value="low">低</option><option value="medium">中</option><option value="high">高</option></select></label>
        </div>
        <div class="dialog-actions"><t-button variant="outline" type="button" @click="modelDialog = false">取消</t-button><t-button type="submit" :disabled="!modelValid" :loading="modelSaving">保存</t-button></div>
      </form>
    </t-dialog>
  </section>
</template>

<style scoped>
.ai-workspace { display: flex; flex-direction: column; min-height: 620px; height: calc(100vh - 132px); color: var(--td-text-color-primary); background: var(--td-bg-color-container); border: 1px solid var(--td-component-border); border-radius: 18px; overflow: hidden; box-shadow: var(--td-shadow-1); }
.ai-workspace.compact { min-height: 520px; height: 76vh; border: 0; box-shadow: none; }
.ai-header { display: flex; align-items: center; justify-content: space-between; gap: 1rem; padding: 1rem 1.25rem; border-bottom: 1px solid var(--td-component-border); background: linear-gradient(135deg, color-mix(in srgb, var(--td-brand-color) 10%, var(--td-bg-color-container)), var(--td-bg-color-container)); }
.ai-title-row { display: flex; align-items: center; gap: 0.65rem; }
.ai-header h1 { margin: 0; font-size: 1.2rem; }
.ai-header p { margin: 0.3rem 0 0; color: var(--td-text-color-secondary); font-size: 12px; }
.instance-badge { padding: 0.18rem 0.48rem; border-radius: 999px; color: var(--td-brand-color); background: color-mix(in srgb, var(--td-brand-color) 12%, transparent); font-size: 11px; font-weight: 600; }
.header-actions, .inline-actions { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
.message-list { flex: 1; overflow-y: auto; padding: 1.2rem clamp(0.8rem, 3vw, 3rem); background: color-mix(in srgb, var(--td-bg-color-page) 55%, var(--td-bg-color-container)); }
.empty-state { min-height: 100%; display: grid; place-content: center; justify-items: center; text-align: center; color: var(--td-text-color-secondary); }
.empty-state h2 { margin: 0.4rem 0; color: var(--td-text-color-primary); font-size: 28px; font-weight: 700; line-height: 1.4; }
.empty-state p { max-width: 560px; margin: 0; line-height: 1.7; }
.empty-mark { display: grid; place-content: center; width: 62px; height: 62px; color: var(--td-brand-color); font-size: 30px; }
.context-hint { margin-top: 1rem; padding: 0.45rem 0.7rem; border: 1px solid var(--td-component-border); border-radius: 999px; font-size: 12px; }
.message-row { display: flex; margin: 0.8rem 0; }
.message-row.user { justify-content: flex-end; }
.message-bubble { max-width: min(780px, 86%); border-radius: 14px; padding: 0.75rem 0.9rem; }
.user-bubble { white-space: pre-wrap; color: var(--td-text-color-anti); background: var(--td-brand-color); border-bottom-right-radius: 5px; }
.assistant-message { width: min(780px, 92%); }
.working-indicator { display: flex; align-items: center; gap: 0.5rem; padding: 0.35rem; color: var(--td-text-color-secondary); font-size: 14px; }
.braille-spinner { flex-shrink: 0; width: 1em; color: var(--td-brand-color); font-family: ui-monospace, monospace; font-size: 18px; line-height: 1; }
.braille-spinner::before { content: '⠋'; animation: braille-spin 0.8s steps(1, end) infinite; }
@keyframes braille-spin {
  0%, 100% { content: '⠋'; } 10% { content: '⠙'; } 20% { content: '⠹'; }
  30% { content: '⠸'; } 40% { content: '⠼'; } 50% { content: '⠴'; }
  60% { content: '⠦'; } 70% { content: '⠧'; } 80% { content: '⠇'; } 90% { content: '⠏'; }
}
@media (prefers-reduced-motion: reduce) { .braille-spinner::before { animation: none; } }
.tool-row { width: min(780px, 92%); padding: 0.25rem 0; }
.tool-row.failed { color: var(--td-error-color); }
.approval-box, .question-box { margin-top: 0.65rem; padding: 0.7rem 0.8rem; border-left: 2px solid var(--td-component-border); color: var(--td-text-color-secondary); }
.approval-box p, .question-box p { margin: 0 0 0.55rem; font-weight: 600; }
.approval-box pre { overflow: auto; max-height: 240px; white-space: pre-wrap; margin: 0 0 0.65rem; padding: 0.6rem; border-radius: 6px; color: #e5e7eb; background: #151922; }
.option-list { display: flex; flex-wrap: wrap; gap: 0.45rem; }
.custom-answer { display: flex; gap: 0.5rem; margin-top: 0.55rem; }.custom-answer input { flex: 1; }
.error-message { width: min(780px, 92%); color: var(--td-error-color); }
.error-strip { color: var(--td-error-color); background: color-mix(in srgb, var(--td-error-color) 9%, var(--td-bg-color-container)); border: 1px solid color-mix(in srgb, var(--td-error-color) 28%, transparent); }
.activity-strip, .error-strip { padding: 0.45rem 1.25rem; font-size: 12px; }.activity-strip { display: flex; justify-content: space-between; color: var(--td-brand-color); border-top: 1px solid var(--td-component-border); }
.composer { padding: 0.75rem 1rem 1rem; border-top: 1px solid var(--td-component-border); background: var(--td-bg-color-container); }
.composer-controls { display: flex; gap: 0.75rem; margin-top: 0.55rem; }.composer-controls label { display: flex; align-items: center; gap: 0.4rem; font-size: 12px; color: var(--td-text-color-secondary); }.composer-controls .model-select { flex: 1; min-width: 0; }.composer-controls select { min-width: 130px; }.model-select select { width: min(420px, 100%); }
.input-shell { border: 1px solid var(--td-component-border); border-radius: 13px; overflow: hidden; transition: border-color .2s; }.input-shell:focus-within { border-color: var(--td-brand-color); }
.input-shell textarea { width: 100%; box-sizing: border-box; resize: none; border: 0; outline: 0; padding: 0.75rem; color: var(--td-text-color-primary); background: transparent; font: inherit; }
.input-footer { display: flex; align-items: center; justify-content: space-between; padding: 0.4rem 0.55rem 0.5rem; color: var(--td-text-color-placeholder); font-size: 11px; }
.input-footer .composer-action { background: transparent; border-color: transparent; }
.input-footer .composer-action:focus-visible { outline: 2px solid var(--td-brand-color); outline-offset: 2px; }
.panel-view { flex: 1; overflow-y: auto; padding: 1.2rem; background: var(--td-bg-color-page); }
.panel-heading, .section-heading, .settings-card, .model-card { display: flex; justify-content: space-between; align-items: center; gap: 1rem; }
.panel-heading { padding-bottom: 1rem; border-bottom: 1px solid var(--td-component-border); }.panel-heading h2, .section-heading h3, .settings-card h3 { margin: 0; }.panel-heading p, .section-heading p, .settings-card p { margin: 0.25rem 0 0; color: var(--td-text-color-secondary); font-size: 12px; }
.history-list, .model-grid { display: grid; gap: 0.65rem; margin-top: 1rem; }
.history-item { display: flex; align-items: center; gap: 0.7rem; padding: 0.75rem; border: 1px solid var(--td-component-border); border-radius: 12px; background: var(--td-bg-color-container); }.history-item button { display: grid; flex: 1; gap: 0.25rem; text-align: left; border: 0; color: inherit; background: transparent; cursor: pointer; }.history-item button span { color: var(--td-text-color-secondary); font-size: 12px; }
.settings-card, .model-section { margin-top: 1rem; padding: 1rem; border: 1px solid var(--td-component-border); border-radius: 14px; background: var(--td-bg-color-container); }.settings-action { display: flex; align-items: center; gap: 0.65rem; }.model-section { display: block; }.model-card { padding: 0.75rem; border: 1px solid var(--td-component-border); border-radius: 11px; }.model-card > div:first-child { display: grid; gap: 0.2rem; }.model-card span, .model-card small { color: var(--td-text-color-secondary); }.model-card small { overflow-wrap: anywhere; }
.muted-box, .center-state { display: flex; align-items: center; justify-content: center; gap: 0.5rem; min-height: 100px; color: var(--td-text-color-secondary); }.muted-box { min-height: auto; margin-top: 0.8rem; padding: 1rem; border-radius: 10px; background: var(--td-bg-color-secondarycontainer); }
.model-form { display: grid; gap: 0.8rem; }.model-form label { display: grid; gap: 0.3rem; font-size: 13px; }.model-form label > span { font-weight: 600; }.model-form input, .model-form select, .custom-answer input, .composer-controls select { box-sizing: border-box; min-height: 34px; padding: 0.42rem 0.55rem; border: 1px solid var(--td-component-border); border-radius: 7px; outline: 0; color: var(--td-text-color-primary); background: var(--td-bg-color-container); }.model-form input:focus, .model-form select:focus, .custom-answer input:focus, .composer-controls select:focus { border-color: var(--td-brand-color); }.model-form .check-line { display: flex; grid-template-columns: auto 1fr; align-items: center; justify-content: start; }.check-line input { min-height: auto; }.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0.75rem; }.dialog-actions { display: flex; justify-content: flex-end; gap: 0.6rem; margin-top: 0.3rem; }
@media (max-width: 720px) { .ai-workspace { height: calc(100vh - 100px); min-height: 520px; }.ai-header { align-items: flex-start; }.header-actions { justify-content: flex-end; }.message-bubble, .assistant-message, .tool-row, .error-message { max-width: 96%; width: auto; }.composer-controls { align-items: stretch; flex-direction: column; }.composer-controls label { justify-content: space-between; }.composer-controls select, .model-select select { width: 68%; }.panel-heading, .section-heading, .settings-card, .model-card { align-items: flex-start; flex-direction: column; }.form-grid { grid-template-columns: 1fr; } }
</style>
