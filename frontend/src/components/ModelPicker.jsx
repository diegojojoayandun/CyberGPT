import { useState, useEffect, useRef } from 'react'

async function fetchModels() {
  const res = await fetch('/api/models')
  if (!res.ok) return []
  return res.json()
}

export default function ModelPicker({ selectedModel, onSelect }) {
  const [models, setModels] = useState([])
  const [open, setOpen] = useState(false)
  const [error, setError] = useState(null)
  const ref = useRef(null)

  useEffect(() => {
    fetchModels()
      .then(setModels)
      .catch(() => setError('Ollama no disponible'))
  }, [])

  // Close dropdown on outside click
  useEffect(() => {
    const handler = (e) => { if (ref.current && !ref.current.contains(e.target)) setOpen(false) }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  const currentName = selectedModel ?? 'Modelo por defecto'
  const shortName = currentName.length > 22 ? currentName.slice(0, 22) + '…' : currentName

  return (
    <div ref={ref} className="relative">
      <button
        onClick={() => setOpen(o => !o)}
        className={`flex items-center gap-2 text-xs px-3 py-1.5 rounded-lg border transition-all duration-150
          ${open
            ? 'border-cyber-accent/50 bg-cyber-accent/10 text-cyber-accent'
            : 'border-white/10 text-gray-400 hover:border-white/20 hover:text-gray-300'
          }`}
        title="Seleccionar modelo"
      >
        <span className="text-cyber-accent/70">⬡</span>
        <span>{shortName}</span>
        <span className={`transition-transform duration-200 text-[10px] ${open ? 'rotate-180' : ''}`}>▾</span>
      </button>

      {open && (
        <div className="absolute right-0 top-full mt-1 w-72 glass rounded-xl border border-white/10
                        shadow-[0_8px_32px_rgba(0,0,0,0.5)] z-50 overflow-hidden">
          <div className="px-3 py-2 border-b border-white/5">
            <p className="text-[11px] text-gray-500 uppercase tracking-widest">Modelos instalados</p>
          </div>

          {error ? (
            <div className="px-3 py-3 text-xs text-red-400">{error}</div>
          ) : models.length === 0 ? (
            <div className="px-3 py-3 text-xs text-gray-500">Cargando…</div>
          ) : (
            <div className="max-h-64 overflow-y-auto">
              {/* Default option */}
              <button
                onClick={() => { onSelect(null); setOpen(false) }}
                className={`w-full text-left px-3 py-2.5 flex items-center justify-between
                  hover:bg-white/5 transition-colors text-xs
                  ${selectedModel === null ? 'text-cyber-accent' : 'text-gray-300'}`}
              >
                <span>Modelo por defecto (config)</span>
                {selectedModel === null && <span className="text-cyber-accent text-[10px]">✓</span>}
              </button>

              <div className="border-t border-white/5" />

              {models.map(m => (
                <button
                  key={m.name}
                  onClick={() => { onSelect(m.name); setOpen(false) }}
                  className={`w-full text-left px-3 py-2.5 flex items-center justify-between
                    hover:bg-white/5 transition-colors
                    ${selectedModel === m.name ? 'text-cyber-accent' : 'text-gray-300'}`}
                >
                  <div className="min-w-0">
                    <p className="text-xs truncate">{m.name}</p>
                    {isCyberModel(m.name) && (
                      <p className="text-[10px] text-cyber-accent/60 mt-0.5">★ Recomendado para ciberseg.</p>
                    )}
                  </div>
                  <div className="flex items-center gap-2 shrink-0 ml-2">
                    <span className="text-[10px] text-gray-600">{m.sizeLabel}</span>
                    {selectedModel === m.name && <span className="text-cyber-accent text-[10px]">✓</span>}
                  </div>
                </button>
              ))}
            </div>
          )}

          <div className="px-3 py-2 border-t border-white/5">
            <p className="text-[10px] text-gray-600">
              Para instalar WhiteRabbitNeo:{' '}
              <code className="text-gray-500 bg-black/30 px-1 rounded">
                ollama pull hf.co/WhiteRabbitNeo/...
              </code>
            </p>
          </div>
        </div>
      )}
    </div>
  )
}

// Highlight models known to be good for cybersecurity
function isCyberModel(name) {
  const lower = name.toLowerCase()
  return lower.includes('whiterabbit') ||
         lower.includes('cybersec') ||
         lower.includes('hackergpt') ||
         lower.includes('pentest')
}
