import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import { api } from './api'

type AuthValue = { email: string | null; login: (email: string, password: string) => Promise<void>; register: (email: string, password: string) => Promise<void>; logout: () => void }
const AuthContext = createContext<AuthValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem('docuchat_email'))
  const save = (token: string, nextEmail: string) => { localStorage.setItem('docuchat_token', token); localStorage.setItem('docuchat_email', nextEmail); setEmail(nextEmail) }
  const value = useMemo<AuthValue>(() => ({
    email,
    login: async (userEmail, password) => { const result = await api.login(userEmail, password); save(result.token, result.email) },
    register: async (userEmail, password) => { const result = await api.register(userEmail, password); save(result.token, result.email) },
    logout: () => { localStorage.removeItem('docuchat_token'); localStorage.removeItem('docuchat_email'); setEmail(null) }
  }), [email])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() { const value = useContext(AuthContext); if (!value) throw new Error('AuthProvider is missing'); return value }
