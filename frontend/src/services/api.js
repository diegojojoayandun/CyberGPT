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

export async function streamMessage(message, sessionId, category, model, { onSources, onToken, onDone }, signal, enableThinking = false) {
  const res = await fetch(`${BASE}/chat/stream`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message, sessionId, category, model, enableThinking }),
    signal
  })
  if (!res.ok) throw new Error('Error en la API')

  const reader = res.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let finished = false

  try {
    while (true) {
      const { done, value } = await reader.read()
      if (done) break

      buffer += decoder.decode(value, { stream: true })
      const parts = buffer.split('\n\n')
      buffer = parts.pop()

      for (const part of parts) {
        if (!part.startsWith('data: ')) continue
        try {
          const json = JSON.parse(part.slice(6))
          if (json.sources !== undefined) onSources?.(json.sources, json.sessionId)
          if (json.token !== undefined) onToken?.(json.token)
          if (json.done) { onDone?.(); finished = true; break }
        } catch { /* ignore malformed chunk */ }
      }
      if (finished) break
    }
  } catch (e) {
    if (e.name !== 'AbortError') throw e
  }
}

export async function getSessions() {
  const res = await fetch(`${BASE}/sessions`)
  if (!res.ok) throw new Error('Error obteniendo sesiones')
  return res.json()
}

export async function getSessionMessages(sessionId) {
  const res = await fetch(`${BASE}/sessions/${sessionId}/messages`)
  if (!res.ok) throw new Error('Error cargando mensajes')
  return res.json()
}

export async function deleteSession(sessionId) {
  const res = await fetch(`${BASE}/sessions/${sessionId}`, { method: 'DELETE' })
  if (!res.ok) throw new Error('Error eliminando sesión')
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

export async function uploadPdf(file, category) {
  const form = new FormData()
  form.append('file', file)
  form.append('category', category)
  const res = await fetch(`${BASE}/documents/pdf`, { method: 'POST', body: form })
  if (!res.ok) throw new Error('Error subiendo PDF')
  return res.json()
}

export async function getHealth() {
  const res = await fetch(`${BASE}/health`, { signal: AbortSignal.timeout(4000) })
  if (!res.ok) throw new Error('Health check failed')
  return res.json()
}

export async function streamOsint(target, targetType = 'auto', { onEvent }, signal) {
  const res = await fetch(`${BASE}/osint/investigate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ target, targetType }),
    signal
  })
  if (!res.ok) throw new Error('Error en OSINT API')

  const reader  = res.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  try {
    while (true) {
      const { done, value } = await reader.read()
      if (done) break

      buffer += decoder.decode(value, { stream: true })
      const parts = buffer.split('\n\n')
      buffer = parts.pop()

      for (const part of parts) {
        if (!part.startsWith('data: ')) continue
        try {
          const evt = JSON.parse(part.slice(6))
          onEvent?.(evt)
        } catch { /* ignore malformed */ }
      }
    }
  } catch (e) {
    if (e.name !== 'AbortError') throw e
  }
}
