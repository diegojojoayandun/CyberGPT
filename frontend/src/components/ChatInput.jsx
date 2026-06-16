import { useState } from 'react'

export default function ChatInput({ onSend, disabled }) {
  const [value, setValue] = useState('')

  const handleSubmit = (e) => {
    e.preventDefault()
    if (!value.trim() || disabled) return
    onSend(value.trim())
    setValue('')
  }

  return (
    <form onSubmit={handleSubmit}
          className="glass flex gap-2 p-4 border-t border-white/5">
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
  )
}
