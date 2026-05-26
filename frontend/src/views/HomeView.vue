
<script setup lang="ts">
import { onMounted } from 'vue'
import { useProjectsStore, type Project } from '../stores/projects'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'

const store = useProjectsStore()
const { projects, loading } = storeToRefs(store)
const router = useRouter()

onMounted(() => store.fetchProjects(true))
</script>

<template>
  <div>
    <div style="text-align:center;padding:60px 0">
      <h1 style="font-size:2.5rem;margin-bottom:16px">🚀 DotNetHub</h1>
      <p style="font-size:1.1rem;color:var(--text-muted);margin-bottom:32px">
        AI-powered .NET application hosting platform — upload, build, deploy, and manage with ease.
      </p>
      <div style="display:flex;gap:12px;justify-content:center">
        <router-link to="/projects" class="btn btn-primary btn-lg">Browse Projects</router-link>
        <router-link to="/login" class="btn btn-outline btn-lg">Get Started</router-link>
      </div>
    </div>

    <h2 style="margin-bottom:16px">📦 Public Projects</h2>
    <div v-if="loading" style="text-align:center;padding:40px">Loading...</div>
    <div v-else-if="projects.length === 0" style="text-align:center;padding:40px;color:var(--text-muted)">
      No public projects yet.
    </div>
    <div v-else class="grid-3">
      <div v-for="p in projects" :key="p.id" class="card" style="cursor:pointer" @click="router.push('/projects/'+p.id)">
        <h3 style="margin-bottom:8px">{{ p.name }}</h3>
        <p style="color:var(--text-muted);font-size:0.85rem;margin-bottom:8px">{{ p.description || 'No description' }}</p>
        <div style="display:flex;gap:8px;align-items:center">
          <span :class="'status-badge status-'+p.status">{{ p.status }}</span>
          <span style="font-size:0.8rem;color:var(--text-muted)">:{{ p.port }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
