<script setup lang="ts">
import { ref } from 'vue'

export interface FileNode {
  name: string
  path: string
  isDirectory: boolean
  size: number
  children: FileNode[] | null
}

const props = defineProps<{
  nodes: FileNode[]
  depth?: number
}>()

const d = props.depth ?? 0
const collapsed = ref<Set<string>>(new Set())

// Initially collapse all folders
function initCollapsed(nodes: FileNode[], set: Set<string>) {
  for (const n of nodes) {
    if (n.isDirectory) {
      set.add(n.path)
      if (n.children) initCollapsed(n.children, set)
    }
  }
}
initCollapsed(props.nodes, collapsed.value)

function toggle(path: string) {
  if (collapsed.value.has(path)) collapsed.value.delete(path)
  else collapsed.value.add(path)
}

function formatSize(bytes: number): string {
  if (!bytes || bytes <= 0) return '0 B'
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / 1048576).toFixed(1) + ' MB'
}
</script>

<template>
  <div v-for="node in nodes" :key="node.path">
    <div
      v-if="node.isDirectory"
      class="tree-row"
      :style="{ paddingLeft: (d * 16) + 'px' }"
      @click="toggle(node.path)"
    >
      <span class="tree-arrow">{{ collapsed.has(node.path) ? '▶' : '▼' }}</span>
      <span class="tree-icon">📁</span>
      <span class="tree-name">{{ node.name }}</span>
    </div>
    <div v-if="!collapsed.has(node.path) && node.children">
      <FileTreeNode :nodes="node.children" :depth="d + 1" />
    </div>
    <div
      v-else
      class="tree-row tree-file"
      :style="{ paddingLeft: (d * 16) + 'px' }"
    >
      <span class="tree-arrow" style="visibility:hidden">▶</span>
      <span class="tree-icon">📄</span>
      <span class="tree-name">{{ node.name }}</span>
      <span class="tree-size">{{ formatSize(node.size) }}</span>
    </div>
  </div>
</template>

<style scoped>
.tree-row {
  display: flex;
  align-items: center;
  padding: 2px 0;
  cursor: default;
  font-family: monospace;
  font-size: 0.85rem;
  line-height: 1.6;
  border-radius: 4px;
  user-select: none;
}
.tree-row:hover {
  background: var(--bg);
}
.tree-arrow {
  width: 16px;
  font-size: 0.7rem;
  color: var(--text-muted);
  flex-shrink: 0;
}
.tree-icon {
  margin-right: 4px;
  flex-shrink: 0;
}
.tree-name {
  color: var(--text);
}
.tree-size {
  margin-left: auto;
  color: var(--text-muted);
  font-size: 0.75rem;
  padding-left: 12px;
}
.tree-file {
  cursor: default;
}
</style>
