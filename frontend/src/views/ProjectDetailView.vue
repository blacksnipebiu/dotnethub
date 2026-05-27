<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProjectsStore, type Project } from '../stores/projects'
import { useAuthStore } from '../stores/auth'
import api from '../api'

const route = useRoute()
const router = useRouter()
const store = useProjectsStore()
const auth = useAuthStore()

const project = ref<Project | null>(null)
const fileTree = ref<any[]>([])
const logs = ref<string[]>([])
const loading = ref(true)
const error = ref('')
const message = ref('')
const fileInput = ref<HTMLInputElement | null>(null)
const logTimer = ref<number | null>(null)
const fileTreeTimer = ref<number | null>(null)

const editingArgs = ref(false)
const startupArgsDraft = ref('')
const editingPort = ref(false)
const portDraft = ref(0)
const activeTab = ref<'files' | 'logs'>('files')

async function load() {
  try {
    const { data } = await api.get(`/projects/${route.params.id}`)
    project.value = data
    startupArgsDraft.value = data.startupArgs || ''
    portDraft.value = data.port || 0
    await loadFileTree()
    await loadLogs()
  } catch (e: any) {
    error.value = '项目不存在'
  } finally {
    loading.value = false
  }
}

async function loadFileTree() {
  try {
    fileTree.value = await store.getFileTree(Number(route.params.id))
  } catch (e) {
    fileTree.value = []
  }
}

async function loadLogs() {
  try {
    const { data } = await api.get(`/projects/${route.params.id}/logs?lines=200`)
    logs.value = Array.isArray(data) ? data : []
  } catch (e) {
    logs.value = []
  }
}

function startLogPolling() {
  stopLogPolling()
  logTimer.value = window.setInterval(loadLogs, 3000)
}

function stopLogPolling() {
  if (logTimer.value) { clearInterval(logTimer.value); logTimer.value = null }
}

async function upload() {
  if (!fileInput.value?.files?.length || !project.value) return

  // Check if project already has files
  let mode = 'overwrite'
  try {
    const { data } = await api.get(`/projects/${project.value.id}/has-files`)
    if (data.hasFiles) {
      const choice = confirm(
        '该项目已有文件，请选择更新方式：\n\n' +
        '【确定】= 覆盖更新（保留同名文件，新增/替换不同文件）\n' +
        '【取消】= 删除后更新（清空所有文件再解压）'
      )
      mode = choice ? 'overwrite' : 'delete'
    }
  } catch (e) { /* proceed with overwrite */ }

  message.value = '上传中...'
  try {
    await store.uploadFiles(project.value.id, fileInput.value.files, mode)
    message.value = '文件上传成功！'
    await loadFileTree()
    startFileTreePolling()
  } catch (e: any) { error.value = '上传失败' }
}

function startFileTreePolling() {
  stopFileTreePolling()
  fileTreeTimer.value = window.setInterval(loadFileTree, 60000)
}
function stopFileTreePolling() {
  if (fileTreeTimer.value) { clearInterval(fileTreeTimer.value); fileTreeTimer.value = null }
}

async function build() {
  if (!project.value) return
  message.value = '正在构建...'
  try {
    await store.buildProject(project.value.id)
    message.value = '构建成功！'
    await load()
  } catch (e: any) {
    message.value = '构建失败：' + (e.response?.data?.message || '')
  }
}

async function deploy() {
  if (!project.value) return
  message.value = '正在部署...'
  try {
    await store.deployProject(project.value.id)
    message.value = '部署成功！'
    await load()
    startLogPolling()
  } catch (e: any) {
    message.value = '部署失败：' + (e.response?.data?.message || '')
  }
}

async function stop() {
  if (!project.value) return
  try {
    await store.stopProject(project.value.id)
    message.value = '已停止'
    stopLogPolling()
    await load()
  } catch (e: any) { message.value = '停止失败' }
}

async function del() {
  if (!project.value || !confirm('确定要删除此项目吗？此操作不可恢复。')) return
  try {
    stopLogPolling()
    stopFileTreePolling()
    await store.deleteProject(project.value.id)
    router.push('/dashboard')
  } catch (e: any) { error.value = '删除失败' }
}

async function saveStartupArgs() {
  if (!project.value) return
  try {
    await store.updateProject(project.value.id, { startupArgs: startupArgsDraft.value })
    project.value.startupArgs = startupArgsDraft.value
    editingArgs.value = false
    message.value = '启动参数已保存'
  } catch (e: any) { error.value = '保存失败' }
}

async function savePort() {
  if (!project.value || !portDraft.value) return
  try {
    await store.updateProject(project.value.id, { port: portDraft.value })
    project.value.port = portDraft.value
    editingPort.value = false
    message.value = '端口已更新（下次部署生效）'
  } catch (e: any) { error.value = '保存失败：' + (e.response?.data?.message || '') }
}

function formatSize(bytes: number): string {
  if (!bytes || bytes <= 0) return '0 B'
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1048576) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / 1048576).toFixed(1) + ' MB'
}

function renderTree(nodes: any[], depth: number = 0): string {
  let out = ''
  for (const n of nodes) {
    const indent = '  '.repeat(depth)
    const name = n.name || '(未知)'
    if (n.isDirectory) {
      out += indent + '\u{1F4C1} ' + name + '/\n'
      if (n.children && n.children.length > 0) out += renderTree(n.children, depth + 1)
    } else {
      out += indent + '\u{1F4C4} ' + name + '  (' + formatSize(n.size || 0) + ')\n'
    }
  }
  return out
}

const canManage = () => {
  if (!project.value) return false
  return auth.user?.username === project.value.ownerName || auth.isAdmin()
}

onMounted(() => {
  load().then(() => {
    if (project.value?.status === 'running') startLogPolling()
  })
})

onUnmounted(() => { stopLogPolling(); stopFileTreePolling() })
</script>

<template>
  <div v-if="loading" style="text-align:center;padding:80px">加载中...</div>
  <div v-else-if="error" class="alert alert-error">{{ error }}</div>
  <div v-else-if="!project" style="text-align:center;padding:80px">未找到该项目</div>
  <div v-else>
    <!-- 标题 -->
    <div style="display:flex;justify-content:space-between;align-items:start;flex-wrap:wrap;gap:16px">
      <div>
        <h1 class="page-title">{{ project.name }}</h1>
        <p style="color:var(--text-muted)">{{ project.description || '暂无描述' }}</p>
      </div>
    </div>

    <div v-if="message" class="alert alert-success mt-16">{{ message }}</div>

    <!-- 🔧 操作按钮 -->
    <div v-if="canManage()" class="card mt-16">
      <div style="display:flex;gap:8px;flex-wrap:wrap;align-items:center">
        <input ref="fileInput" type="file" multiple accept=".zip,.cs,.csproj,.sln,.json,.cshtml" style="display:none" @change="upload" />
        <button class="btn btn-outline btn-sm" @click="fileInput?.click()">📁 上传文件</button>
        <button class="btn btn-outline btn-sm" @click="build">🔨 构建</button>
        <button class="btn btn-success btn-sm" @click="deploy">🚀 部署</button>
        <button class="btn btn-error btn-sm" @click="stop">⏹ 停止</button>
        <button class="btn btn-error btn-sm" @click="del" style="margin-left:auto">🗑 删除</button>
      </div>
    </div>

    <!-- 状态卡片 -->
    <div class="card mt-16">
      <div class="grid-3">
        <div>
          <strong>状态</strong><br>
          <span :class="'status-badge status-'+project.status" style="font-size:0.85rem">
            {{ project.status === 'running' ? '运行中' : project.status === 'stopped' ? '已停止' : project.status === 'building' ? '构建中' : '异常' }}
          </span>
        </div>
        <div v-if="project.processId">
          <strong>进程 PID</strong><br>
          <code style="font-size:1rem">{{ project.processId }}</code>
        </div>
        <div>
          <strong>端口</strong><br>
          <span v-if="!editingPort">{{ project.port }}
            <button v-if="canManage()" class="btn btn-outline btn-sm" style="margin-left:4px;padding:2px 6px;font-size:0.7rem" @click="editingPort = true; portDraft = project.port">改</button>
          </span>
          <span v-else style="display:flex;gap:4px;align-items:center;margin-top:4px">
            <input v-model.number="portDraft" class="form-input" type="number" style="width:80px;padding:4px 8px" min="1024" max="65535" />
            <button class="btn btn-primary btn-sm" style="padding:3px 8px;font-size:0.75rem" @click="savePort">✓</button>
            <button class="btn btn-outline btn-sm" style="padding:3px 8px;font-size:0.75rem" @click="editingPort = false">✕</button>
          </span>
        </div>
      </div>
      <div v-if="project.status === 'running' && project.actualCommand" style="margin-top:12px">
        <strong>执行命令</strong>
        <pre style="background:var(--code-bg);padding:8px 12px;border-radius:6px;font-size:0.82rem;margin:6px 0 0;overflow-x:auto">{{ project.actualCommand }}</pre>
      </div>
      <div v-if="project.status === 'running'" style="margin-top:8px;font-size:0.85rem;color:var(--text-muted)">
        🌐 <a :href="'http://localhost:'+project.port" target="_blank">http://localhost:{{ project.port }}</a>
      </div>
    </div>

    <!-- 部署指令 -->
    <div class="card mt-16">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px">
        <h3>⚙️ 部署指令</h3>
        <button v-if="!editingArgs" class="btn btn-outline btn-sm" @click="editingArgs = true">编辑</button>
      </div>
      <p style="color:var(--text-muted);font-size:0.82rem;margin-bottom:8px">
        自定义启动参数，留空则使用默认端口 <code>--urls http://0.0.0.0:{{ project.port }}</code>
      </p>
      <div v-if="editingArgs">
        <textarea v-model="startupArgsDraft" class="form-input" rows="2" placeholder="--urls http://0.0.0.0:5000" style="font-family:monospace;font-size:0.85rem"></textarea>
        <div style="display:flex;gap:8px;margin-top:8px">
          <button class="btn btn-primary btn-sm" @click="saveStartupArgs">保存</button>
          <button class="btn btn-outline btn-sm" @click="editingArgs = false; startupArgsDraft = project.startupArgs || ''">取消</button>
        </div>
      </div>
      <div v-else>
        <pre style="background:var(--code-bg);padding:10px;border-radius:6px;font-size:0.85rem;margin:0;overflow-x:auto">{{ startupArgsDraft || 'dotnet run -c Release --urls http://0.0.0.0:' + project.port }}</pre>
      </div>
    </div>

    <!-- TabGroup: 文件结构 | 运行日志 -->
    <div class="card mt-16">
      <div class="tab-bar">
        <button :class="['tab-btn', activeTab === 'files' ? 'tab-active' : '']" @click="activeTab = 'files'">📂 文件结构</button>
        <button :class="['tab-btn', activeTab === 'logs' ? 'tab-active' : '']" @click="activeTab = 'logs'">📋 运行日志</button>
      </div>

      <!-- 文件结构 -->
      <div v-if="activeTab === 'files'" style="margin-top:12px">
        <div style="display:flex;gap:8px;align-items:center;margin-bottom:8px">
          <button class="btn btn-outline btn-sm" @click="loadFileTree">🔄 刷新文件树</button>
        </div>
        <pre v-if="fileTree.length > 0" style="background:var(--code-bg);padding:16px;border-radius:8px;font-size:0.85rem;line-height:1.6;overflow-x:auto;margin:0;white-space:pre;font-family:monospace">{{ renderTree(fileTree) }}</pre>
        <div v-else style="text-align:center;padding:40px;color:var(--text-muted)">暂无文件，请先上传项目文件</div>
      </div>

      <!-- 运行日志 -->
      <div v-if="activeTab === 'logs'" style="margin-top:12px">
        <div style="display:flex;gap:8px;align-items:center;margin-bottom:8px">
          <button class="btn btn-outline btn-sm" @click="loadLogs">🔄 刷新日志</button>
          <span v-if="logTimer" style="font-size:0.75rem;color:var(--success)">● 自动刷新中</span>
          <span v-else style="font-size:0.75rem;color:var(--text-muted)">○ 自动刷新已停止</span>
        </div>
        <pre v-if="logs.length > 0" style="background:var(--code-bg);color:var(--text);padding:16px;border-radius:8px;font-size:0.82rem;line-height:1.5;overflow-x:auto;margin:0;max-height:500px;overflow-y:auto;white-space:pre-wrap;word-break:break-all;font-family:monospace">{{ logs.join('\n') }}</pre>
        <div v-else style="text-align:center;padding:40px;color:var(--text-muted)">暂无日志输出</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tab-bar {
  display: flex;
  gap: 0;
  border-bottom: 2px solid var(--border);
  margin-bottom: 0;
}

.tab-btn {
  padding: 10px 20px;
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  color: var(--text-muted);
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s;
  margin-bottom: -2px;
}

.tab-btn:hover { color: var(--text); }

.tab-active {
  color: var(--primary);
  border-bottom-color: var(--primary);
  font-weight: 600;
}
</style>
