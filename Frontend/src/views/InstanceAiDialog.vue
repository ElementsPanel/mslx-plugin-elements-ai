<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref, shallowRef } from 'vue';
import { ChatIcon } from 'tdesign-icons-vue-next';
import AiWorkspace from '../components/AiWorkspace.vue';

const props = defineProps<{ serverId?: number }>();
const visible = ref(false);
const headerTarget = shallowRef<HTMLElement | null>(null);
let headerObserver: MutationObserver | undefined;

function open() {
  visible.value = true;
}

function syncHeaderTarget() {
  const operations = document.querySelector<HTMLElement>(
    '.mslx-webpanel-header-layout .t-head-menu__operations',
  );
  const next = operations?.firstElementChild instanceof HTMLElement
    ? operations.firstElementChild
    : operations;
  if (headerTarget.value !== next) headerTarget.value = next;
}

onMounted(async () => {
  await nextTick();
  syncHeaderTarget();
  headerObserver = new MutationObserver(syncHeaderTarget);
  headerObserver.observe(document.body, { childList: true, subtree: true });
});

onBeforeUnmount(() => headerObserver?.disconnect());

defineExpose({ open });
</script>

<template>
  <Teleport v-if="headerTarget" :to="headerTarget">
    <t-tooltip content="询问 Elements AI" placement="bottom">
      <t-button
        class="header-btn elements-ai-header-button"
        theme="default"
        shape="square"
        variant="text"
        aria-label="询问 Elements AI"
        @click="open"
      >
        <ChatIcon size="20px" />
      </t-button>
    </t-tooltip>
  </Teleport>

  <t-drawer
    v-model:visible="visible"
    header="Elements AI"
    placement="right"
    size="min(720px, 100vw)"
    attach="body"
    drawer-class-name="elements-ai-drawer"
    :footer="false"
  >
    <div class="instance-ai-sidebar">
      <AiWorkspace :current-instance-id="props.serverId" compact />
    </div>
  </t-drawer>
</template>

<style scoped>
.instance-ai-sidebar {
  height: 100%;
  min-height: 0;
}

.instance-ai-sidebar :deep(.ai-workspace) {
  height: 100%;
  min-height: 0;
  border-radius: 0;
}

:global(.elements-ai-drawer .t-drawer__body) {
  padding: 0;
  overflow: hidden;
}

:global(.elements-ai-header-button) {
  flex: 0 0 auto;
}
</style>
