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
    <aside className="w-64 bg-cyber-panel border-r border-cyber-border flex flex-col">
      <div className="p-4 border-b border-cyber-border">
        <div className="flex items-center gap-2">
          <span className="text-cyber-accent text-lg font-bold">⚡ CyberGPT</span>
        </div>
        <p className="text-xs text-gray-500 mt-1">RAG · Ciberseguridad</p>
      </div>

      <div className="p-4 flex-1 overflow-y-auto">
        <p className="text-xs text-gray-500 uppercase tracking-wider mb-3">Preguntas rápidas</p>
        <div className="flex flex-col gap-2">
          {QUICK_PROMPTS.map((p, i) => (
            <button
              key={i}
              onClick={() => onSelect(p)}
              className="text-left text-xs text-gray-400 hover:text-cyber-accent
                         border border-cyber-border hover:border-cyber-accent/40
                         rounded-lg px-3 py-2 transition-colors"
            >
              {p}
            </button>
          ))}
        </div>
      </div>

      <div className="p-4 border-t border-cyber-border">
        <p className="text-xs text-gray-600">MITRE · OWASP · AD · Malware</p>
      </div>
    </aside>
  )
}
