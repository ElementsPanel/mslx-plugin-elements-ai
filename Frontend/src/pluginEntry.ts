import ElementsAiPage from './views/ElementsAiPage.vue';
import InstanceAiDialog from './views/InstanceAiDialog.vue';

export const pluginConfig = {
  name: 'ElementsAI',
  version: '0.1.0',
  routes: [
    {
      path: '/elements-ai',
      name: 'ElementsAIBase',
      component: 'HOST_LAYOUT',
      meta: { title: 'Elements AI', icon: 'chat', roleCode: ['admin', 'user'] },
      children: [
        {
          path: '',
          name: 'ElementsAIChat',
          component: ElementsAiPage,
          meta: { title: 'Elements AI', hidden: true, roleCode: ['admin', 'user'] },
        },
      ],
    },
  ],
  extensions: [
    {
      slot: 'instance-console-overview-bottom',
      component: InstanceAiDialog,
    },
  ],
};
