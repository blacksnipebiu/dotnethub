
<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

const isRegister = ref(false)
const username = ref('')
const password = ref('')
const error = ref('')

async function submit() {
  error.value = ''
  try {
    if (isRegister.value) {
      await auth.register(username.value, password.value)
    } else {
      await auth.login(username.value, password.value)
    }
    router.push('/dashboard')
  } catch (e: any) {
    error.value = e.response?.data?.message || '操作失败，请重试'
  }
}
</script>

<template>
  <div style="max-width:400px;margin:80px auto">
    <div class="card">
      <h1 class="page-title">{{ isRegister ? '注册账号' : '登录' }}</h1>
      
      <div v-if="error" class="alert alert-error">{{ error }}</div>
      
      <div class="form-group">
        <label>用户名</label>
        <input v-model="username" class="form-input" placeholder="请输入用户名" @keyup.enter="submit" />
      </div>
      <div class="form-group">
        <label>密码</label>
        <input v-model="password" class="form-input" type="password" placeholder="请输入密码" @keyup.enter="submit" />
      </div>
      
      <button class="btn btn-primary" style="width:100%" @click="submit">
        {{ isRegister ? '注册' : '登录' }}
      </button>
      
      <p style="text-align:center;margin-top:16px;font-size:0.9rem;color:var(--text-muted)">
        {{ isRegister ? '已有账号？' : '没有账号？' }}
        <a href="#" @click.prevent="isRegister = !isRegister; error = ''">
          {{ isRegister ? '去登录' : '去注册' }}
        </a>
      </p>
      
      <p style="text-align:center;margin-top:8px;font-size:0.8rem;color:var(--text-muted)">
        默认管理员：admin / admin123
      </p>
    </div>
  </div>
</template>
