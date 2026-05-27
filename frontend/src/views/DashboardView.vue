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

async function deployProject(p: Project) {
  actionMsg.value = `正在部署 ${p.name}...`
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
    <div v-else-if="projects.length === 0" style="text-align:center;padding:60px;color:var(--text-muted)">
      <p style="font-size:1.2rem;margin-bottom:16px">没有任何项目</p>
      <router-link to="/projects/create" class="btn btn-primary">前往创建项目</router-link>
    </div>
    <div v-else class="grid-2">
      <div v-for="p in projects" :key="p.id" class="card" style="cursor:pointer" @click="router.push('/projects/'+p.id)">
        <div style="display:flex;justify-content:space-between;align-items:start">
          <div>
            <h3>{{ p.name }}</h3>
            <p style="color:var(--text-muted);font-size:0.85rem;margin-top:4px">
              {{ p.description || '暂无描述' }}
            </p>
          </div>
          <span :class="'status-badge status-'+p.status">
            {{ p.status === 'running' ? '运行中' : p.status === 'stopped' ? '已停止' : p.status === 'building' ? '构建中' : '异常' }}
          </span>
        </div>
        <div style="display:flex;gap:12px;margin-top:12px;font-size:0.8rem;color:var(--text-muted)">
          <span>🔌 端口 :{{ p.port }}</span>
          <span>.NET {{ p.dotNetVersion }}</span>
          <span>{{ p.isPublic ? '🌐 公开' : '🔒 私有' }}</span>
        </div>
        <div v-if="p.status === 'running'" style="margin-top:8px;font-size:0.85rem;color:var(--text-muted)">
          🌐 <a :href="'http://localhost:'+p.port" target="_blank" @click.stop>http://localhost:{{ p.port }}</a>
        </div>
        <!-- 操作按钮：阻止冒泡以免触发卡片点击 -->
        <div style="display:flex;gap:8px;margin-top:16px" @click.stop>
          <template v-if="p.status === 'running'">
            <button class="btn btn-outline btn-sm" @click="deployProject(p)">🔄 重新部署</button>
            <button class="btn btn-error btn-sm" @click="stopProject(p)">⏹ 停止</button>
          </template>
          <template v-else-if="p.status === 'stopped' || p.status === 'error'">
            <button class="btn btn-success btn-sm" @click="deployProject(p)">🚀 部署</button>
          </template>
          <template v-else>
            <span style="font-size:0.8rem;color:var(--text-muted)">构建中，请稍候...</span>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>
