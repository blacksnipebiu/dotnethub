
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
    error.value = e.response?.data?.message || 'An error occurred'
  }
}
</script>

<template>
  <div style="max-width:400px;margin:80px auto">
    <div class="card">
      <h1 class="page-title">{{ isRegister ? 'Register' : 'Login' }}</h1>
      
      <div v-if="error" class="alert alert-error">{{ error }}</div>
      
      <div class="form-group">
        <label>Username</label>
        <input v-model="username" class="form-input" placeholder="Enter username" @keyup.enter="submit" />
      </div>
      <div class="form-group">
        <label>Password</label>
        <input v-model="password" class="form-input" type="password" placeholder="Enter password" @keyup.enter="submit" />
      </div>
      
      <button class="btn btn-primary" style="width:100%" @click="submit">
        {{ isRegister ? 'Register' : 'Login' }}
      </button>
      
      <p style="text-align:center;margin-top:16px;font-size:0.9rem;color:var(--text-muted)">
        {{ isRegister ? 'Already have an account?' : "Don't have an account?" }}
        <a href="#" @click.prevent="isRegister = !isRegister; error = ''">
          {{ isRegister ? 'Login' : 'Register' }}
        </a>
      </p>
      
      <p style="text-align:center;margin-top:8px;font-size:0.8rem;color:var(--text-muted)">
        Default admin: admin / admin123
      </p>
    </div>
  </div>
</template>
