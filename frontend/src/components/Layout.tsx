import { FileText, LogOut, MessageSquareText, Sparkles } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../AuthContext'

export function Layout() {
  const { email, logout } = useAuth()
  const link = ({ isActive }: { isActive: boolean }) => `flex items-center gap-2 rounded-xl px-3.5 py-2 text-sm font-semibold transition ${isActive ? 'bg-emerald-50 text-emerald-800' : 'text-slate-500 hover:bg-slate-100 hover:text-slate-900'}`
  return <div className="min-h-screen">
    <header className="sticky top-0 z-20 border-b bg-white/90 backdrop-blur-xl">
      <div className="mx-auto flex h-16 max-w-7xl items-center gap-4 px-4 sm:px-6">
        <div className="flex items-center gap-2.5"><span className="grid h-9 w-9 place-items-center rounded-xl bg-ink text-mint shadow-glow"><Sparkles size={18}/></span><span className="font-bold tracking-tight">DocuChat <span className="text-emerald-600">AI</span></span></div>
        <nav className="ml-2 flex gap-1"><NavLink className={link} to="/documents"><FileText size={17}/> <span className="hidden sm:inline">Documents</span></NavLink><NavLink className={link} to="/chat"><MessageSquareText size={17}/> <span className="hidden sm:inline">Chat</span></NavLink></nav>
        <div className="ml-auto flex items-center gap-2"><span className="hidden text-sm text-slate-500 md:block">{email}</span><button className="btn-ghost" onClick={logout} aria-label="Log out"><LogOut size={17}/></button></div>
      </div>
    </header>
    <main className="mx-auto max-w-7xl px-4 py-8 sm:px-6"><Outlet/></main>
  </div>
}
