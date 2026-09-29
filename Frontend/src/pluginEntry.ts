import InstanceAiDialog from './views/InstanceAiDialog.vue';

export const pluginConfig = {
  name: 'ElementsAI',
  version: '0.1.1',
  extensions: [
    {
      slot: 'instance-console-overview-bottom',
      component: InstanceAiDialog,
    },
  ],
};
