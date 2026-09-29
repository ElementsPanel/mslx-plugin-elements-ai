<script setup lang="ts">
import { ref } from 'vue';
import AiWorkspace from '../components/AiWorkspace.vue';

const props = defineProps<{ serverId?: number }>();
const visible = ref(false);

defineExpose({ open: () => { visible.value = true; } });
</script>

<template>
  <t-dialog v-model:visible="visible" header="Elements AI" width="min(1100px, 94vw)" top="4vh" attach="body" :footer="false">
    <AiWorkspace :current-instance-id="props.serverId" compact />
  </t-dialog>
</template>
