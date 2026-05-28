<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProjectsStore, type Project } from '../stores/projects'
import { useAuthStore } from '../stores/auth'
import FileTreeNode from '../components/FileTreeNode.vue'
import api from '../api'

const CHUNK_SIZE = 2 * 1024 * 1024 // 2MB per chunk

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
const statusTimer = ref<number | null>(null)

const editingArgs = ref(false)
const startupArgsDraft = ref('')
const editingPort = ref(false)
const portDraft = ref(0)
const activeTab = ref<'files' | 'logs'>('files')
const showTree = ref(false)

// Upload progress
const uploading = ref(false)
const uploadProgress = ref(0)
const uploadFileName = ref('')

async function load() {
  try {
    const { data } = await api.get(`/projects/${route.params.id}`)
    project.value = data
    startupArgsDraft.value = data.startupArgs || ''
    portDraft.value = data.port || 0
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
  } catch (e) { fileTree.value = [] }
}

async function loadLogs() {
  try {
    const { data } = await api.get(`/projects/${route.params.id}/logs?lines=200`)
    logs.value = Array.isArray(data) ? data : []
  } catch (e) { logs.value = [] }
}

async function clearLogs() {
  if (!project.value || !confirm('确定清除所有日志？')) return
  try {
    await api.post(`/projects/${project.value.id}/logs/clear`)
    logs.value = []
    message.value = '日志已清除'
  } catch (e) { message.value = '清除失败' }
}

async function refreshStatus() {
  if (!project.value) return
  try {
    const { data } = await api.post(`/projects/${project.value.id}/refresh-status`)
    project.value = data
    if (project.value.status === 'stopped') stopLogPolling()
  } catch (e) { /* silently ignore */ }
}

function startStatusPolling() {
  stopStatusPolling()
  statusTimer.value = window.setInterval(refreshStatus, 15000)
}
function stopStatusPolling() {
  if (statusTimer.value) { clearInterval(statusTimer.value); statusTimer.value = null }
}

function startLogPolling() {
  stopLogPolling()
  logTimer.value = window.setInterval(loadLogs, 3000)
}
function stopLogPolling() {
  if (logTimer.value) { clearInterval(logTimer.value); logTimer.value = null }
}
function startFileTreePolling() {
  stopFileTreePolling()
  fileTreeTimer.value = window.setInterval(loadFileTree, 60000)
}
function stopFileTreePolling() {
  if (fileTreeTimer.value) { clearInterval(fileTreeTimer.value); fileTreeTimer.value = null }
}

// ---- Chunked upload ----
async function upload() {
  if (!fileInput.value?.files?.length || !project.value) return
  const file = fileInput.value.files[0]
  uploadFileName.value = file.name

  // Determine overwrite mode
  let mode = 'overwrite'
  try {
    const { data } = await api.get(`/projects/${project.value.id}/has-files`)
    if (data.hasFiles) {
      const choice = confirm(
        '该项目已有文件，请选择更新方式：\n\n【确定】= 覆盖更新  【取消】= 清空后更新'
      )
      mode = choice ? 'overwrite' : 'delete'
    }
  } catch (e) { /* default overwrite */ }

  uploading.value = true
  uploadProgress.value = 0

  const totalChunks = Math.ceil(file.size / CHUNK_SIZE)

  // If 'delete' mode, clear first
  if (mode === 'delete') {
    try { await api.post(`/projects/${project.value.id}/upload-chunk/clear`) } catch (e) {}
  }

  // Check resume status
  let startChunk = 0
  try {
    const { data } = await api.get(`/projects/${project.value.id}/upload-status`, {
      params: { fileName: file.name, totalChunks }
    })
    if (data.receivedChunks && data.receivedChunks.length > 0 && !data.complete) {
      const r = confirm(`检测到未完成的上传，已接收 ${data.receivedChunks.length}/${totalChunks} 块。\n【确定】= 继续上传  【取消】= 重新开始`)
      if (r) {
        startChunk = Math.min(...data.receivedChunks) > 0 ? Math.min(...data.receivedChunks) : 0
      }
    }
  } catch (e) { /* start from 0 */ }

  for (let i = startChunk; i < totalChunks; i++) {
    const start = i * CHUNK_SIZE
    const end = Math.min(start + CHUNK_SIZE, file.size)
    const chunk = file.slice(start, end)
    
    const formData = new FormData()
    formData.append('file', chunk, file.name)
    formData.append('chunkIndex', String(i))
    formData.append('totalChunks', String(totalChunks))
    formData.append('fileName', file.name)

    try {
      await api.post(`/projects/${project.value.id}/upload-chunk`, formData)
      uploadProgress.value = Math.round(((i + 1) / totalChunks) * 100)
    } catch (e: any) {
      error.value = `上传失败（块 ${i + 1}/${totalChunks}）：${e.response?.data?.message || e.message}`
      uploading.value = false
      return
    }
  }

  uploading.value = false
  message.value = `上传完成！正在解压...`

  // Trigger combine & extract
  try {
    await api.post(`/projects/${project.value.id}/upload-chunk/combine`, {
      fileName: file.name,
      totalChunks
    })
    message.value = '上传并解压成功！'
    await loadFileTree()
    showTree.value = true
    startFileTreePolling()
  } catch (e: any) {
    error.value = '解压失败：' + (e.response?.data?.message || '')
  }
}

async function build() {
  if (!project.value) return
  message.value = '正在构建...'
  try {
    await store.buildProject(project.value.id)
    message.value = '构建成功！'
    await load()
  } catch (e: any) { message.value = '构建失败：' + (e.response?.data?.message || '') }
}

async function deploy() {
  if (!project.value) return
  message.value = '正在部署...'
  try {
    await store.deployProject(project.value.id)
    message.value = '部署成功！'
    await load()
    startLogPolling()
    startStatusPolling()
  } catch (e: any) { message.value = '部署失败：' + (e.response?.data?.message || '') }
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
    if (project.value?.status === 'running') { startLogPolling(); startStatusPolling() }
  })
})

onUnmounted(() => { stopLogPolling(); stopFileTreePolling(); stopStatusPolling() })
</script>

<template>
  <div v-if="loading" style="text-align:center;padding:80px">加载中...</div>
  <div v-else-if="error" class="alert alert-error">{{ error }}</div>
  <div v-else-if="!project" style="text-align:center;padding:80px">未找到该项目</div>
  <div v-else class="detail-layout">
    <!-- ========== 左侧列 ========== -->
    <div class="left-col">
      <!-- 标题 -->
      <div>
        <h1 class="page-title">{{ project.name }}</h1>
        <p style="color:var(--text-muted)">{{ project.description || '暂无描述' }}</p>
      </div>

      <div v-if="message" class="alert alert-success mt-16">{{ message }}</div>

      <!-- 🔧 操作按钮 -->
      <div v-if="canManage()" class="card mt-16">
        <div style="display:flex;gap:6px;flex-wrap:wrap;align-items:center">
          <input ref="fileInput" type="file" accept=".zip" style="display:none" @change="upload" />
          <button class="btn btn-outline btn-sm" :disabled="uploading" @click="fileInput?.click()">
            {{ uploading ? '上传中...' : '📁 上传文件' }}
          </button>
          <button class="btn btn-outline btn-sm" @click="build">🔨 构建</button>
          <button class="btn btn-success btn-sm" @click="deploy">🚀 部署</button>
          <button class="btn btn-error btn-sm" @click="stop">⏹ 停止</button>
          <button class="btn btn-error btn-sm" @click="del" style="margin-left:auto">🗑 删除</button>
        </div>
        <div v-if="uploading" style="margin-top:12px">
          <div style="display:flex;align-items:center;gap:8px;margin-bottom:4px">
            <span style="font-size:0.85rem">上传中: {{ uploadFileName }}</span>
            <span style="font-size:0.8rem;color:var(--primary);font-weight:600">{{ uploadProgress }}%</span>
          </div>
          <div style="background:var(--border);height:6px;border-radius:3px;overflow:hidden">
            <div :style="{ width: uploadProgress + '%', height:'100%', background:'var(--primary)', transition:'width 0.3s' }"></div>
          </div>
        </div>
      </div>

      <!-- 状态卡片 -->
      <div class="card mt-16">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:8px">
          <strong>运行状态</strong>
          <button class="btn btn-outline btn-sm" @click="refreshStatus">🔄 刷新状态</button>
        </div>
        <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px">
          <div><strong>状态</strong><br><span :class="'status-badge status-'+project.status" style="font-size:0.85rem">{{ project.status === 'running' ? '运行中' : project.status === 'stopped' ? '已停止' : project.status === 'building' ? '构建中' : '异常' }}</span></div>
          <div v-if="project.processId"><strong>PID</strong><br><code>{{ project.processId }}</code></div>
          <div><strong>端口</strong><br>
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
        <div v-if="project.status === 'running' && project.actualCommand" style="margin-top:8px">
          <strong>执行命令</strong>
          <pre style="background:var(--code-bg);padding:6px 10px;border-radius:6px;font-size:0.8rem;margin:4px 0 0;overflow-x:auto">{{ project.actualCommand }}</pre>
        </div>
        <div v-if="project.status === 'running'" style="margin-top:6px;font-size:0.8rem;color:var(--text-muted)">
          🌐 <a :href="'http://localhost:'+project.port" target="_blank">http://localhost:{{ project.port }}</a>
        </div>
      </div>

      <!-- 启动命令 -->
      <div class="card mt-16">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px">
          <h3 style="font-size:0.95rem">⚙️ 启动命令</h3>
          <button v-if="!editingArgs" class="btn btn-outline btn-sm" @click="editingArgs = true">编辑</button>
        </div>
        <p style="color:var(--text-muted);font-size:0.75rem;margin-bottom:6px">端口 <code>--urls :{{ project.port }}</code> 自动追加</p>
        <div v-if="editingArgs">
          <textarea v-model="startupArgsDraft" class="form-input" rows="2" placeholder="dotnet MyApp.dll" style="font-family:monospace;font-size:0.8rem"></textarea>
          <div style="display:flex;gap:6px;margin-top:6px">
            <button class="btn btn-primary btn-sm" @click="saveStartupArgs">保存</button>
            <button class="btn btn-outline btn-sm" @click="editingArgs = false; startupArgsDraft = project.startupArgs || ''">取消</button>
          </div>
        </div>
        <pre v-else style="background:var(--code-bg);padding:8px;border-radius:6px;font-size:0.8rem;margin:0;overflow-x:auto;color:var(--text)">{{ startupArgsDraft || '（未设置）' }}</pre>
      </div>
    </div>

    <!-- ========== 右侧列: 文件 + 日志 ========== -->
    <div class="right-col">
      <div class="card" style="display:flex;flex-direction:column;height:100%;min-height:0">
        <div class="tab-bar">
          <button :class="['tab-btn', activeTab === 'files' ? 'tab-active' : '']" @click="activeTab = 'files'">📂 文件</button>
          <button :class="['tab-btn', activeTab === 'logs' ? 'tab-active' : '']" @click="activeTab = 'logs'">📋 日志</button>
        </div>

        <!-- 文件结构 -->
        <div v-if="activeTab === 'files'" style="margin-top:8px;display:flex;flex-direction:column;flex:1;min-height:0">
          <div style="display:flex;gap:6px;align-items:center;margin-bottom:6px;flex-shrink:0">
            <button class="btn btn-outline btn-sm" @click="loadFileTree(); showTree = true">🔄 刷新</button>
            <button v-if="showTree && fileTree.length > 0" class="btn btn-outline btn-sm" @click="showTree = false">🙈 隐藏</button>
          </div>
          <div v-if="showTree && fileTree.length > 0" style="flex:1;overflow-y:auto;min-height:0;border:1px solid var(--border);border-radius:6px;padding:6px">
            <FileTreeNode :nodes="fileTree" />
          </div>
          <div v-else-if="!showTree" style="flex:1;display:flex;align-items:center;justify-content:center;color:var(--text-muted)">点击「刷新」加载</div>
          <div v-else style="flex:1;display:flex;align-items:center;justify-content:center;color:var(--text-muted)">暂无文件</div>
        </div>

        <!-- 运行日志 -->
        <div v-if="activeTab === 'logs'" style="margin-top:8px;display:flex;flex-direction:column;flex:1;min-height:0">
          <div style="display:flex;gap:6px;align-items:center;margin-bottom:6px;flex-shrink:0">
            <button class="btn btn-outline btn-sm" @click="loadLogs">🔄 刷新</button>
            <button class="btn btn-outline btn-sm" @click="clearLogs">🗑 清除</button>
            <span v-if="logTimer" style="font-size:0.7rem;color:var(--success)">● 自动</span>
            <span v-else style="font-size:0.7rem;color:var(--text-muted)">○ 已停</span>
          </div>
          <pre v-if="logs.length > 0" style="flex:1;overflow:auto;min-height:0;background:var(--code-bg);color:var(--text);padding:10px;border-radius:6px;font-size:0.8rem;line-height:1.4;margin:0;white-space:pre-wrap;word-break:break-all;font-family:monospace">{{ logs.join('\n') }}</pre>
          <div v-else style="flex:1;display:flex;align-items:center;justify-content:center;color:var(--text-muted)">暂无日志</div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.detail-layout {
  display: flex;
  gap: 16px;
  align-items: flex-start;
}
.left-col {
  width: 380px;
  min-width: 300px;
  flex-shrink: 0;
}
.right-col {
  flex: 1;
  min-width: 0;
  height: calc(100vh - 100px);
  position: sticky;
  top: 80px;
}
.tab-bar { display: flex; border-bottom: 2px solid var(--border); flex-shrink: 0; }
.tab-btn { padding: 8px 16px; background: none; border: none; border-bottom: 2px solid transparent; color: var(--text-muted); font-size: 0.85rem; cursor: pointer; margin-bottom: -2px; }
.tab-btn:hover { color: var(--text); }
.tab-active { color: var(--primary); border-bottom-color: var(--primary); font-weight: 600; }
</style>
