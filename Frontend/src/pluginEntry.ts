import { createApp } from 'vue';
import TDesign from 'tdesign-vue-next';
import InstanceAiDialog from './views/InstanceAiDialog.vue';

const pluginVersion = '0.1.12';
const globalRootId = `mslx-elements-ai-global-root-${pluginVersion.replaceAll('.', '-')}`;
const runtimeKey = '__MSLX_ELEMENTS_AI_RUNTIME__';

type ElementsAiRuntime = {
  version: string;
  unmount: () => void;
};

declare global {
  interface Window {
    [runtimeKey]?: ElementsAiRuntime;
  }
}

function mountGlobalLauncher() {
  if (typeof document === 'undefined') return;

  const activeRuntime = window[runtimeKey];
  if (activeRuntime?.version === pluginVersion && document.getElementById(globalRootId)) return;

  // MSLX can load a newly installed plugin asset without recreating the SPA page.
  // Tear down the previous runtime and legacy mount nodes so the new launcher is
  // mounted immediately instead of waiting for a full browser refresh.
  activeRuntime?.unmount();
  document
    .querySelectorAll<HTMLElement>('[id^="mslx-elements-ai-global-root"]')
    .forEach(element => element.remove());
  document
    .querySelectorAll<HTMLElement>('.elements-ai-header-button')
    .forEach(element => element.remove());

  const root = document.createElement('div');
  root.id = globalRootId;
  document.body.appendChild(root);

  const app = createApp(InstanceAiDialog);
  app.use(TDesign);
  app.mount(root);

  window[runtimeKey] = {
    version: pluginVersion,
    unmount: () => {
      app.unmount();
      root.remove();
    },
  };
}

if (typeof document !== 'undefined') {
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', mountGlobalLauncher, { once: true });
  } else {
    queueMicrotask(mountGlobalLauncher);
  }
}

export const pluginConfig = {
  name: 'ElementsAI',
  version: pluginVersion,
};
