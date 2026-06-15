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
    <form onSubmit={handleSubmit} className="flex gap-2 p-4 border-t border-cyber-border">
      <input
        value={value}
        onChange={e => setValue(e.target.value)}
        placeholder="Pregunta sobre ATT&CK, malware, AD, Windows events..."
        disabled={disabled}
        className="flex-1 bg-cyber-panel border border-cyber-border rounded-lg
                   px-4 py-3 text-sm text-gray-200 placeholder-gray-600
                   focus:outline-none focus:border-cyber-accent/60
                   disabled:opacity-50"
      />
      <button
        type="submit"
        disabled={disabled || !value.trim()}
        className="px-5 py-3 bg-cyber-accent/20 border border-cyber-accent/40
                   text-cyber-accent rounded-lg text-sm font-medium
                   hover:bg-cyber-accent/30 disabled:opacity-40
                   transition-colors"
      >
        Enviar
      </button>
    </form>
  )
}
