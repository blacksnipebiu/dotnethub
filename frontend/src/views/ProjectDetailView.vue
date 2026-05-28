<template>
  <div v-if="loading" style="text-align:center;padding:80px">加载中...</div>
  <div v-else-if="error" class="alert alert-error">{{ error }}</div>
  <div v-else-if="!project" style="text-align:center;padding:80px">未找到该项目</div>
  <div v-else class="detail-layout">
    <!-- ========== 左侧列 ========== -->
    <div class="left-col">
      <div v-if="message" class="alert alert-success">{{ message }}</div>

      <!-- 状态卡片 -->
      <div class="card">
        <!-- 项目名 + 操作按钮同一行 -->
        <div style="display:flex;justify-content:space-between;align-items:center;gap:8px">
          <div style="min-width:0">
            <h1 style="font-size:1.1rem;font-weight:700;margin:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{{ project.name }}</h1>
            <p style="color:var(--text-muted);font-size:0.75rem;margin:2px 0 0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{{ project.description || '暂无描述' }}</p>
          </div>
          <div v-if="canManage()" style="display:flex;gap:4px;flex-shrink:0">
            <button class="btn btn-success btn-sm" @click="deploy">🚀 部署</button>
            <button v-if="project.status === 'running'" class="btn btn-error btn-sm" @click="stop">⏹ 停止</button>
            <button class="btn btn-error btn-sm" @click="del">🗑</button>
          </div>
        </div>

        <!-- 状态/PID/端口 同一行 -->
        <div style="display:flex;align-items:center;gap:12px;margin-top:10px;flex-wrap:wrap">
          <span :class="'status-badge status-'+project.status" style="font-size:0.8rem">{{ project.status === 'running' ? '运行中' : project.status === 'stopped' ? '已停止' : project.status === 'building' ? '构建中' : '异常' }}</span>
          <span v-if="project.processId" style="font-size:0.8rem;color:var(--text-muted)">PID <code style="font-size:0.85rem">{{ project.processId }}</code></span>
          <span style="font-size:0.8rem;color:var(--text-muted)">
            端口
            <span v-if="!editingPort">{{ project.port }} <button v-if="canManage()" class="btn btn-outline btn-sm" style="padding:1px 5px;font-size:0.65rem;vertical-align:middle" @click="editingPort = true; portDraft = project.port">改</button></span>
            <span v-else>
              <input v-model.number="portDraft" type="number" style="width:60px;padding:2px 4px;font-size:0.75rem;border:1px solid var(--border);border-radius:4px" min="1024" max="65535" />
              <button class="btn btn-primary btn-sm" style="padding:1px 5px;font-size:0.65rem" @click="savePort">✓</button>
              <button class="btn btn-outline btn-sm" style="padding:1px 5px;font-size:0.65rem" @click="editingPort = false">✕</button>
            </span>
          </span>
          <button class="btn btn-outline btn-sm" style="padding:2px 8px;font-size:0.7rem" @click="refreshStatus">🔄</button>
        </div>

        <div v-if="project.status === 'running' && project.actualCommand" style="margin-top:8px">
          <pre style="background:var(--code-bg);padding:6px 10px;border-radius:6px;font-size:0.78rem;margin:0;overflow-x:auto;white-space:pre-wrap;word-break:break-all">{{ project.actualCommand }}</pre>
        </div>
        <div v-if="project.status === 'running'" style="margin-top:4px;font-size:0.75rem;color:var(--text-muted)">
          🌐 <a :href="'http://localhost:'+project.port" target="_blank">http://localhost:{{ project.port }}</a>
        </div>
      </div>

      <!-- 启动命令 -->
      <div class="card mt-16">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:4px">
          <h3 style="font-size:0.9rem;margin:0">⚙️ 启动命令</h3>
          <button v-if="!editingArgs" class="btn btn-outline btn-sm" @click="editingArgs = true">编辑</button>
        </div>
        <div v-if="editingArgs">
          <textarea v-model="startupArgsDraft" class="form-input" rows="2" placeholder="dotnet MyApp.dll" style="font-family:monospace;font-size:0.8rem"></textarea>
          <div style="display:flex;gap:6px;margin-top:6px">
            <button class="btn btn-primary btn-sm" @click="saveStartupArgs">保存</button>
            <button class="btn btn-outline btn-sm" @click="editingArgs = false; startupArgsDraft = project.startupArgs || ''">取消</button>
          </div>
        </div>
        <pre v-else style="background:var(--code-bg);padding:8px;border-radius:6px;font-size:0.8rem;margin:0;overflow-x:auto;color:var(--text)">{{ startupArgsDraft || '（未设置）' }}</pre>
      </div>

      <!-- Upload progress -->
      <div v-if="uploading" class="card mt-16">
        <div style="display:flex;align-items:center;gap:8px;margin-bottom:4px">
          <span style="font-size:0.85rem">上传中: {{ uploadFileName }}</span>
          <span style="font-size:0.8rem;color:var(--primary);font-weight:600">{{ uploadProgress }}%</span>
        </div>
        <div style="background:var(--border);height:6px;border-radius:3px;overflow:hidden">
          <div :style="{ width: uploadProgress + '%', height:'100%', background:'var(--primary)', transition:'width 0.3s' }"></div>
        </div>
      </div>
    </div>

    <!-- ========== 右侧列 ========== -->
    <div class="right-col">
      <div class="card" style="display:flex;flex-direction:column;height:100%;min-height:0;overflow:hidden">
        <div class="tab-bar">
          <button :class="['tab-btn', activeTab === 'files' ? 'tab-active' : '']" @click="activeTab = 'files'">📂 文件</button>
          <button :class="['tab-btn', activeTab === 'logs' ? 'tab-active' : '']" @click="activeTab = 'logs'">📋 日志</button>
        </div>

        <div v-if="activeTab === 'files'" style="margin-top:8px;display:flex;flex-direction:column;flex:1;min-height:0">
          <div style="display:flex;gap:6px;align-items:center;margin-bottom:6px;flex-shrink:0">
            <input ref="fileInput" type="file" accept=".zip" style="display:none" @change="upload" />
            <button class="btn btn-outline btn-sm" :disabled="uploading" @click="fileInput?.click()">📁 上传</button>
            <button class="btn btn-outline btn-sm" @click="loadFileTree(); showTree = true">🔄 刷新</button>
            <button v-if="showTree && fileTree.length > 0" class="btn btn-outline btn-sm" @click="showTree = false">🙈 隐藏</button>
          </div>
          <div v-if="showTree && fileTree.length > 0" style="flex:1;overflow-y:auto;min-height:0;border:1px solid var(--border);border-radius:6px;padding:6px">
            <FileTreeNode :nodes="fileTree" />
          </div>
          <div v-else-if="!showTree" style="flex:1;display:flex;align-items:center;justify-content:center;color:var(--text-muted)">点击「刷新」加载</div>
          <div v-else style="flex:1;display:flex;align-items:center;justify-content:center;color:var(--text-muted)">暂无文件</div>
        </div>

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
.detail-layout { display: flex; gap: 16px; height: calc(100vh - 100px); overflow: hidden; }
.left-col { width: 380px; min-width: 300px; flex-shrink: 0; overflow-y: auto; height: 100%; }
.right-col { flex: 1; min-width: 0; height: 100%; overflow: hidden; }
.tab-bar { display: flex; border-bottom: 2px solid var(--border); flex-shrink: 0; }
.tab-btn { padding: 8px 16px; background: none; border: none; border-bottom: 2px solid transparent; color: var(--text-muted); font-size: 0.85rem; cursor: pointer; margin-bottom: -2px; }
.tab-btn:hover { color: var(--text); }
.tab-active { color: var(--primary); border-bottom-color: var(--primary); font-weight: 600; }
</style>
