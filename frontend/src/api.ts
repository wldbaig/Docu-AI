import type { AuthResponse, DocumentItem, Source } from './types'

const baseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5080/api'

async function problem(response: Response): Promise<never> {
  let message = `Request failed (${response.status})`
  try { const body = await response.json(); message = body.detail ?? body.title ?? message } catch { /* non-JSON response */ }
  throw new Error(message)
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('docuchat_token')
  const headers = new Headers(init.headers)
  if (!(init.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)
  const response = await fetch(`${baseUrl}${path}`, { ...init, headers })
  if (!response.ok) return problem(response)
  return response.status === 204 ? undefined as T : response.json()
}

export const api = {
  login: (email: string, password: string) => request<AuthResponse>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  register: (email: string, password: string) => request<AuthResponse>('/auth/register', { method: 'POST', body: JSON.stringify({ email, password }) }),
  documents: () => request<DocumentItem[]>('/documents'),
  upload: (file: File) => { const body = new FormData(); body.append('file', file); return request<DocumentItem>('/documents', { method: 'POST', body }) },
  deleteDocument: (id: string) => request<void>(`/documents/${id}`, { method: 'DELETE' }),
  async ask(question: string, callbacks: { sources: (sources: Source[]) => void; token: (token: string) => void }, signal?: AbortSignal) {
    const token = localStorage.getItem('docuchat_token')
    const response = await fetch(`${baseUrl}/chat/ask`, { method: 'POST', signal, headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }, body: JSON.stringify({ question, topK: 5 }) })
    if (!response.ok) return problem(response)
    if (!response.body) throw new Error('Streaming is not supported by this browser.')
    const reader = response.body.getReader(); const decoder = new TextDecoder(); let buffer = ''
    while (true) {
      const { done, value } = await reader.read(); buffer += decoder.decode(value, { stream: !done })
      const frames = buffer.split('\n\n'); buffer = frames.pop() ?? ''
      for (const frame of frames) {
        const event = frame.match(/^event: (.+)$/m)?.[1]; const data = frame.match(/^data: (.+)$/m)?.[1]
        if (!event || !data) continue
        if (event === 'sources') callbacks.sources(JSON.parse(data))
        if (event === 'token') callbacks.token(JSON.parse(data))
        if (event === 'error') throw new Error(JSON.parse(data).message ?? 'The answer stream was interrupted.')
      }
      if (done) break
    }
  }
}
