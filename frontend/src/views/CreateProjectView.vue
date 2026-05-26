
<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useProjectsStore } from '../stores/projects'

const router = useRouter()
const store = useProjectsStore()

const name = ref('')
const description = ref('')
const port = ref(5000)
const isPublic = ref(false)
const gitRepo = ref('')
const error = ref('')
const submitting = ref(false)

async function submit() {
  if (!name.value.trim()) {
    error.value = 'Name is required'
    return
  }
  submitting.value = true
  error.value = ''
  try {
    const project = await store.createProject({
      name: name.value,
      description: description.value,
      port: port.value,
      isPublic: isPublic.value,
      gitRepo: gitRepo.value || null,
    })
    router.push(`/projects/${project.id}`)
  } catch (e: any) {
    error.value = e.response?.data?.message || 'Failed to create project'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div style="max-width:600px;margin:40px auto">
    <div class="card">
      <h1 class="page-title">Create New Project</h1>
      
      <div v-if="error" class="alert alert-error">{{ error }}</div>
      
      <div class="form-group">
        <label>Project Name *</label>
        <input v-model="name" class="form-input" placeholder="My .NET App" />
      </div>
      
      <div class="form-group">
        <label>Description</label>
        <textarea v-model="description" class="form-input" rows="3" placeholder="Brief description..."></textarea>
      </div>
      
      <div class="form-group">
        <label>Port</label>
        <input v-model.number="port" class="form-input" type="number" min="1024" max="65535" />
      </div>
      
      <div class="form-group">
        <label>Git Repository (optional)</label>
        <input v-model="gitRepo" class="form-input" placeholder="https://github.com/user/repo.git" />
      </div>
      
      <div class="form-group">
        <label style="display:flex;align-items:center;gap:8px">
          <input v-model="isPublic" type="checkbox" />
          Public project (visible to everyone)
        </label>
      </div>
      
      <div style="display:flex;gap:12px">
        <button class="btn btn-primary" :disabled="submitting" @click="submit">
          {{ submitting ? 'Creating...' : 'Create Project' }}
        </button>
        <router-link to="/projects" class="btn btn-outline">Cancel</router-link>
      </div>
    </div>
  </div>
</template>
