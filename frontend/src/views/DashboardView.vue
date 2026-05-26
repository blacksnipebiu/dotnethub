
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
    <div style="display:flex;justify-content:space-between;align-items:center" class="mb-16">
      <h1 class="page-title">My Dashboard</h1>
      <router-link to="/projects/create" class="btn btn-primary">+ New Project</router-link>
    </div>

    <div v-if="loading" style="text-align:center;padding:40px">Loading...</div>
    <div v-else-if="projects.length === 0" style="text-align:center;padding:60px;color:var(--text-muted)">
      <p style="font-size:1.2rem;margin-bottom:16px">No projects yet</p>
      <router-link to="/projects/create" class="btn btn-primary">Create Your First Project</router-link>
    </div>
    <div v-else class="grid-2">
      <div v-for="p in projects" :key="p.id" class="card" style="cursor:pointer" @click="router.push('/projects/'+p.id)">
        <div style="display:flex;justify-content:space-between;align-items:start">
          <div>
            <h3>{{ p.name }}</h3>
            <p style="color:var(--text-muted);font-size:0.85rem;margin-top:4px">{{ p.description || 'No description' }}</p>
          </div>
          <span :class="'status-badge status-'+p.status">{{ p.status }}</span>
        </div>
        <div style="display:flex;gap:12px;margin-top:12px;font-size:0.8rem;color:var(--text-muted)">
          <span>🔌 :{{ p.port }}</span>
          <span>.NET {{ p.dotNetVersion }}</span>
          <span>{{ p.isPublic ? '🌐 Public' : '🔒 Private' }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
