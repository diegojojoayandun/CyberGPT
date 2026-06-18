import { useState, useRef } from 'react'
import { uploadPdf } from '../services/api'

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

const UPLOAD_CATEGORIES = ['general', 'mitre', 'ad', 'malware', 'owasp', 'windows']

function PdfUploadZone() {
  const [dragging, setDragging] = useState(false)
  const [status, setStatus] = useState(null) // null | 'uploading' | 'done' | 'error'
  const [category, setCategory] = useState('general')
  const [expanded, setExpanded] = useState(false)
  const inputRef = useRef(null)

  const handleFiles = async (files) => {
    const file = files[0]
    if (!file) return
    setStatus('uploading')
    try {
      await uploadPdf(file, category)
      setStatus('done')
      setTimeout(() => setStatus(null), 3000)
    } catch {
      setStatus('error')
      setTimeout(() => setStatus(null), 3000)
    }
  }

  const onDrop = (e) => {
    e.preventDefault()
    setDragging(false)
    handleFiles(e.dataTransfer.files)
  }

  return (
    <div className="px-4 pt-3">
      <button
        onClick={() => setExpanded(v => !v)}
        className="w-full text-xs text-gray-500 hover:text-gray-400 flex items-center gap-1 mb-2 transition-colors"
      >
        <span>{expanded ? '▾' : '▸'}</span>
        <span className="uppercase tracking-widest">Subir documento</span>
      </button>
      {expanded && (
        <div className="space-y-2 pb-2">
          <select
            value={category}
            onChange={e => setCategory(e.target.value)}
            className="w-full text-[11px] bg-white/5 border border-white/10 rounded-lg px-2 py-1.5
                       text-gray-400 focus:outline-none focus:border-cyber-accent/30"
          >
            {UPLOAD_CATEGORIES.map(c => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
          <div
            onDragOver={e => { e.preventDefault(); setDragging(true) }}
            onDragLeave={() => setDragging(false)}
            onDrop={onDrop}
            onClick={() => inputRef.current?.click()}
            className={`border-2 border-dashed rounded-lg px-3 py-4 text-center cursor-pointer
                        transition-all duration-200 text-[11px]
                        ${dragging
                          ? 'border-cyber-accent/60 bg-cyber-accent/10 text-cyber-accent'
                          : 'border-white/10 text-gray-600 hover:border-white/20 hover:text-gray-500'
                        }`}
          >
            {status === 'uploading' && <span className="text-cyber-accent animate-pulse">Indexando…</span>}
            {status === 'done'      && <span className="text-green-400">✓ Indexado</span>}
            {status === 'error'     && <span className="text-red-400">✕ Error al subir</span>}
            {!status && <span>Arrastra PDF/TXT/MD<br />o haz clic</span>}
          </div>
          <input
            ref={inputRef}
            type="file"
            accept=".pdf,.txt,.md"
            className="hidden"
            onChange={e => handleFiles(e.target.files)}
          />
        </div>
      )}
    </div>
  )
}

export default function Sidebar({ sessions, activeSessionId, onSelect, onNewSession, onLoadSession, onDeleteSession, loading, mode, onModeChange }) {
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

      {/* Mode switcher */}
      <div className="px-4 pt-4 flex gap-2">
        <button
          onClick={() => onModeChange('chat')}
          className={`flex-1 text-xs rounded-lg px-3 py-2 transition-all duration-150
            ${mode === 'chat'
              ? 'glass-accent text-cyber-accent border border-cyber-accent/30'
              : 'glass text-gray-500 border border-white/10 hover:text-gray-400'}`}
        >
          💬 Chat
        </button>
        <button
          onClick={() => onModeChange('osint')}
          className={`flex-1 text-xs rounded-lg px-3 py-2 transition-all duration-150
            ${mode === 'osint'
              ? 'glass-accent text-cyber-accent border border-cyber-accent/30'
              : 'glass text-gray-500 border border-white/10 hover:text-gray-400'}`}
        >
          🕵️ OSINT
        </button>
      </div>

      {/* Nueva sesión (solo en modo chat) */}
      {mode === 'chat' && (
      <div className="px-4 pt-3">
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
      )}

      {mode === 'chat' && <PdfUploadZone />}

      {/* Conversaciones recientes + Quick prompts (solo chat) */}
      <div className="px-4 pt-4 flex-1 overflow-y-auto">
      {mode === 'osint' && (
        <div className="text-center py-8 text-gray-700 text-xs space-y-1">
          <p>Modo OSINT activo.</p>
          <p>Ingresa un target en el panel principal.</p>
        </div>
      )}
      {mode === 'chat' && <>
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
              disabled={loading}
              className={`text-left text-xs glass rounded-lg px-3 py-2.5
                         transition-all duration-200
                         ${loading
                           ? 'text-gray-600 cursor-not-allowed opacity-50'
                           : 'text-gray-400 hover:text-cyber-accent hover:border-cyber-accent/30 hover:bg-cyber-accent/5 hover:shadow-[0_0_12px_rgba(34,211,238,0.1)]'
                         }`}
            >
              {p}
            </button>
          ))}
        </div>
        </>}
      </div>

      <div className="p-4 border-t border-white/5">
        <p className="text-xs text-gray-700 tracking-wider">MITRE · OWASP · AD · Malware</p>
      </div>
    </aside>
  )
}
