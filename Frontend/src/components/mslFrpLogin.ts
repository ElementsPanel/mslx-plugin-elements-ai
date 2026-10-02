import { onBeforeUnmount, watch, type Ref } from 'vue';
import type { ChatMessage } from '../types/ai';

// Watch browser login state locally; never poll the cloud or put credentials in chat.
export function useMslFrpLogin(messages: Ref<ChatMessage[]>, resume: (message: ChatMessage) => Promise<void>) {
  const observed = new Map<string, string>();
  let timer: ReturnType<typeof setInterval> | undefined;
  const token = () => {
    try { return localStorage.getItem('msl-user-token') || ''; } catch { return ''; }
  };
  const check = () => {
    const current = token();
    for (const message of messages.value) {
      const question = message.question;
      if (question?.kind !== 'mslfrp_login') continue;
      if (current && current !== observed.get(question.id)) {
        observed.set(question.id, current);
        void resume(message);
      }
    }
  };
  const stop = watch(() => messages.value.filter(m => m.question?.kind === 'mslfrp_login').map(m => m.question!.id), ids => {
    for (const id of observed.keys()) if (!ids.includes(id)) observed.delete(id);
    for (const id of ids) if (!observed.has(id)) observed.set(id, token());
    if (ids.length && timer === undefined) timer = setInterval(check, 1000);
    if (!ids.length && timer !== undefined) { clearInterval(timer); timer = undefined; }
  }, { flush: 'sync' });
  onBeforeUnmount(() => { stop(); if (timer !== undefined) clearInterval(timer); observed.clear(); });
}
