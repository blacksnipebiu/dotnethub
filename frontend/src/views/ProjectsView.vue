
<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useProjectsStore } from '../stores/projects'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const store = useProjectsStore()
const auth = useAuthStore()
const { projects, loading } = storeToRefs(store)
const router = useRouter()

onMounted(() => store.fetchProjects())
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:12px" class="mb-16">
      <h1 class="page-title" style="margin:0">项目管理</h1>
      <router-link to="/projects/create" class="btn btn-primary">+ 新建项目</router-link>
    </div>

    <div v-if="loading" style="text-align:center;padding:40px">加载中...</div>
    <div v-else class="grid-2">
      <div v-for="p in projects" :key="p.id" class="card" style="cursor:pointer" @click="router.push('/projects/'+p.id)">
        <div style="display:flex;justify-content:space-between;align-items:start">
          <div>
            <h3>{{ p.name }}</h3>
            <p style="color:var(--text-muted);font-size:0.85rem;margin-top:4px">
              {{ p.description || '暂无描述' }}
            </p>
          </div>
          <span :class="'status-badge status-'+p.status">{{ p.status }}</span>
        </div>
        <div style="display:flex;gap:12px;margin-top:12px;font-size:0.8rem;color:var(--text-muted)">
          <span>👤 {{ p.ownerName || '匿名' }}</span>
          <span>🔌 端口 :{{ p.port }}</span>
          <span v-if="p.isPublic">🌐 公开</span>
        </div>
      </div>
    </div>
  </div>
</template>
