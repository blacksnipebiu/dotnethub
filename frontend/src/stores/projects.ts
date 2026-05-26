
import { defineStore } from 'pinia'
import { ref } from 'vue'
import api from '../api'

export interface Project {
  id: number
  name: string
  description: string
  port: number
  status: string
  dotNetVersion: string
  createdAt: string
  updatedAt: string
  isPublic: boolean
  gitRepo: string | null
  ownerName: string | null
}

export const useProjectsStore = defineStore('projects', () => {
  const projects = ref<Project[]>([])
  const loading = ref(false)

  async function fetchProjects(publicOnly?: boolean) {
    loading.value = true
    try {
      const params = publicOnly !== undefined ? { publicOnly } : {}
      const { data } = await api.get('/projects', { params })
      projects.value = data
    } finally {
      loading.value = false
    }
  }

  async function createProject(project: any) {
    const { data } = await api.post('/projects', project)
    return data
  }

  async function updateProject(id: number, update: any) {
    const { data } = await api.put(`/projects/${id}`, update)
    return data
  }

  async function deleteProject(id: number) {
    await api.delete(`/projects/${id}`)
    projects.value = projects.value.filter(p => p.id !== id)
  }

  async function uploadFiles(id: number, files: FileList | File[]) {
    const formData = new FormData()
    for (const file of Array.from(files)) {
      formData.append('files', file)
    }
    await api.post(`/projects/${id}/upload`, formData)
  }

  async function buildProject(id: number) {
    await api.post(`/projects/${id}/build`)
  }

  async function deployProject(id: number) {
    await api.post(`/projects/${id}/deploy`)
  }

  async function stopProject(id: number) {
    await api.post(`/projects/${id}/stop`)
  }

  return { projects, loading, fetchProjects, createProject, updateProject, 
           deleteProject, uploadFiles, buildProject, deployProject, stopProject }
})
