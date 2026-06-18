import { useState } from 'react'
import ReactMarkdown from 'react-markdown'

const TOOL_LABELS = {
  whois_lookup:        { icon: '🔎', label: 'WHOIS / RDAP' },
  dns_lookup:          { icon: '🌐', label: 'DNS Records' },
  subdomain_discovery: { icon: '🗂️',  label: 'Subdominios (crt.sh)' },
  geoip_lookup:        { icon: '📍', label: 'GeoIP / ASN' },
  shodan_lookup:       { icon: '🛰️',  label: 'Shodan — Puertos & CVEs' },
  virustotal_lookup:   { icon: '🦠', label: 'VirusTotal — Reputación' },
  whatsapp_osint:      { icon: '📱', label: 'WhatsApp OSINT' },
}

function ToolCard({ step }) {
  const [open, setOpen] = useState(false)
  const meta = TOOL_LABELS[step.tool] ?? { icon: '🔧', label: step.tool }
  const isDone    = step.status === 'done'
  const isRunning = step.status === 'running'

  return (
    <div className={`glass rounded-xl border transition-all duration-200
      ${isDone    ? 'border-white/10' : ''}
      ${isRunning ? 'border-cyber-accent/30 shadow-[0_0_12px_rgba(34,211,238,0.1)]' : ''}
    `}>
      <button
        className="w-full flex items-center gap-3 px-4 py-3 text-left"
        onClick={() => isDone && setOpen(v => !v)}
        disabled={!isDone}
      >
        <span className="text-base">{meta.icon}</span>
        <div className="flex-1 min-w-0">
          <span className="text-sm text-gray-300 font-medium">{meta.label}</span>
          {step.target && (
            <span className="text-xs text-gray-600 ml-2 font-mono">{step.target}</span>
          )}
        </div>
        {isRunning && (
          <span className="text-[11px] text-cyber-accent animate-pulse">ejecutando…</span>
        )}
        {isDone && (
          <span className="text-[11px] text-green-400">✓ {open ? '▴' : '▾'}</span>
        )}
      </button>
      {open && step.result && (
        <div className="px-4 pb-3 border-t border-white/5">
          <pre className="text-[11px] text-gray-400 whitespace-pre-wrap mt-2 font-mono leading-relaxed">
            {step.result}
          </pre>
        </div>
      )}
    </div>
  )
}

export default function OsintTimeline({ steps, report, thinking, running }) {
  return (
    <div className="flex flex-col gap-3">
      {/* Thinking / planning bubble */}
      {thinking && (
        <div className="glass rounded-xl border border-purple-400/20 px-4 py-3">
          <p className="text-[11px] text-purple-400 uppercase tracking-widest mb-1">Planificando</p>
          <p className="text-xs text-gray-400">{thinking}</p>
        </div>
      )}

      {/* Tool steps */}
      {steps.map((step, i) => (
        <ToolCard key={i} step={step} />
      ))}

      {/* Still running spinner */}
      {running && steps.length === 0 && (
        <div className="text-center py-8 text-gray-600 text-sm animate-pulse">
          Iniciando investigación…
        </div>
      )}

      {/* Final report */}
      {report && (
        <div className="glass rounded-xl border border-cyber-accent/20 px-5 py-4
                        shadow-[0_0_20px_rgba(34,211,238,0.06)]">
          <p className="text-[11px] text-cyber-accent uppercase tracking-widest mb-3">
            Reporte de inteligencia
          </p>
          <div className="prose prose-invert prose-sm max-w-none text-gray-300
                          prose-headings:text-cyber-accent prose-headings:text-sm
                          prose-strong:text-gray-200 prose-code:text-cyber-accent/80">
            <ReactMarkdown>{report}</ReactMarkdown>
          </div>
        </div>
      )}
    </div>
  )
}
