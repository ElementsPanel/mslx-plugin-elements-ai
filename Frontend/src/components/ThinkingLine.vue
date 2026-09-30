<script setup lang="ts">
defineProps<{ content: string; pending?: boolean }>();
</script>

<template>
  <div class="thinking-line">
    <span class="thinking-label">{{ pending ? '正在思考…' : '思考完成' }}</span>
    <span class="thinking-content">{{ content }}</span>
  </div>
</template>

<style scoped>
.thinking-line { display: flex; align-items: center; gap: 0.5rem; min-width: 0; margin-bottom: 0.7rem; overflow: hidden; white-space: nowrap; color: var(--td-text-color-secondary); font-size: 12px; }
.thinking-label { flex-shrink: 0; font-weight: 600; }
.thinking-content { min-width: 0; overflow: hidden; text-overflow: ellipsis; }
</style>
