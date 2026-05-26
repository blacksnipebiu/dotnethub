
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProjectsStore, type Project } from '../stores/projects'
import { useAuthStore } from '../stores/auth'
import api from '../api'
import { storeToRefs } from 'pinia'

const route = useRoute()
const router = useRouter()
const store = useProjectsStore()
const auth = useAuthStore()

const project = ref<Project | null>(null)
const loading = ref(true)
const error = ref('')
const message = ref('')
const fileInput = ref<HTMLInputElement | null>(null)

async function load() {
  try {
    const { data } = await api.get(`/projects/${route.params.id}`)
    project.value = data
  } catch (e: any) {
    error.value = 'Project not found'
  } finally {
    loading.value = false
  }
}

async function upload() {
  if (!fileInput.value?.files?.length || !project.value) return
  message.value = 'Uploading...'
  try {
    await store.uploadFiles(project.value.id, fileInput.value.files)
    message.value = 'Files uploaded!'
  } catch (e: any) {
    error.value = 'Upload failed'
  }
}

async function build() {
  if (!project.value) return
  message.value = 'Building...'
  try {
    await store.buildProject(project.value.id)
    message.value = 'Build successful!'
    await load()
  } catch (e: any) {
    message.value = 'Build failed: ' + (e.response?.data?.message || 'error')
  }
}

async function deploy() {
  if (!project.value) return
  message.value = 'Deploying...'
  try {
    await store.deployProject(project.value.id)
    message.value = 'Deployed!'
    await load()
  } catch (e: any) {
    message.value = 'Deploy failed: ' + (e.response?.data?.message || 'error')
  }
}

async function stop() {
  if (!project.value) return
  try {
    await store.stopProject(project.value.id)
    message.value = 'Stopped'
    await load()
  } catch (e: any) {
    message.value = 'Stop failed'
  }
}

async function del() {
  if (!project.value || !confirm('Delete this project?')) return
  try {
    await store.deleteProject(project.value.id)
    router.push('/dashboard')
  } catch (e: any) {
    error.value = 'Delete failed'
  }
}

const canManage = () => {
  if (!project.value) return false
  return auth.user?.username === project.value.ownerName || auth.isAdmin()
}

onMounted(load)
</script>

<template>
  <div v-if="loading" style="text-align:center;padding:80px">Loading...</div>
  <div v-else-if="error" class="alert alert-error">{{ error }}</div>
  <div v-else-if="!project" style="text-align:center;padding:80px">Not found</div>
  <div v-else>
    <div style="display:flex;justify-content:space-between;align-items:start;flex-wrap:wrap;gap:16px">
      <div>
        <h1 class="page-title">{{ project.name }}</h1>
        <p style="color:var(--text-muted)">{{ project.description || 'No description' }}</p>
      </div>
      <span :class="'status-badge status-'+project.status" style="font-size:0.9rem">{{ project.status }}</span>
    </div>

    <div v-if="message" class="alert alert-success mt-16">{{ message }}</div>

    <div class="grid-3 mt-16">
      <div class="card"><strong>Port</strong><br>{{ project.port }}</div>
      <div class="card"><strong>.NET</strong><br>{{ project.dotNetVersion }}</div>
      <div class="card"><strong>Owner</strong><br>{{ project.ownerName }}</div>
      <div class="card"><strong>Visibility</strong><br>{{ project.isPublic ? 'Public' : 'Private' }}</div>
      <div class="card"><strong>Git Repo</strong><br>{{ project.gitRepo || 'None' }}</div>
      <div class="card"><strong>Created</strong><br>{{ new Date(project.createdAt).toLocaleDateString() }}</div>
    </div>

    <!-- Actions -->
    <div v-if="canManage()" class="card mt-16">
      <h3 style="margin-bottom:16px">🔧 Actions</h3>
      
      <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
        <!-- Upload -->
        <input ref="fileInput" type="file" multiple accept=".zip,.cs,.csproj,.sln,.json,.cshtml" style="display:none" @change="upload" />
        <button class="btn btn-outline btn-sm" @click="fileInput?.click()">📁 Upload Files</button>
        
        <button class="btn btn-outline btn-sm" @click="build">🔨 Build</button>
        <button class="btn btn-success btn-sm" @click="deploy">🚀 Deploy</button>
        <button class="btn btn-error btn-sm" @click="stop">⏹ Stop</button>
        <button class="btn btn-error btn-sm" @click="del" style="margin-left:auto">🗑 Delete</button>
      </div>
      
      <p v-if="project.status === 'running'" class="mt-16" style="font-size:0.85rem;color:var(--text-muted)">
        🌐 Running at: <a :href="'http://localhost:'+project.port" target="_blank">http://localhost:{{ project.port }}</a>
      </p>
    </div>
  </div>
</template>
