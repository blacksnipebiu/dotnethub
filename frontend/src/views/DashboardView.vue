
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useProjectsStore, type Project } from '../stores/projects'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'

const store = useProjectsStore()
const { projects, loading } = storeToRefs(store)
const router = useRouter()
const actionMsg = ref('')

onMounted(() => store.fetchProjects())

const runningProjects = () => projects.value.filter(p => p.status === 'running')

async function stopProject(p: Project) {
  actionMsg.value = `正在停止 ${p.name}...`
  try {
    await store.stopProject(p.id)
    actionMsg.value = `${p.name} 已停止`
    await store.fetchProjects()
  } catch (e) {
    actionMsg.value = `停止失败`
  }
}

async function redeploy(p: Project) {
  actionMsg.value = `正在重新部署 ${p.name}...`
  try {
    await store.deployProject(p.id)
    actionMsg.value = `${p.name} 已部署`
    await store.fetchProjects()
  } catch (e) {
    actionMsg.value = `部署失败`
  }
}
</script>

<template>
  <div>
    <h1 class="page-title">控制台</h1>

    <div v-if="actionMsg" class="alert alert-success mb-16">{{ actionMsg }}</div>

    <div v-if="loading" style="text-align:center;padding:40px">加载中...</div>
    <div v-else-if="runningProjects().length === 0" style="text-align:center;padding:60px;color:var(--text-muted)">
      <p style="font-size:1.2rem;margin-bottom:16px">当前没有运行中的项目</p>
      <router-link to="/projects" class="btn btn-primary">前往项目管理</router-link>
    </div>
    <div v-else class="grid-2">
      <div v-for="p in runningProjects()" :key="p.id" class="card">
        <div style="display:flex;justify-content:space-between;align-items:start">
          <div>
            <h3 style="cursor:pointer" @click="router.push('/projects/'+p.id)">{{ p.name }}</h3>
            <p style="color:var(--text-muted);font-size:0.85rem;margin-top:4px">
              {{ p.description || '暂无描述' }}
            </p>
          </div>
          <span class="status-badge status-running">运行中</span>
        </div>
        <div style="display:flex;gap:12px;margin-top:12px;font-size:0.8rem;color:var(--text-muted)">
          <span>🔌 端口 :{{ p.port }}</span>
          <span>.NET {{ p.dotNetVersion }}</span>
          <span>{{ p.isPublic ? '🌐 公开' : '🔒 私有' }}</span>
        </div>
        <div v-if="p.status === 'running'" style="margin-top:12px;font-size:0.85rem;color:var(--text-muted)">
          🌐 <a :href="'http://localhost:'+p.port" target="_blank">http://localhost:{{ p.port }}</a>
        </div>
        <div style="display:flex;gap:8px;margin-top:16px">
          <button class="btn btn-outline btn-sm" @click="redeploy(p)">🔄 重新部署</button>
          <button class="btn btn-error btn-sm" @click="stopProject(p)">⏹ 停止</button>
          <button class="btn btn-outline btn-sm" @click="router.push('/projects/'+p.id)">📋 详情</button>
        </div>
      </div>
    </div>
  </div>
</template>
