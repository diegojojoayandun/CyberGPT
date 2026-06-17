import { useState } from 'react'

const QUICK_PROMPTS = [
  'Explica Kerberoasting y cómo detectarlo en logs',
  'Mapea Pass-the-Hash a MITRE ATT&CK',
  'Analiza un evento Windows 4625',
  'Qué hace el grupo APT29 típicamente',
  'Diferencia entre NTLM y Kerberos',
  'Cómo detectar LSASS dumping',
]

function formatDate(isoString) {
  const d = new Date(isoString)
  const now = new Date()
  const diffMs = now - d
  const diffH = diffMs / 3600000
  if (diffH < 24) return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
  if (diffH < 168) return d.toLocaleDateString([], { weekday: 'short' })
  return d.toLocaleDateString([], { day: '2-digit', month: 'short' })
}

export default function Sidebar({ sessions, activeSessionId, onSelect, onNewSession, onLoadSession, onDeleteSession }) {
  const [hoveredId, setHoveredId] = useState(null)

  return (
    <aside className="glass-panel w-64 flex flex-col relative z-10 shrink-0">
      {/* Header */}
      <div className="p-5 border-b border-white/5">
        <div className="flex items-center gap-2">
          <span className="text-cyber-accent text-lg font-bold glow-text">⚡ CyberGPT</span>
        </div>
        <p className="text-xs text-gray-500 mt-1 tracking-widest uppercase">RAG · Ciberseguridad</p>
      </div>

      {/* Nueva sesión */}
      <div className="px-4 pt-4">
        <button
          onClick={onNewSession}
          className="w-full text-xs glass rounded-lg px-3 py-2.5 text-left
                     text-cyber-accent hover:bg-cyber-accent/10 hover:border-cyber-accent/30
                     border border-cyber-accent/20 transition-all duration-200
                     hover:shadow-[0_0_12px_rgba(34,211,238,0.15)] flex items-center gap-2"
        >
          <span className="text-base leading-none">+</span>
          <span>Nueva conversación</span>
        </button>
      </div>

      {/* Conversaciones recientes */}
      <div className="px-4 pt-4 flex-1 overflow-y-auto">
        {sessions.length > 0 && (
          <>
            <p className="text-xs text-gray-600 uppercase tracking-widest mb-2">Recientes</p>
            <div className="flex flex-col gap-1 mb-4">
              {sessions.map(s => (
                <div
                  key={s.sessionId}
                  onMouseEnter={() => setHoveredId(s.sessionId)}
                  onMouseLeave={() => setHoveredId(null)}
                  className={`group flex items-center gap-1 rounded-lg px-2 py-2 cursor-pointer
                    transition-all duration-150
                    ${s.sessionId === activeSessionId
                      ? 'bg-cyber-accent/10 border border-cyber-accent/20'
                      : 'hover:bg-white/5 border border-transparent hover:border-white/8'
                    }`}
                  onClick={() => onLoadSession(s.sessionId)}
                >
                  <div className="flex-1 min-w-0">
                    <p className={`text-xs truncate leading-snug ${
                      s.sessionId === activeSessionId ? 'text-cyber-accent' : 'text-gray-400'
                    }`}>
                      {s.title}
                    </p>
                    <p className="text-[10px] text-gray-600 mt-0.5">
                      {formatDate(s.lastTimestamp)} · {s.messageCount} msgs
                    </p>
                  </div>
                  {hoveredId === s.sessionId && (
                    <button
                      onClick={e => { e.stopPropagation(); onDeleteSession(s.sessionId) }}
                      className="text-gray-600 hover:text-red-400 transition-colors text-xs px-1 shrink-0"
                      title="Eliminar"
                    >
                      ✕
                    </button>
                  )}
                </div>
              ))}
            </div>
          </>
        )}

        {/* Preguntas rápidas */}
        <p className="text-xs text-gray-600 uppercase tracking-widest mb-2">Preguntas rápidas</p>
        <div className="flex flex-col gap-1.5">
          {QUICK_PROMPTS.map((p, i) => (
            <button
              key={i}
              onClick={() => onSelect(p)}
              className="text-left text-xs text-gray-400 hover:text-cyber-accent
                         glass rounded-lg px-3 py-2.5
                         hover:border-cyber-accent/30 hover:bg-cyber-accent/5
                         transition-all duration-200 hover:shadow-[0_0_12px_rgba(34,211,238,0.1)]"
            >
              {p}
            </button>
          ))}
        </div>
      </div>

      <div className="p-4 border-t border-white/5">
        <p className="text-xs text-gray-700 tracking-wider">MITRE · OWASP · AD · Malware</p>
      </div>
    </aside>
  )
}
