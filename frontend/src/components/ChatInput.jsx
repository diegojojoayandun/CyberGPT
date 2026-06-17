import { useState } from 'react'

const CATEGORIES = [
  { id: null,      label: 'Todo' },
  { id: 'mitre',   label: 'MITRE' },
  { id: 'ad',      label: 'Active Directory' },
  { id: 'malware', label: 'Malware' },
  { id: 'owasp',   label: 'OWASP' },
  { id: 'windows', label: 'Windows' },
]

export default function ChatInput({ onSend, disabled, onCategoryChange }) {
  const [value, setValue] = useState('')
  const [activeCategory, setActiveCategory] = useState(null)

  const handleSubmit = (e) => {
    e.preventDefault()
    if (!value.trim() || disabled) return
    onSend(value.trim())
    setValue('')
  }

  const handleCategory = (id) => {
    setActiveCategory(id)
    onCategoryChange?.(id)
  }

  return (
    <div className="glass border-t border-white/5">
      {/* Category filter chips */}
      <div className="px-4 pt-3 flex gap-2 flex-wrap">
        {CATEGORIES.map(cat => (
          <button
            key={String(cat.id)}
            onClick={() => handleCategory(cat.id)}
            className={`text-[11px] px-2.5 py-1 rounded-full border transition-all duration-150
              ${activeCategory === cat.id
                ? 'border-cyber-accent/60 bg-cyber-accent/15 text-cyber-accent'
                : 'border-white/10 text-gray-500 hover:border-white/20 hover:text-gray-400'
              }`}
          >
            {cat.label}
          </button>
        ))}
      </div>

      <form onSubmit={handleSubmit} className="flex gap-2 p-4">
        <input
          value={value}
          onChange={e => setValue(e.target.value)}
          placeholder="Pregunta sobre ATT&CK, malware, AD, Windows events..."
          disabled={disabled}
          className="flex-1 bg-white/5 border border-white/8 rounded-xl
                     px-4 py-3 text-sm text-gray-200 placeholder-gray-600
                     focus:outline-none focus:border-cyber-accent/40
                     focus:bg-cyber-accent/5 focus:shadow-[0_0_16px_rgba(34,211,238,0.08)]
                     disabled:opacity-50 transition-all duration-200
                     backdrop-blur-[3px]"
        />
        <button
          type="submit"
          disabled={disabled || !value.trim()}
          className="px-5 py-3 glass-accent text-cyber-accent rounded-xl text-sm font-medium
                     hover:bg-cyber-accent/15 hover:shadow-[0_0_20px_rgba(34,211,238,0.2)]
                     disabled:opacity-30 disabled:cursor-not-allowed
                     transition-all duration-200 active:scale-95"
        >
          Enviar
        </button>
      </form>
    </div>
  )
}
