import request from 'mslx-request';
import type {
  AiStatus,
  AiTaskProgress,
  ChatEvent,
  ChatPreferences,
  ConversationDetail,
  ConversationSummary,
  ModelInput,
  ModelOption,
  PermissionMode,
} from './types/ai';

const base = '/api/plugins/mslx-plugin-elements-ai/ai';

function userStore() {
  const stores = (window as any).MSLX_Stores;
  return stores?.getUserStore?.() || stores?.useUserStore?.();
}

function token() {
  const value = userStore()?.token;
  if (!value) throw new Error('登录状态已失效，请重新登录。');
  return value as string;
}

export const getStatus = () => request.get({ url: `${base}/status` }) as Promise<AiStatus>;
export async function getTaskProgress(taskId: string, signal: AbortSignal): Promise<AiTaskProgress> {
  const response = await fetch(`${base}/tasks/${encodeURIComponent(taskId)}`, {
    credentials: 'same-origin', signal, headers: { 'x-user-token': token() },
  });
  const body = await response.json();
  if (!response.ok || body.code !== 200) throw new Error(body.message || `HTTP ${response.status}`);
  return body.data;
}
export const savePreferences = (preferences: ChatPreferences) =>
  request.put({ url: `${base}/preferences`, data: preferences }) as Promise<boolean>;
export const saveModel = (model: ModelInput) =>
  request.put({ url: `${base}/models`, data: model }) as Promise<boolean>;
export const deleteModel = (id: string) =>
  request.delete({ url: `${base}/models/${encodeURIComponent(id)}` }) as Promise<boolean>;
export const listPresets = () => request.get({ url: `${base}/presets` }) as Promise<ModelOption[]>;
export const savePreset = (model: ModelInput) =>
  request.put({ url: `${base}/presets`, data: model }) as Promise<boolean>;
export const deletePreset = (id: string) =>
  request.delete({ url: `${base}/presets/${encodeURIComponent(id)}` }) as Promise<boolean>;
export const listConversations = () =>
  request.get({ url: `${base}/conversations` }) as Promise<ConversationSummary[]>;
export const getConversation = (id: string) =>
  request.get({ url: `${base}/conversations/${encodeURIComponent(id)}` }) as Promise<ConversationDetail>;
export const deleteConversations = (ids: string[]) =>
  request.delete({ url: `${base}/conversations`, data: { ids } }) as Promise<number>;
export const respondToApproval = (id: string, approved: boolean) =>
  request.post({ url: `${base}/approvals/${encodeURIComponent(id)}`, data: { approved } }) as Promise<boolean>;
export const respondToQuestion = (id: string, answer: string) =>
  request.post({ url: `${base}/questions/${encodeURIComponent(id)}`, data: { answer } }) as Promise<boolean>;

export async function sendMessage(
  message: string,
  conversationId: string | undefined,
  modelId: string,
  permissionMode: PermissionMode,
  currentInstanceId: number | undefined,
  signal: AbortSignal,
  onEvent: (event: ChatEvent) => void,
) {
  const response = await fetch(`${base}/chat`, {
    method: 'POST',
    credentials: 'same-origin',
    signal,
    headers: {
      'Content-Type': 'application/json',
      'X-Requested-With': 'XMLHttpRequest',
      'x-user-token': token(),
    },
    body: JSON.stringify({
      message, conversationId, modelId, permissionMode, currentInstanceId,
      currentNodeId: localStorage.getItem('ACTIVE_NODE_ID') || 'local',
    }),
  });
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    throw new Error(body.message || `HTTP ${response.status}`);
  }
  if (!response.body || !response.headers.get('content-type')?.includes('text/event-stream'))
    throw new Error('服务器没有返回事件流。');

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  let dataLines: string[] = [];
  let completed = false;
  try {
    while (!signal.aborted) {
      const { value, done } = await reader.read();
      if (done) break;
      buffer += decoder.decode(value, { stream: true });
      while (true) {
        const match = buffer.match(/\r?\n/);
        if (!match || match.index === undefined) break;
        const line = buffer.slice(0, match.index);
        buffer = buffer.slice(match.index + match[0].length);
        if (!line) {
          if (dataLines.length) {
            const event = JSON.parse(dataLines.join('\n')) as ChatEvent;
            dataLines = [];
            if (event.type === 'error') throw new Error(event.message);
            onEvent(event);
            if (event.type === 'done') completed = true;
          }
        } else if (line.startsWith('data:')) dataLines.push(line.slice(5).trimStart());
      }
      if (completed) break;
    }
    if (!completed && !signal.aborted) throw new Error('AI 事件流意外中断。');
  } finally {
    await reader.cancel().catch(() => undefined);
    reader.releaseLock();
  }
}
