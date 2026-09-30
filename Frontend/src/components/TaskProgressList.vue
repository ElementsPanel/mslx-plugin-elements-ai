<script setup lang="ts">
import type { TaskProgressView } from '../types/ai';

defineProps<{ tasks: TaskProgressView[] }>();
const labels = { pending: '等待中', running: '进行中', success: '已完成', failed: '失败', canceled: '已取消' };
function percent(task: TaskProgressView) {
  return task.value == null || !Number.isFinite(task.value) ? undefined : Math.round(Math.min(100, Math.max(0, task.value)));
}
</script>

<template>
  <div v-if="tasks.length" class="task-progress-list" aria-label="下载与安装任务进度">
    <div v-for="task in tasks" :key="task.taskId" class="task-progress" :class="[task.state, { unavailable: task.unavailable }]">
      <div class="task-heading">
        <span class="task-title" :title="task.title">{{ task.title }}</span>
        <span class="task-state">{{ task.unavailable ? '状态暂不可用' : labels[task.state] }}<template v-if="!task.unavailable && percent(task) !== undefined"> · {{ percent(task) }}%</template></span>
      </div>
      <div class="task-detail">
        <span>{{ task.message || task.fileName || labels[task.state] }}</span>
        <span v-if="task.speed && !task.completed && !task.unavailable" class="task-speed">{{ task.speed }}</span>
      </div>
      <div class="task-bar" :class="{ indeterminate: percent(task) === undefined && !task.completed && !task.unavailable }"
        role="progressbar" :aria-label="task.title" :aria-valuenow="task.unavailable ? undefined : percent(task)" :aria-valuetext="task.unavailable ? '状态暂不可用' : labels[task.state]" :aria-valuemin="0" :aria-valuemax="100">
        <span :style="{ width: `${percent(task) ?? 30}%` }"></span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.task-progress-list { flex-shrink: 0; max-height: 180px; overflow-y: auto; padding: 0.7rem 1.25rem; border-top: 1px solid var(--td-component-border); display: grid; gap: 0.8rem; }
.task-progress { min-width: 0; color: var(--td-brand-color); }
.task-heading, .task-detail { display: flex; align-items: center; justify-content: space-between; gap: 0.75rem; font-size: 12px; }
.task-title { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--td-text-color-primary); }
.task-state, .task-speed { flex-shrink: 0; font-variant-numeric: tabular-nums; }
.task-detail { margin-top: 4px; color: var(--td-text-color-secondary); }
.task-detail > span:first-child { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.task-bar { height: 3px; margin-top: 7px; overflow: hidden; border-radius: 3px; background: var(--td-bg-color-component); }
.task-bar > span { display: block; height: 100%; border-radius: inherit; background: currentColor; transition: width 0.3s; }
.success { color: var(--td-success-color); }.failed { color: var(--td-error-color); }.canceled, .unavailable { color: var(--td-text-color-placeholder); }
.indeterminate > span { animation: task-moving 1.5s ease-in-out infinite alternate; }
@keyframes task-moving { to { transform: translateX(230%); } }
@media (prefers-reduced-motion: reduce) { .indeterminate > span { animation: none; } }
</style>
