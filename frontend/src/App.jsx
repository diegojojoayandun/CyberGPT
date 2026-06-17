import { useState, useRef, useEffect, useCallback } from 'react'
import ChatMessage from './components/ChatMessage'
import ChatInput from './components/ChatInput'
import Sidebar from './components/Sidebar'
import ModelPicker from './components/ModelPicker'
import { streamMessage, getSessions, getSessionMessages, deleteSession } from './services/api'

const WELCOME = {
  role: 'assistant',
  content: '**CyberGPT listo.** Pregúntame sobre ATT&CK, Active Directory, malware, Windows Internals, OWASP o OSINT.',
  sources: []
}

export default function App() {
  const [messages, setMessages] = useState([WELCOME])
  const [loading, setLoading] = useState(false)
  const [sessionId, setSessionId] = useState(null)
  const [sessions, setSessions] = useState([])
  const [category, setCategory] = useState(null)
  const [selectedModel, setSelectedModel] = useState(null)
  const bottomRef = useRef(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const refreshSessions = useCallback(async () => {
    try { setSessions(await getSessions()) } catch { /* ignore */ }
  }, [])

  useEffect(() => { refreshSessions() }, [refreshSessions])

  const handleSend = async (text) => {
    setMessages(prev => [...prev, { role: 'user', content: text, sources: [] }])
    setLoading(true)

    // Add placeholder for streaming assistant message
    const assistantIdx = (prev => prev.length)(messages) + 1
    setMessages(prev => [...prev, { role: 'assistant', content: '', sources: [], streaming: true }])

    try {
      let activeSessionId = sessionId

      await streamMessage(text, sessionId, category, selectedModel, {
        onSources: (srcs, sid) => {
          activeSessionId = sid
          setSessionId(sid)
          setMessages(prev => {
            const updated = [...prev]
            updated[updated.length - 1] = { ...updated[updated.length - 1], sources: srcs }
            return updated
          })
        },
        onToken: (token) => {
          setMessages(prev => {
            const updated = [...prev]
            const last = updated[updated.length - 1]
            updated[updated.length - 1] = { ...last, content: last.content + token }
            return updated
          })
        },
        onDone: () => {
          setMessages(prev => {
            const updated = [...prev]
            updated[updated.length - 1] = { ...updated[updated.length - 1], streaming: false }
            return updated
          })
          refreshSessions()
        }
      })
    } catch {
      setMessages(prev => {
        const updated = [...prev]
        updated[updated.length - 1] = {
          role: 'assistant',
          content: '⚠️ Error al conectar con el backend. Verifica que Ollama y la API estén corriendo.',
          sources: [],
          streaming: false
        }
        return updated
      })
    } finally {
      setLoading(false)
    }
  }

  const handleNewSession = () => {
    setSessionId(null)
    setMessages([WELCOME])
  }

  const handleLoadSession = async (sid) => {
    try {
      const turns = await getSessionMessages(sid)
      const msgs = turns.map(t => ({ role: t.role, content: t.message, sources: [] }))
      setMessages(msgs.length ? msgs : [WELCOME])
      setSessionId(sid)
    } catch { /* ignore */ }
  }

  const handleDeleteSession = async (sid) => {
    try {
      await deleteSession(sid)
      if (sid === sessionId) handleNewSession()
      await refreshSessions()
    } catch { /* ignore */ }
  }

  return (
    <div className="flex h-screen bg-cyber-dark overflow-hidden relative">
      <div className="absolute inset-0 pointer-events-none z-0"
           style={{background:'radial-gradient(ellipse at 50% 80%, rgba(34,211,238,0.04) 0%, transparent 60%)'}} />

      <Sidebar
        sessions={sessions}
        activeSessionId={sessionId}
        onSelect={handleSend}
        onNewSession={handleNewSession}
        onLoadSession={handleLoadSession}
        onDeleteSession={handleDeleteSession}
      />

      <main className="flex-1 flex flex-col relative z-10 min-w-0">
        <header className="glass px-6 py-3 flex items-center justify-between">
          <span className="text-xs text-gray-500">
            {sessionId ? `Sesión: ${sessionId.slice(0, 8)}…` : 'Nueva sesión'}
          </span>
          <div className="flex items-center gap-3">
            <ModelPicker selectedModel={selectedModel} onSelect={setSelectedModel} />
            <span className={`text-xs px-3 py-1 rounded-full transition-all ${
              loading
                ? 'glass-accent text-cyber-accent animate-pulse'
                : 'text-gray-600'
            }`}>
              {loading ? '⟳ Procesando…' : 'Listo'}
            </span>
          </div>
        </header>

        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-1">
          {messages.map((m, i) => (
            <ChatMessage key={i} role={m.role} content={m.content} sources={m.sources} streaming={m.streaming} />
          ))}
          <div ref={bottomRef} />
        </div>

        <ChatInput onSend={handleSend} disabled={loading} onCategoryChange={setCategory} />
      </main>
    </div>
  )
}
