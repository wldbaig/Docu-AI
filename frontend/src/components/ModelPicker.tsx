import { useEffect, useState } from 'react'
import { Cpu } from 'lucide-react'
import { api } from '../api'
import type { ProviderCatalog } from '../types'

const STORAGE_KEY = 'docuchat_chat_provider'

/**
 * Lets the user pick which chat model answers their questions.
 * Only providers with a configured API key are selectable; the choice is
 * remembered in localStorage and reported to the parent via onChange.
 */
export function ModelPicker({ value, onChange }: { value: string; onChange: (provider: string) => void }) {
  const [catalog, setCatalog] = useState<ProviderCatalog | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    api.providers()
      .then(result => {
        if (!active) return
        setCatalog(result)
        const stored = localStorage.getItem(STORAGE_KEY)
        const configured = result.chat.filter(p => p.configured).map(p => p.name)
        // Prefer a remembered, still-configured choice; otherwise the active default.
        const initial = stored && configured.includes(stored) ? stored
          : configured.includes(result.activeChat) ? result.activeChat
          : configured[0] ?? ''
        if (initial) onChange(initial)
      })
      .catch(e => active && setError(e instanceof Error ? e.message : 'Could not load models'))
    return () => { active = false }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const select = (provider: string) => { localStorage.setItem(STORAGE_KEY, provider); onChange(provider) }

  if (error) return <span className="text-xs text-red-600">{error}</span>
  if (!catalog) return null

  const anyConfigured = catalog.chat.some(p => p.configured)

  return (
    <label className="flex items-center gap-2 rounded-xl border bg-white px-3 py-1.5 text-sm shadow-sm">
      <Cpu size={15} className="text-emerald-600" />
      <span className="hidden font-semibold text-slate-500 sm:inline">Model</span>
      <select
        className="cursor-pointer bg-transparent font-semibold text-slate-800 outline-none disabled:cursor-not-allowed disabled:text-slate-400"
        value={value}
        disabled={!anyConfigured}
        onChange={e => select(e.target.value)}
        aria-label="Chat model provider"
      >
        {!anyConfigured && <option value="">No model configured</option>}
        {catalog.chat.map(p => (
          <option key={p.name} value={p.name} disabled={!p.configured}>
            {p.name}{p.configured ? '' : ' — no key'}
          </option>
        ))}
      </select>
    </label>
  )
}
