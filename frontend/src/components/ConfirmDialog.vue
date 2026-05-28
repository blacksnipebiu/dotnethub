<script setup lang="ts">
defineProps<{
  show: boolean
  title?: string
  message?: string
}>()

const emit = defineEmits<{
  confirm: []
  cancel: []
}>()
</script>

<template>
  <Teleport to="body">
    <div v-if="show" class="dialog-overlay" @click.self="emit('cancel')">
      <div class="dialog-box">
        <h3 class="dialog-title">{{ title || '确认操作' }}</h3>
        <p class="dialog-msg">{{ message || '确定要执行此操作吗？' }}</p>
        <div class="dialog-actions">
          <button class="btn btn-error btn-sm" @click="emit('confirm')">确定</button>
          <button class="btn btn-outline btn-sm" @click="emit('cancel')">取消</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.dialog-overlay {
  position: fixed; inset: 0; z-index: 9999;
  background: rgba(0,0,0,0.5);
  display: flex; align-items: center; justify-content: center;
}
.dialog-box {
  background: var(--card-bg);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 24px;
  min-width: 340px;
  max-width: 440px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.3);
}
.dialog-title { font-size: 1.05rem; margin: 0 0 12px; }
.dialog-msg { font-size: 0.9rem; color: var(--text-muted); margin: 0 0 20px; }
.dialog-actions { display: flex; gap: 8px; justify-content: flex-end; }
</style>
