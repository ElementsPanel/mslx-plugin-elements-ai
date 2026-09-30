import { computed, ref } from 'vue';
import { getTaskProgress } from '../api';
import type { AiTaskProgress, ChatMessage, TaskProgressView } from '../types/ai';

export function useTaskProgress(read = getTaskProgress) {
  const snapshots = ref<Record<string, TaskProgressView>>({});
  const tasks = computed(() => Object.values(snapshots.value));
  const revisions = new Map<string, number>();
  const failures = new Map<string, number>();
  let generation = 0;
  let timer: ReturnType<typeof setTimeout> | undefined;
  let polling = false;
  const requests = new Set<AbortController>();

  function schedule() {
    if (timer || polling || !tasks.value.some((task) => !task.completed && !task.unavailable)) return;
    timer = setTimeout(() => { timer = undefined; void poll(); }, 2000);
  }

  function track(progress?: AiTaskProgress) {
    if (!progress?.taskId || snapshots.value[progress.taskId]?.completed) return;
    snapshots.value[progress.taskId] = { ...progress, unavailable: false };
    revisions.set(progress.taskId, (revisions.get(progress.taskId) ?? 0) + 1);
    failures.delete(progress.taskId);
    schedule();
  }

  async function poll() {
    const current = generation;
    polling = true;
    await Promise.all(tasks.value.filter((task) => !task.completed && !task.unavailable).map(async (task) => {
      const revision = revisions.get(task.taskId);
      const controller = new AbortController();
      requests.add(controller);
      const timeout = setTimeout(() => controller.abort(), 10000);
      try {
        const progress = await read(task.taskId, controller.signal);
        if (current !== generation || revision !== revisions.get(task.taskId)) return;
        track(progress);
      } catch {
        if (current !== generation || revision !== revisions.get(task.taskId)) return;
        const count = (failures.get(task.taskId) || 0) + 1;
        failures.set(task.taskId, count);
        // Stop repeated requests for expired tasks; a new streamed update can resume tracking.
        if (count >= 3) snapshots.value[task.taskId] = { ...task, unavailable: true, speed: undefined };
      } finally {
        clearTimeout(timeout);
        requests.delete(controller);
      }
    }));
    if (current !== generation) return;
    polling = false;
    schedule();
  }

  function reset() {
    generation++;
    if (timer) clearTimeout(timer);
    timer = undefined;
    for (const request of requests) request.abort();
    requests.clear();
    polling = false;
    snapshots.value = {};
    revisions.clear();
    failures.clear();
  }

  function beginTurn() {
    for (const task of tasks.value) {
      if (task.completed) delete snapshots.value[task.taskId];
      else if (task.unavailable) {
        snapshots.value[task.taskId] = { ...task, unavailable: false };
        failures.delete(task.taskId);
      }
    }
    schedule();
  }

  function restore(messages: ChatMessage[]) {
    reset();
    for (const message of messages) track(message.taskProgress);
  }

  return { tasks, track, reset, beginTurn, restore };
}
