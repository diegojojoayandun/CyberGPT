import { useState, useRef, useEffect } from 'react'
import ChatMessage from './components/ChatMessage'
import ChatInput from './components/ChatInput'
import Sidebar from './components/Sidebar'
import { sendMessage } from './services/api'

export default function App() {
  const [messages, setMessages] = useState([
    {
      role: 'assistant',
      content: '**CyberGPT listo.** Pregúntame sobre ATT&CK, Active Directory, malware, Windows Internals, OWASP o OSINT.'
    }
  ])
  const [loading, setLoading] = useState(false)
  const [sessionId, setSessionId] = useState(null)
  const bottomRef = useRef(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const handleSend = async (text) => {
    setMessages(prev => [...prev, { role: 'user', content: text }])
    setLoading(true)
    try {
      const data = await sendMessage(text, sessionId)
      setSessionId(data.sessionId)
      setMessages(prev => [...prev, { role: 'assistant', content: data.reply }])
    } catch {
      setMessages(prev => [...prev, {
        role: 'assistant',
        content: '⚠️ Error al conectar con el backend. Verifica que Ollama y la API estén corriendo.'
      }])
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex h-screen bg-cyber-dark overflow-hidden relative">
      {/* Orb extra central */}
      <div className="absolute inset-0 pointer-events-none z-0"
           style={{background:'radial-gradient(ellipse at 50% 80%, rgba(34,211,238,0.04) 0%, transparent 60%)'}} />

      <Sidebar onSelect={handleSend} />

      <main className="flex-1 flex flex-col relative z-10">
        <header className="glass px-6 py-3 flex items-center justify-between">
          <span className="text-xs text-gray-500">
            {sessionId ? `Sesión: ${sessionId.slice(0, 8)}…` : 'Nueva sesión'}
          </span>
          <span className={`text-xs px-3 py-1 rounded-full transition-all ${
            loading
              ? 'glass-accent text-cyber-accent animate-pulse'
              : 'text-gray-600'
          }`}>
            {loading ? '⟳ Procesando…' : 'Listo'}
          </span>
        </header>

        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-1">
          {messages.map((m, i) => (
            <ChatMessage key={i} role={m.role} content={m.content} />
          ))}
          {loading && (
            <div className="flex justify-start mb-4">
              <div className="glass rounded-xl px-4 py-3">
                <span className="text-cyber-accent text-sm animate-pulse">Analizando…</span>
              </div>
            </div>
          )}
          <div ref={bottomRef} />
        </div>

        <ChatInput onSend={handleSend} disabled={loading} />
      </main>
    </div>
  )
}
