<script setup lang="ts">
import { computed } from 'vue';
import type { FileDiff } from '../types/ai';

const props = defineProps<{ diff: FileDiff }>();
const lines = computed(() => props.diff.patch.replace(/\r?\n$/, '').split(/\r?\n/).map((text, index) => ({
  text,
  type: index < 2 ? 'context' : text.startsWith('+') ? 'add' : text.startsWith('-') ? 'remove' : 'context',
})));
</script>

<template>
  <details class="file-diff" open>
    <summary>文件差异 · {{ diff.path }}<span v-if="diff.truncated">（已截断）</span></summary>
    <pre><code><span v-for="(line, index) in lines" :key="index" :class="`diff-${line.type}`">{{ line.text }}</span></code></pre>
  </details>
</template>

<style scoped>
.file-diff { margin-top: 0.65rem; border: 1px solid var(--td-component-border); border-radius: 10px; overflow: hidden; }
.file-diff summary { cursor: pointer; padding: 0.55rem 0.7rem; font-size: 12px; font-weight: 600; background: var(--td-bg-color-secondarycontainer); }
.file-diff pre { max-height: 320px; overflow: auto; margin: 0; padding: 0.65rem; background: #151922; color: #d7dce5; font-size: 12px; }
.file-diff code span { display: block; min-height: 1.35em; }
.diff-add { background: rgba(46, 160, 67, 0.22); color: #aff5b4; }
.diff-remove { background: rgba(248, 81, 73, 0.2); color: #ffdcd7; }
</style>
