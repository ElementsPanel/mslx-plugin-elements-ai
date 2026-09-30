<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue';

const props = defineProps<{ content: string; pending?: boolean }>();
const contentElement = ref<HTMLElement>();
let resizeObserver: ResizeObserver | undefined;

function followLatest() {
  if (contentElement.value) contentElement.value.scrollLeft = contentElement.value.scrollWidth;
}

watch(() => props.content, followLatest, { flush: 'post' });
onMounted(() => {
  followLatest();
  if (typeof ResizeObserver !== 'undefined' && contentElement.value) {
    resizeObserver = new ResizeObserver(followLatest);
    resizeObserver.observe(contentElement.value);
  }
});
onBeforeUnmount(() => resizeObserver?.disconnect());
</script>

<template>
  <div class="thinking-line">
    <span class="thinking-label">{{ pending ? '正在思考…' : '思考完成' }}</span>
    <span ref="contentElement" class="thinking-content">{{ content }}</span>
  </div>
</template>

<style scoped>
.thinking-line { display: flex; align-items: center; gap: 0.5rem; min-width: 0; margin-bottom: 0.7rem; overflow: hidden; white-space: nowrap; color: var(--td-text-color-secondary); font-size: 12px; }
.thinking-label { flex-shrink: 0; font-weight: 600; }
.thinking-content { flex: 1; min-width: 0; overflow: hidden; white-space: nowrap; }
</style>
