<script setup lang="ts">
import { computed } from 'vue';
import { CheckCircleIcon, ErrorCircleIcon, LoadingIcon } from 'tdesign-icons-vue-next';
import type { ChatMessage } from '../types/ai';

const props = defineProps<{ message: ChatMessage; label: string }>();
const available = computed(() => !props.message.pending && Boolean(props.message.content || props.message.commandResult));
const receipt = computed(() => {
  try { return JSON.stringify(JSON.parse(props.message.content), null, 2); }
  catch { return props.message.content; }
});
</script>

<template>
  <details class="tool-receipt">
    <summary class="tool-title" :aria-disabled="!available" :tabindex="available ? 0 : -1" @click="!available && $event.preventDefault()">
      <LoadingIcon v-if="message.pending" class="tool-status-icon pending" size="16px" />
      <CheckCircleIcon v-else-if="message.ok" class="tool-status-icon success" size="16px" />
      <ErrorCircleIcon v-else class="tool-status-icon failed" size="16px" />
      <strong>{{ label }}</strong>
      <small>{{ message.pending ? '处理中' : message.ok ? '已完成' : message.commandResult ? '执行失败' : '未执行' }}</small>
    </summary>
    <div v-if="available && message.commandResult" class="command-result">
      <p>节点 {{ message.commandResult.nodeId }} · {{ message.commandResult.timedOut ? '执行超时' : `退出码 ${message.commandResult.exitCode}` }}</p>
      <pre v-if="message.commandResult.stdout">{{ message.commandResult.stdout }}</pre>
      <pre v-if="message.commandResult.stderr">{{ message.commandResult.stderr }}</pre>
      <small v-if="message.commandResult.truncated">输出过长，已截断。</small>
    </div>
    <pre v-else-if="available" class="receipt-content">{{ receipt }}</pre>
  </details>
</template>

<style scoped>
.tool-title { display: flex; align-items: center; gap: 0.45rem; list-style: none; cursor: pointer; color: var(--td-text-color-secondary); font-size: 13px; border-radius: 4px; }
.tool-title::-webkit-details-marker { display: none; }
.tool-title[aria-disabled="true"] { cursor: default; }
.tool-title:not([aria-disabled="true"]):hover { background: var(--td-bg-color-secondarycontainer); }
.tool-title:focus-visible { outline: 2px solid var(--td-brand-color); outline-offset: 3px; }
.tool-title strong { color: var(--td-text-color-primary); font-weight: 600; }
.tool-title small { margin-left: auto; color: var(--td-text-color-placeholder); }
.tool-status-icon { flex: 0 0 auto; }
.tool-status-icon.pending { color: var(--td-brand-color); animation: spin 1s linear infinite; }
.tool-status-icon.success { color: var(--td-success-color); }
.tool-status-icon.failed { color: var(--td-error-color); }
@keyframes spin { to { transform: rotate(360deg); } }
.command-result { margin-top: 0.65rem; color: var(--td-text-color-secondary); }
.command-result p { margin: 0 0 0.5rem; font-size: 12px; }
.receipt-content, .command-result pre { margin: 0.65rem 0 0; padding: 0.6rem; max-height: 300px; overflow: auto; white-space: pre-wrap; overflow-wrap: anywhere; border-radius: 6px; color: #e5e7eb; background: #151922; }
</style>
