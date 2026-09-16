import { Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { useAuth } from './AuthContext'
import { Layout } from './components/Layout'
import { AuthPage } from './pages/AuthPage'
import { ChatPage } from './pages/ChatPage'
import { DocumentsPage } from './pages/DocumentsPage'

function Protected() { return useAuth().email ? <Outlet/> : <Navigate to="/login" replace/> }
export default function App() { return <Routes><Route path="/login" element={<AuthPage/>}/><Route element={<Protected/>}><Route element={<Layout/>}><Route path="/documents" element={<DocumentsPage/>}/><Route path="/chat" element={<ChatPage/>}/></Route></Route><Route path="*" element={<Navigate to="/documents" replace/>}/></Routes> }
