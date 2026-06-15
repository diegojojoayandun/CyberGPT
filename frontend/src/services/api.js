const BASE = '/api'

export async function sendMessage(message, sessionId = null) {
  const res = await fetch(`${BASE}/chat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message, sessionId })
  })
  if (!res.ok) throw new Error('Error en la API')
  return res.json()
}

export async function uploadDocument(content, fileName, category) {
  const res = await fetch(`${BASE}/documents`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ content, fileName, category })
  })
  if (!res.ok) throw new Error('Error subiendo documento')
  return res.json()
}
