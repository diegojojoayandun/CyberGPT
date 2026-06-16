const QUICK_PROMPTS = [
  'Explica Kerberoasting y cómo detectarlo en logs',
  'Mapea Pass-the-Hash a MITRE ATT&CK',
  'Analiza un evento Windows 4625',
  'Qué hace el grupo APT29 típicamente',
  'Diferencia entre NTLM y Kerberos',
  'Cómo detectar LSASS dumping',
]

export default function Sidebar({ onSelect }) {
  return (
    <aside className="glass-panel w-64 flex flex-col relative z-10">
      <div className="p-5 border-b border-white/5">
        <div className="flex items-center gap-2">
          <span className="text-cyber-accent text-lg font-bold glow-text">⚡ CyberGPT</span>
        </div>
        <p className="text-xs text-gray-500 mt-1 tracking-widest uppercase">RAG · Ciberseguridad</p>
      </div>

      <div className="p-4 flex-1 overflow-y-auto">
        <p className="text-xs text-gray-600 uppercase tracking-widest mb-3">Preguntas rápidas</p>
        <div className="flex flex-col gap-2">
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
