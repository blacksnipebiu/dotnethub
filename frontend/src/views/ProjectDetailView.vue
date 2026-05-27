
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProjectsStore, type Project } from '../stores/projects'
import { useAuthStore } from '../stores/auth'
import api from '../api'

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
    error.value = '项目不存在'
  } finally {
    loading.value = false
  }
}

async function upload() {
  if (!fileInput.value?.files?.length || !project.value) return
  message.value = '上传中...'
  try {
    await store.uploadFiles(project.value.id, fileInput.value.files)
    message.value = '文件上传成功！'
  } catch (e: any) {
    error.value = '上传失败'
  }
}

async function build() {
  if (!project.value) return
  message.value = '正在构建...'
  try {
    await store.buildProject(project.value.id)
    message.value = '构建成功！'
    await load()
  } catch (e: any) {
    message.value = '构建失败：' + (e.response?.data?.message || '未知错误')
  }
}

async function deploy() {
  if (!project.value) return
  message.value = '正在部署...'
  try {
    await store.deployProject(project.value.id)
    message.value = '部署成功！'
    await load()
  } catch (e: any) {
    message.value = '部署失败：' + (e.response?.data?.message || '未知错误')
  }
}

async function stop() {
  if (!project.value) return
  try {
    await store.stopProject(project.value.id)
    message.value = '已停止'
    await load()
  } catch (e: any) {
    message.value = '停止失败'
  }
}

async function del() {
  if (!project.value || !confirm('确定要删除此项目吗？此操作不可恢复。')) return
  try {
    await store.deleteProject(project.value.id)
    router.push('/dashboard')
  } catch (e: any) {
    error.value = '删除失败'
  }
}

const canManage = () => {
  if (!project.value) return false
  return auth.user?.username === project.value.ownerName || auth.isAdmin()
}

onMounted(load)
</script>

<template>
  <div v-if="loading" style="text-align:center;padding:80px">加载中...</div>
  <div v-else-if="error" class="alert alert-error">{{ error }}</div>
  <div v-else-if="!project" style="text-align:center;padding:80px">未找到该项目</div>
  <div v-else>
    <div style="display:flex;justify-content:space-between;align-items:start;flex-wrap:wrap;gap:16px">
      <div>
        <h1 class="page-title">{{ project.name }}</h1>
        <p style="color:var(--text-muted)">{{ project.description || '暂无描述' }}</p>
      </div>
      <span :class="'status-badge status-'+project.status" style="font-size:0.9rem">
        {{ project.status === 'running' ? '运行中' : project.status === 'stopped' ? '已停止' : project.status === 'building' ? '构建中' : '异常' }}
      </span>
    </div>

    <div v-if="message" class="alert alert-success mt-16">{{ message }}</div>

    <div class="grid-3 mt-16">
      <div class="card"><strong>端口</strong><br>{{ project.port }}</div>
      <div class="card"><strong>.NET 版本</strong><br>{{ project.dotNetVersion }}</div>
      <div class="card"><strong>创建者</strong><br>{{ project.ownerName }}</div>
      <div class="card"><strong>可见性</strong><br>{{ project.isPublic ? '公开' : '私有' }}</div>
      <div class="card"><strong>Git 仓库</strong><br>{{ project.gitRepo || '无' }}</div>
      <div class="card"><strong>创建时间</strong><br>{{ new Date(project.createdAt).toLocaleDateString() }}</div>
    </div>

    <!-- 操作按钮 -->
    <div v-if="canManage()" class="card mt-16">
      <h3 style="margin-bottom:16px">🔧 操作</h3>
      
      <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
        <input ref="fileInput" type="file" multiple accept=".zip,.cs,.csproj,.sln,.json,.cshtml" style="display:none" @change="upload" />
        <button class="btn btn-outline btn-sm" @click="fileInput?.click()">📁 上传文件</button>
        
        <button class="btn btn-outline btn-sm" @click="build">🔨 构建</button>
        <button class="btn btn-success btn-sm" @click="deploy">🚀 部署</button>
        <button class="btn btn-error btn-sm" @click="stop">⏹ 停止</button>
        <button class="btn btn-error btn-sm" @click="del" style="margin-left:auto">🗑 删除</button>
      </div>
      
      <p v-if="project.status === 'running'" class="mt-16" style="font-size:0.85rem;color:var(--text-muted)">
        🌐 访问地址：<a :href="'http://localhost:'+project.port" target="_blank">http://localhost:{{ project.port }}</a>
      </p>
    </div>
  </div>
</template>
