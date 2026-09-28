import DemoPage from './views/DemoPage.vue';
import './style.css';
import InstanceDropDownItemInject from './views/InstanceDropDownItemInject.vue';
import {GitBranchIcon} from "tdesign-icons-vue-next";
import InstanceCardInject from "./views/InstanceCardInject.vue";

export const pluginConfig = {
    name: 'DemoMenuPlugin',
    version: '1.0.0',

    // 注入路由
    routes: [
        // 插入新的一级菜单
        {
            path: '/plugin-single',
            name: 'PluginSingleBase',
            component: 'HOST_LAYOUT',
            meta: { title: '插件单页', icon: 'app', roleCode: ['admin', 'user'] },
            children: [
                {
                    path: '',
                    name: 'PluginSingle',
                    component: DemoPage,
                    meta: { title: '插件单页', hidden: true, roleCode: ['admin', 'user'] },
                },
            ],
        },

        // 新的一级+二级菜单
        {
            path: '/plugin-multi',
            name: 'PluginMultiBase',
            component: 'HOST_LAYOUT',
            meta: { title: '插件多页', icon: 'layers', roleCode: ['admin', 'user'] },
            children: [
                {
                    path: 'page-a',
                    name: 'PluginMultiA',
                    component: DemoPage,
                    meta: { title: '子页面A', roleCode: ['admin', 'user'] },
                },
                {
                    path: 'page-b',
                    name: 'PluginMultiB',
                    component: DemoPage,
                    meta: { title: '子页面B', roleCode: ['admin', 'user'] },
                },
            ],
        },

        // 插入在原有的一级菜单内
        {
            parentName: 'instance',
            path: 'plugin-extra',
            name: 'PluginNestedSetting',
            component: DemoPage,
            meta: { title: '插件子菜单', icon: 'control-platform', roleCode: ['admin', 'user'] },
        }
    ],
    // 注入组件
    extensions: [
        {
            slot: 'instance-console-dropdown', // 注入到实例控制台更多功能的下拉菜单
            component: InstanceDropDownItemInject, // 弹窗组件
            label: '插件弹窗', // 下拉子菜单名
            icon: GitBranchIcon, // 下拉子菜单Icon
        },
        {
            slot: 'instance-console-overview-bottom', // 注入到实例控制台玩家卡片下方
            component: InstanceCardInject, // 组件
        }
    ]
};