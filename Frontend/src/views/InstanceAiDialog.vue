<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, shallowRef } from 'vue';
import {
  ChatAddIcon,
  ChatIcon,
  CloseIcon,
  HistoryIcon,
  SettingIcon,
} from 'tdesign-icons-vue-next';
import AiWorkspace from '../components/AiWorkspace.vue';

const props = defineProps<{ serverId?: number }>();
const visible = ref(false);
const headerTarget = shallowRef<HTMLElement | null>(null);
const routeInstanceId = ref<number>();
const workspace = ref<{
  newChat: () => void;
  openHistory: () => Promise<void>;
  openSettings: () => Promise<void>;
  syncInteractions: () => Promise<void>;
} | null>(null);
let headerObserver: MutationObserver | undefined;
const currentInstanceId = computed(() => props.serverId ?? routeInstanceId.value);

async function open() {
  visible.value = true;
  await nextTick();
  await workspace.value?.syncInteractions();
}

function syncHeaderTarget() {
  const match = window.location.pathname.match(/^\/instance\/console\/(\d+)(?:\/|$)/);
  routeInstanceId.value = match ? Number(match[1]) : undefined;

  const operations = document.querySelector<HTMLElement>(
    '.mslx-webpanel-header-layout .t-menu__operations, '
      + '.mslx-webpanel-header-layout .t-head-menu__operations',
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
        <ChatIcon size="18px" />
      </t-button>
    </t-tooltip>
  </Teleport>

  <t-drawer
    v-model:visible="visible"
    placement="right"
    size="min(720px, 100vw)"
    attach="body"
    drawer-class-name="elements-ai-drawer"
    :footer="false"
    :destroy-on-close="false"
  >
    <template #header>
      <div class="drawer-header">
        <span>Elements AI</span>
        <div class="drawer-header-actions">
          <t-tooltip content="新对话" placement="bottom">
            <t-button
              theme="default"
              shape="square"
              variant="text"
              aria-label="新对话"
              @click="workspace?.newChat()"
            >
              <ChatAddIcon size="20px" />
            </t-button>
          </t-tooltip>
          <t-tooltip content="历史" placement="bottom">
            <t-button
              theme="default"
              shape="square"
              variant="text"
              aria-label="历史"
              @click="workspace?.openHistory()"
            >
              <HistoryIcon size="20px" />
            </t-button>
          </t-tooltip>
          <t-tooltip content="设置" placement="bottom">
            <t-button
              theme="default"
              shape="square"
              variant="text"
              aria-label="设置"
              @click="workspace?.openSettings()"
            >
              <SettingIcon size="20px" />
            </t-button>
          </t-tooltip>
          <t-tooltip content="关闭侧边栏" placement="bottom">
            <t-button
              class="drawer-header-close"
              theme="default"
              shape="square"
              variant="text"
              aria-label="关闭侧边栏"
              @click="visible = false"
            >
              <CloseIcon size="20px" />
            </t-button>
          </t-tooltip>
        </div>
      </div>
    </template>
    <div class="instance-ai-sidebar">
      <AiWorkspace ref="workspace" :current-instance-id="currentInstanceId" compact />
    </div>
  </t-drawer>
</template>

<style scoped>
.instance-ai-sidebar {
  height: 100%;
  min-height: 0;
}

.drawer-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  gap: 0.75rem;
}

.drawer-header-actions {
  display: flex;
  align-items: center;
  gap: 0.15rem;
}

/* 分隔线画在按钮外侧，避免 padding/border 改变 square 按钮的固定尺寸与图标居中 */
.drawer-header-close {
  position: relative;
  margin-left: 0.55rem;
}

.drawer-header-close::before {
  content: '';
  position: absolute;
  top: 50%;
  left: -0.28rem;
  width: 1px;
  height: 1.1em;
  background: var(--td-component-border);
  transform: translateY(-50%);
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
  margin-left: 8px;
}
</style>
