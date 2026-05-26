
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
const filter = ref<'all'|'public'|'mine'>('all')

onMounted(() => store.fetchProjects())
</script>

<template>
  <div>
    <div style="display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:12px" class="mb-16">
      <h1 class="page-title" style="margin:0">All Projects</h1>
      <div style="display:flex;gap:8px">
        <select v-model="filter" class="form-input" style="width:auto">
          <option value="all">All Projects</option>
          <option value="public">Public Only</option>
          <option value="mine">My Projects</option>
        </select>
        <router-link v-if="auth.isLoggedIn()" to="/projects/create" class="btn btn-primary">+ New</router-link>
      </div>
    </div>

    <div v-if="loading" style="text-align:center;padding:40px">Loading...</div>
    <div v-else class="grid-2">
      <div v-for="p in projects" :key="p.id" class="card" style="cursor:pointer" @click="router.push('/projects/'+p.id)">
        <div style="display:flex;justify-content:space-between;align-items:start">
          <div>
            <h3>{{ p.name }}</h3>
            <p style="color:var(--text-muted);font-size:0.85rem;margin-top:4px">
              {{ p.description || 'No description' }}
            </p>
          </div>
          <span :class="'status-badge status-'+p.status">{{ p.status }}</span>
        </div>
        <div style="display:flex;gap:12px;margin-top:12px;font-size:0.8rem;color:var(--text-muted)">
          <span>👤 {{ p.ownerName || 'anon' }}</span>
          <span>🔌 :{{ p.port }}</span>
          <span v-if="p.isPublic">🌐 Public</span>
        </div>
      </div>
    </div>
  </div>
</template>
