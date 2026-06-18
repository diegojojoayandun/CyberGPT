import { useState, useEffect } from 'react'
import { getHealth } from '../services/api'

function Dot({ status }) {
  const colors = {
    up:      'bg-green-400 shadow-[0_0_6px_rgba(74,222,128,0.6)]',
    down:    'bg-red-400  shadow-[0_0_6px_rgba(248,113,113,0.6)]',
    loading: 'bg-gray-500 animate-pulse'
  }
  return <span className={`inline-block w-1.5 h-1.5 rounded-full ${colors[status]}`} />
}

export default function HealthBadge() {
  const [ollama, setOllama] = useState('loading')
  const [chroma, setChroma] = useState('loading')

  const check = async () => {
    try {
      const data = await getHealth()
      setOllama(data.ollama)
      setChroma(data.chroma)
    } catch {
      setOllama('down')
      setChroma('down')
    }
  }

  useEffect(() => {
    check()
    const id = setInterval(check, 30_000)
    return () => clearInterval(id)
  }, [])

  return (
    <div className="flex items-center gap-2 text-[11px] text-gray-500 select-none" title={`Ollama: ${ollama} · Chroma: ${chroma}`}>
      <span className="flex items-center gap-1"><Dot status={ollama} />Ollama</span>
      <span className="flex items-center gap-1"><Dot status={chroma} />Chroma</span>
    </div>
  )
}
