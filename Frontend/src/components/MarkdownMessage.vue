<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{ content: string }>();

function escapeHtml(value: string) {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function inline(value: string) {
  return value
    .replace(/`([^`\n]+)`/g, '<code>$1</code>')
    .replace(/\*\*([^*\n]+)\*\*/g, '<strong>$1</strong>')
    .replace(/(?<!\*)\*([^*\n]+)\*(?!\*)/g, '<em>$1</em>');
}

const html = computed(() => {
  const blocks: string[] = [];
  let value = props.content.replace(/```([^\n`]*)\n([\s\S]*?)```/g, (_all, language, code) => {
    const index = blocks.push(
      `<pre><code data-language="${escapeHtml(String(language).trim())}">${escapeHtml(String(code))}</code></pre>`,
    ) - 1;
    return `@@ELEMENTS_AI_BLOCK_${index}@@`;
  });
  value = escapeHtml(value);
  const lines = value.split(/\r?\n/);
  const output: string[] = [];
  let listOpen = false;
  for (const line of lines) {
    const block = /^@@ELEMENTS_AI_BLOCK_(\d+)@@$/.exec(line);
    if (block) {
      if (listOpen) output.push('</ul>');
      listOpen = false;
      output.push(blocks[Number(block[1])] || '');
      continue;
    }
    const list = /^\s*[-*]\s+(.+)$/.exec(line);
    if (list) {
      if (!listOpen) output.push('<ul>');
      listOpen = true;
      output.push(`<li>${inline(list[1])}</li>`);
      continue;
    }
    if (listOpen) output.push('</ul>');
    listOpen = false;
    if (!line.trim()) output.push('<div class="md-gap"></div>');
    else if (line.startsWith('### ')) output.push(`<h3>${inline(line.slice(4))}</h3>`);
    else if (line.startsWith('## ')) output.push(`<h2>${inline(line.slice(3))}</h2>`);
    else if (line.startsWith('# ')) output.push(`<h1>${inline(line.slice(2))}</h1>`);
    else if (line.startsWith('&gt; ')) output.push(`<blockquote>${inline(line.slice(5))}</blockquote>`);
    else output.push(`<p>${inline(line)}</p>`);
  }
  if (listOpen) output.push('</ul>');
  return output.join('');
});
</script>

<template>
  <div class="markdown-message" v-html="html"></div>
</template>

<style scoped>
.markdown-message { line-height: 1.72; overflow-wrap: anywhere; }
.markdown-message :deep(p) { margin: 0 0 0.45rem; }
.markdown-message :deep(p:last-child) { margin-bottom: 0; }
.markdown-message :deep(h1),
.markdown-message :deep(h2),
.markdown-message :deep(h3) { margin: 0.8rem 0 0.35rem; line-height: 1.3; }
.markdown-message :deep(h1) { font-size: 1.2rem; }
.markdown-message :deep(h2) { font-size: 1.08rem; }
.markdown-message :deep(h3) { font-size: 1rem; }
.markdown-message :deep(ul) { margin: 0.35rem 0; padding-left: 1.35rem; }
.markdown-message :deep(code) { font-family: ui-monospace, SFMono-Regular, Menlo, monospace; background: var(--td-bg-color-secondarycontainer); padding: 0.12rem 0.32rem; border-radius: 5px; }
.markdown-message :deep(pre) { overflow: auto; margin: 0.55rem 0; padding: 0.8rem; border-radius: 10px; background: #151922; color: #e5e7eb; }
.markdown-message :deep(pre code) { padding: 0; background: transparent; white-space: pre; }
.markdown-message :deep(blockquote) { margin: 0.5rem 0; padding-left: 0.75rem; border-left: 3px solid var(--td-brand-color); color: var(--td-text-color-secondary); }
.markdown-message :deep(.md-gap) { height: 0.45rem; }
</style>
