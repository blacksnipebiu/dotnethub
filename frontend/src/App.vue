
<script setup lang="ts">
import { useAuthStore } from './stores/auth'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div class="app">
    <nav class="navbar">
      <div class="nav-brand">
        <router-link to="/">🚀 DotNetHub</router-link>
      </div>
      <div class="nav-links">
        <router-link to="/projects">项目管理</router-link>
        <template v-if="auth.isLoggedIn()">
          <router-link to="/dashboard">控制台</router-link>
          <router-link to="/projects/create">+ 新建项目</router-link>
          <router-link v-if="auth.isAdmin()" to="/admin">系统管理</router-link>
          <span class="nav-user">{{ auth.user?.username }}</span>
          <a href="#" @click.prevent="logout">退出登录</a>
        </template>
        <template v-else>
          <router-link to="/login">登录</router-link>
        </template>
      </div>
    </nav>
    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<style>
:root {
  --primary: #6366f1;
  --primary-dark: #4f46e5;
  --bg: #f8fafc;
  --card-bg: #ffffff;
  --text: #1e293b;
  --text-muted: #64748b;
  --border: #e2e8f0;
  --success: #22c55e;
  --error: #ef4444;
  --warning: #f59e0b;
}

* { margin: 0; padding: 0; box-sizing: border-box; }

body {
  font-family: system-ui, -apple-system, sans-serif;
  background: var(--bg);
  color: var(--text);
}

.navbar {
  background: var(--card-bg);
  border-bottom: 1px solid var(--border);
  padding: 0 24px;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  position: sticky;
  top: 0;
  z-index: 100;
}

.nav-brand a {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--primary);
  text-decoration: none;
}

.nav-links {
  display: flex;
  gap: 16px;
  align-items: center;
}

.nav-links a {
  color: var(--text-muted);
  text-decoration: none;
  font-size: 0.9rem;
  transition: color 0.2s;
}

.nav-links a:hover, .nav-links a.router-link-exact-active {
  color: var(--primary);
}

.nav-user {
  color: var(--text);
  font-weight: 500;
  font-size: 0.85rem;
  padding: 4px 10px;
  background: var(--bg);
  border-radius: 6px;
}

.main-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 24px;
}

.card {
  background: var(--card-bg);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 24px;
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 20px;
  border: none;
  border-radius: 8px;
  font-size: 0.9rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  text-decoration: none;
}

.btn-primary {
  background: var(--primary);
  color: white;
}
.btn-primary:hover { background: var(--primary-dark); }

.btn-success {
  background: var(--success);
  color: white;
}

.btn-error {
  background: var(--error);
  color: white;
}

.btn-outline {
  background: transparent;
  border: 1px solid var(--border);
  color: var(--text);
}
.btn-outline:hover { border-color: var(--primary); color: var(--primary); }

.btn-sm {
  padding: 6px 12px;
  font-size: 0.8rem;
}

.form-group {
  margin-bottom: 16px;
}

.form-group label {
  display: block;
  margin-bottom: 6px;
  font-weight: 500;
  font-size: 0.9rem;
  color: var(--text-muted);
}

.form-input {
  width: 100%;
  padding: 10px 14px;
  border: 1px solid var(--border);
  border-radius: 8px;
  font-size: 0.95rem;
  transition: border-color 0.2s;
  outline: none;
}

.form-input:focus {
  border-color: var(--primary);
  box-shadow: 0 0 0 3px rgba(99,102,241,0.1);
}

.alert {
  padding: 12px 16px;
  border-radius: 8px;
  margin-bottom: 16px;
  font-size: 0.9rem;
}

.alert-error { background: #fef2f2; color: var(--error); border: 1px solid #fecaca; }
.alert-success { background: #f0fdf4; color: #16a34a; border: 1px solid #bbf7d0; }

.grid-2 { display: grid; grid-template-columns: repeat(auto-fill, minmax(350px, 1fr)); gap: 16px; }
.grid-3 { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; }

.mt-16 { margin-top: 16px; }
.mb-16 { margin-bottom: 16px; }

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 10px;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
}

.status-running { background: #dcfce7; color: #16a34a; }
.status-stopped { background: #f1f5f9; color: #64748b; }
.status-building { background: #fef9c3; color: #ca8a04; }
.status-error { background: #fef2f2; color: #dc2626; }

.page-title {
  font-size: 1.5rem;
  font-weight: 700;
  margin-bottom: 24px;
}
</style>
