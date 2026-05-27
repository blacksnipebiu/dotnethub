
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
    error.value = '项目名称不能为空'
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
    error.value = e.response?.data?.message || '创建失败'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div style="max-width:600px;margin:40px auto">
    <div class="card">
      <h1 class="page-title">新建项目</h1>
      
      <div v-if="error" class="alert alert-error">{{ error }}</div>
      
      <div class="form-group">
        <label>项目名称 *</label>
        <input v-model="name" class="form-input" placeholder="我的 .NET 应用" />
      </div>
      
      <div class="form-group">
        <label>项目描述</label>
        <textarea v-model="description" class="form-input" rows="3" placeholder="简要描述..."></textarea>
      </div>
      
      <div class="form-group">
        <label>端口号</label>
        <input v-model.number="port" class="form-input" type="number" min="1024" max="65535" />
      </div>
      
      <div class="form-group">
        <label>Git 仓库地址（可选）</label>
        <input v-model="gitRepo" class="form-input" placeholder="https://github.com/user/repo.git" />
      </div>
      
      <div class="form-group">
        <label style="display:flex;align-items:center;gap:8px">
          <input v-model="isPublic" type="checkbox" />
          公开项目（所有人可见）
        </label>
      </div>
      
      <div style="display:flex;gap:12px">
        <button class="btn btn-primary" :disabled="submitting" @click="submit">
          {{ submitting ? '创建中...' : '创建项目' }}
        </button>
        <router-link to="/projects" class="btn btn-outline">取消</router-link>
      </div>
    </div>
  </div>
</template>
