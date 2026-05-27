<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import api from '../api'

interface Stats { totalUsers: number; totalProjects: number; runningProjects: number }
interface User { id: number; username: string; role: string; createdAt: string; isActive: boolean }

const auth = useAuthStore()
const stats = ref<Stats>({ totalUsers: 0, totalProjects: 0, runningProjects: 0 })
const users = ref<User[]>([])
const message = ref('')
const error = ref('')

// Change password
const showPwdForm = ref(false)
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')

async function load() {
  const [s, u] = await Promise.all([
    api.get('/admin/stats'),
    api.get('/admin/users'),
  ])
  stats.value = s.data
  users.value = u.data
}

async function toggleActive(user: User) {
  if (user.id === auth.user?.id) {
    error.value = '不能对自己执行此操作'
    return
  }
  await api.put(`/admin/users/${user.id}/toggle-active`)
  await load()
}

async function changeRole(user: User, role: string) {
  if (user.id === auth.user?.id) {
    error.value = '不能修改自己的角色'
    return
  }
  try {
    await api.put(`/admin/users/${user.id}/role`, { role })
    await load()
    message.value = `${user.username} 的角色已变更为 ${role === 'admin' ? '管理员' : '普通用户'}`
  } catch (e: any) {
    error.value = e.response?.data?.message || '操作失败'
  }
}

async function changePassword() {
  error.value = ''
  message.value = ''
  if (newPassword.value !== confirmPassword.value) {
    error.value = '两次输入的新密码不一致'
    return
  }
  if (newPassword.value.length < 6) {
    error.value = '新密码至少 6 个字符'
    return
  }
  try {
    await api.put('/admin/change-password', {
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
    })
    message.value = '密码修改成功'
    showPwdForm.value = false
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
  } catch (e: any) {
    error.value = e.response?.data?.message || '密码修改失败'
  }
}

onMounted(load)
</script>

<template>
  <div>
    <h1 class="page-title">系统管理</h1>

    <div v-if="message" class="alert alert-success">{{ message }}</div>
    <div v-if="error" class="alert alert-error">{{ error }}</div>

    <div class="grid-3 mb-16">
      <div class="card" style="text-align:center">
        <div style="font-size:2rem;font-weight:700;color:var(--primary)">{{ stats.totalUsers }}</div>
        <div style="color:var(--text-muted);font-size:0.9rem">用户总数</div>
      </div>
      <div class="card" style="text-align:center">
        <div style="font-size:2rem;font-weight:700;color:var(--primary)">{{ stats.totalProjects }}</div>
        <div style="color:var(--text-muted);font-size:0.9rem">项目总数</div>
      </div>
      <div class="card" style="text-align:center">
        <div style="font-size:2rem;font-weight:700;color:var(--success)">{{ stats.runningProjects }}</div>
        <div style="color:var(--text-muted);font-size:0.9rem">运行中</div>
      </div>
    </div>

    <!-- 修改密码 -->
    <div class="card mb-16">
      <div style="display:flex;justify-content:space-between;align-items:center">
        <h2 style="margin:0">账号安全</h2>
        <button class="btn btn-outline btn-sm" @click="showPwdForm = !showPwdForm">
          {{ showPwdForm ? '取消' : '修改密码' }}
        </button>
      </div>
      <div v-if="showPwdForm" style="margin-top:16px;max-width:400px">
        <div class="form-group">
          <label>当前密码</label>
          <input v-model="currentPassword" class="form-input" type="password" placeholder="请输入当前密码" />
        </div>
        <div class="form-group">
          <label>新密码</label>
          <input v-model="newPassword" class="form-input" type="password" placeholder="至少 6 个字符" />
        </div>
        <div class="form-group">
          <label>确认新密码</label>
          <input v-model="confirmPassword" class="form-input" type="password" placeholder="再次输入新密码" />
        </div>
        <button class="btn btn-primary" @click="changePassword">确认修改</button>
      </div>
    </div>

    <!-- 用户管理 -->
    <div class="card">
      <h2 style="margin-bottom:16px">用户管理</h2>
      <table style="width:100%;border-collapse:collapse">
        <thead>
          <tr style="text-align:left;border-bottom:1px solid var(--border)">
            <th style="padding:8px">ID</th>
            <th style="padding:8px">用户名</th>
            <th style="padding:8px">角色</th>
            <th style="padding:8px">状态</th>
            <th style="padding:8px">创建时间</th>
            <th style="padding:8px">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in users" :key="u.id" style="border-bottom:1px solid var(--border)">
            <td style="padding:8px">{{ u.id }}</td>
            <td style="padding:8px;font-weight:500">
              {{ u.username }}
              <span v-if="u.id === auth.user?.id" style="font-size:0.75rem;color:var(--primary);margin-left:4px">(我)</span>
            </td>
            <td style="padding:8px">
              <span :class="'status-badge '+(u.role==='admin'?'status-running':'status-stopped')">
                {{ u.role === 'admin' ? '管理员' : '用户' }}
              </span>
            </td>
            <td style="padding:8px">
              <span :style="{color:u.isActive?'var(--success)':'var(--error)'}">
                {{ u.isActive ? '正常' : '已禁用' }}
              </span>
            </td>
            <td style="padding:8px;font-size:0.85rem">{{ new Date(u.createdAt).toLocaleDateString() }}</td>
            <td style="padding:8px">
              <div style="display:flex;gap:6px">
                <button
                  class="btn btn-outline btn-sm"
                  :disabled="u.id === auth.user?.id"
                  :title="u.id === auth.user?.id ? '不能对自己操作' : ''"
                  @click="toggleActive(u)"
                >
                  {{ u.isActive ? '禁用' : '启用' }}
                </button>
                <button
                  class="btn btn-outline btn-sm"
                  :disabled="u.id === auth.user?.id"
                  :title="u.id === auth.user?.id ? '不能对自己操作' : ''"
                  @click="changeRole(u, u.role==='admin'?'user':'admin')"
                >
                  {{ u.role === 'admin' ? '降级' : '提升' }}
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
