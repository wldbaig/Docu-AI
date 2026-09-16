export type AuthResponse = { token: string; email: string }
export type DocumentItem = { id: string; fileName: string; uploadedAt: string; status: 'Processing' | 'Ready' | 'Failed'; error?: string; chunkCount: number }
export type Source = { documentId: string; fileName: string; chunkIndex: number; content: string; score: number }
export type ChatMessage = { id: string; role: 'user' | 'assistant'; content: string; sources?: Source[]; pending?: boolean }
