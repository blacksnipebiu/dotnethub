
<script setup lang="ts">
import { onMounted } from 'vue'
import { useProjectsStore } from '../stores/projects'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'

const store = useProjectsStore()
const { projects, loading } = storeToRefs(store)
const router = useRouter()

onMounted(() => store.fetchProjects())
</script>

<template>
  <div>
    <div style="text-align:center;padding:60px 0">
      <h1 style="font-size:2.5rem;margin-bottom:16px">🚀 DotNetHub</h1>
      <p style="font-size:1.1rem;color:var(--text-muted);margin-bottom:32px">
        AI 驱动的 .NET 应用托管平台 — 上传、构建、部署、管理，一站式服务
      </p>
      <div style="display:flex;gap:12px;justify-content:center">
        <router-link to="/projects" class="btn btn-primary">浏览项目</router-link>
        <router-link to="/projects/create" class="btn btn-outline">新建项目</router-link>
      </div>
    </div>

    <h2 style="margin-bottom:16px">📦 我的项目</h2>
    <div v-if="loading" style="text-align:center;padding:40px">加载中...</div>
    <div v-else-if="projects.length === 0" style="text-align:center;padding:40px;color:var(--text-muted)">
      暂无项目，<router-link to="/projects/create">点击创建</router-link>
    </div>
    <div v-else class="grid-3">
      <div v-for="p in projects" :key="p.id" class="card" style="cursor:pointer" @click="router.push('/projects/'+p.id)">
        <h3 style="margin-bottom:8px">{{ p.name }}</h3>
        <p style="color:var(--text-muted);font-size:0.85rem;margin-bottom:8px">{{ p.description || '暂无描述' }}</p>
        <div style="display:flex;gap:8px;align-items:center">
          <span :class="'status-badge status-'+p.status">{{ p.status }}</span>
          <span style="font-size:0.8rem;color:var(--text-muted)">端口 :{{ p.port }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
