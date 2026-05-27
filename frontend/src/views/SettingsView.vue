<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../api'

interface Setting { key: string; value: string; description: string }

const settings = ref<Setting[]>([])
const message = ref('')
const editingKey = ref('')
const editingValue = ref('')

async function load() {
  const { data } = await api.get('/settings')
  settings.value = data
}

function edit(s: Setting) {
  editingKey.value = s.key
  editingValue.value = s.value
}

async function save() {
  try {
    await api.put(`/settings/${editingKey.value}`, { value: editingValue.value })
    message.value = '保存成功'
    editingKey.value = ''
    await load()
  } catch (e: any) {
    message.value = '保存失败'
  }
}

onMounted(load)
</script>

<template>
  <div>
    <h1 class="page-title">系统设置</h1>
    <div v-if="message" class="alert alert-success">{{ message }}</div>

    <div class="card">
      <table style="width:100%;border-collapse:collapse">
        <thead>
          <tr style="text-align:left;border-bottom:1px solid var(--border)">
            <th style="padding:8px">设置项</th>
            <th style="padding:8px">当前值</th>
            <th style="padding:8px">说明</th>
            <th style="padding:8px">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="s in settings" :key="s.key" style="border-bottom:1px solid var(--border)">
            <td style="padding:8px;font-weight:500">{{ s.key }}</td>
            <td style="padding:8px">
              <span v-if="editingKey !== s.key">{{ s.value }}</span>
              <input v-else v-model="editingValue" class="form-input" style="width:120px;display:inline" />
            </td>
            <td style="padding:8px;color:var(--text-muted);font-size:0.85rem">{{ s.description }}</td>
            <td style="padding:8px">
              <template v-if="editingKey === s.key">
                <button class="btn btn-primary btn-sm" @click="save">保存</button>
                <button class="btn btn-outline btn-sm" style="margin-left:4px" @click="editingKey = ''">取消</button>
              </template>
              <button v-else class="btn btn-outline btn-sm" @click="edit(s)">编辑</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
