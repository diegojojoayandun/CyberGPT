import { useState, useRef } from 'react'
import { streamOsint } from '../services/api'
import OsintTimeline from './OsintTimeline'

const TARGET_TYPES = [
  { id: 'auto',     label: 'Auto' },
  { id: 'domain',   label: 'Dominio' },
  { id: 'ip',       label: 'IP' },
  { id: 'email',    label: 'Email' },
  { id: 'username', label: 'Username' },
  { id: 'phone',    label: 'Teléfono' },
]

export default function OsintPanel() {
  const [target,     setTarget]     = useState('')
  const [targetType, setTargetType] = useState('auto')
  const [running,    setRunning]    = useState(false)
  const [steps,      setSteps]      = useState([])   // { tool, status, target, result }
  const [report,     setReport]     = useState(null)
  const [thinking,   setThinking]   = useState(null)
  const [error,      setError]      = useState(null)
  const abortRef = useRef(null)

  const handleStart = async () => {
    if (!target.trim() || running) return

    // Reset state
    setSteps([])
    setReport(null)
    setThinking(null)
    setError(null)
    setRunning(true)

    const ctrl = new AbortController()
    abortRef.current = ctrl

    try {
      await streamOsint(target.trim(), targetType, {
        onEvent: (evt) => {
          switch (evt.type) {
            case 'thinking':
              setThinking(evt.content)
              break

            case 'tool_start':
              setSteps(prev => [...prev, { tool: evt.tool, status: 'running', target: evt.target }])
              break

            case 'tool_done':
              setSteps(prev => {
                const updated = [...prev]
                // Update the last entry for this tool
                const idx = [...updated].reverse().findIndex(s => s.tool === evt.tool && s.status === 'running')
                if (idx !== -1) {
                  const realIdx = updated.length - 1 - idx
                  updated[realIdx] = { ...updated[realIdx], status: 'done', result: evt.result }
                }
                return updated
              })
              break

            case 'report':
              setReport(evt.content)
              setThinking(null)
              break

            case 'error':
              setError(evt.content)
              break

            case 'done':
              setRunning(false)
              abortRef.current = null
              break
          }
        }
      }, ctrl.signal)
    } catch (e) {
      if (e.name !== 'AbortError') setError(e.message)
    } finally {
      setRunning(false)
      abortRef.current = null
    }
  }

  const handleStop = () => {
    abortRef.current?.abort()
    setRunning(false)
  }

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleStart() }
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="glass px-6 py-4 border-b border-white/5">
        <h2 className="text-sm font-semibold text-cyber-accent glow-text">⚡ OSINT Agent</h2>
        <p className="text-[11px] text-gray-600 mt-0.5">
          Investigación automática: WHOIS · DNS · Subdominios · GeoIP
        </p>
      </div>

      {/* Input area */}
      <div className="glass border-b border-white/5 px-6 py-4 space-y-3">
        {/* Target type selector */}
        <div className="flex gap-2">
          {TARGET_TYPES.map(t => (
            <button
              key={t.id}
              onClick={() => setTargetType(t.id)}
              className={`text-[11px] px-2.5 py-1 rounded-full border transition-all duration-150
                ${targetType === t.id
                  ? 'border-cyber-accent/60 bg-cyber-accent/15 text-cyber-accent'
                  : 'border-white/10 text-gray-500 hover:border-white/20 hover:text-gray-400'
                }`}
            >
              {t.label}
            </button>
          ))}
        </div>

        {/* Target input + action button */}
        <div className="flex gap-2">
          <input
            value={target}
            onChange={e => setTarget(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={running}
            placeholder="example.com · 8.8.8.8 · user@email.com · @username · 5491112345678"
            className="flex-1 bg-white/5 border border-white/8 rounded-xl
                       px-4 py-3 text-sm text-gray-200 placeholder-gray-600
                       focus:outline-none focus:border-cyber-accent/40
                       focus:bg-cyber-accent/5 focus:shadow-[0_0_16px_rgba(34,211,238,0.08)]
                       disabled:opacity-50 transition-all duration-200 font-mono"
          />
          {running ? (
            <button
              onClick={handleStop}
              className="px-5 py-3 border border-red-400/40 bg-red-400/10 text-red-400
                         rounded-xl text-sm font-medium hover:bg-red-400/20
                         transition-all duration-200 active:scale-95"
            >
              Detener
            </button>
          ) : (
            <button
              onClick={handleStart}
              disabled={!target.trim()}
              className="px-5 py-3 glass-accent text-cyber-accent rounded-xl text-sm font-medium
                         hover:bg-cyber-accent/15 hover:shadow-[0_0_20px_rgba(34,211,238,0.2)]
                         disabled:opacity-30 disabled:cursor-not-allowed
                         transition-all duration-200 active:scale-95"
            >
              Investigar
            </button>
          )}
        </div>
      </div>

      {/* Timeline / results */}
      <div className="flex-1 overflow-y-auto px-6 py-4">
        {error && (
          <div className="glass rounded-xl border border-red-400/30 px-4 py-3 mb-4 text-sm text-red-400">
            ⚠️ {error}
          </div>
        )}

        {(steps.length > 0 || report || thinking || running) ? (
          <OsintTimeline
            steps={steps}
            report={report}
            thinking={thinking}
            running={running}
          />
        ) : (
          <div className="flex flex-col items-center justify-center h-full text-center text-gray-700 space-y-2">
            <span className="text-4xl">🕵️</span>
            <p className="text-sm">Ingresa un dominio, IP o email para iniciar la investigación.</p>
            <p className="text-xs">El agente usará WHOIS, DNS, crt.sh y GeoIP automáticamente.</p>
          </div>
        )}
      </div>
    </div>
  )
}
