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
    <div className="flex h-screen bg-cyber-dark overflow-hidden">
      <Sidebar onSelect={handleSend} />

      <main className="flex-1 flex flex-col">
        <header className="px-6 py-3 border-b border-cyber-border flex items-center justify-between">
          <span className="text-xs text-gray-500">
            {sessionId ? `Sesión: ${sessionId.slice(0, 8)}…` : 'Nueva sesión'}
          </span>
          <span className={`text-xs px-2 py-1 rounded ${
            loading
              ? 'text-cyber-accent bg-cyber-accent/10 border border-cyber-accent/30'
              : 'text-gray-600'
          }`}>
            {loading ? '⟳ Procesando…' : 'Listo'}
          </span>
        </header>

        <div className="flex-1 overflow-y-auto px-6 py-4">
          {messages.map((m, i) => (
            <ChatMessage key={i} role={m.role} content={m.content} />
          ))}
          {loading && (
            <div className="flex justify-start mb-4">
              <div className="bg-cyber-panel border border-cyber-border rounded-lg px-4 py-3">
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
