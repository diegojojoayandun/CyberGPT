# CyberGPT

Plataforma de ciberseguridad con RAG local y agente OSINT autónomo. Corre 100% offline con modelos locales via Ollama.

---

## Stack

| Capa | Tecnología |
|---|---|
| Frontend | React 18 · Vite · Tailwind CSS (glassmorphism) |
| Backend | ASP.NET Core 8 · C# |
| LLM | Ollama — cualquier modelo instalado (default: `qwen3:4b`) |
| Embeddings | `nomic-embed-text` via Ollama |
| Vector DB | ChromaDB (servidor local) |
| Keyword search | SQLite FTS5 (BM25) |
| Historial | SQLite · EF Core |
| Resiliencia | Polly — retry + circuit breaker |

---

## Características

### Chat RAG
- **Búsqueda híbrida** — ChromaDB (semántico coseno) + SQLite FTS5 (BM25) fusionados con RRF (k=60)
- **Query rewriting** — el LLM reformula la pregunta antes de buscar para mejorar recall
- **Historial de conversación** — 6 turnos de memoria por sesión (SQLite)
- **Gestión de sesiones** — crear, cargar y eliminar conversaciones desde el sidebar
- **Streaming SSE** — tokens en tiempo real via `POST /api/chat/stream`
- **Panel de fuentes RAG** — cada respuesta muestra los chunks usados (fileName + categoría)
- **Model picker** — cambia el modelo Ollama en runtime desde el header
- **Filtro por categoría** — chips para MITRE ATT&CK, AD, Malware, OWASP, Windows, etc.
- **Think toggle** — activa/desactiva el razonamiento interno de qwen3 (`think: true/false`)
- **Stop button** — cancela la generación en mitad del stream (AbortController)

### Ingesta de documentos
- Upload de PDF/TXT/MD desde el sidebar (drag & drop, hasta 50 MB)
- `POST /api/documents/pdf` con override de fileName y categoría
- Escribe en ChromaDB y SQLite FTS5 simultáneamente
- Chunking con overlap: 1000 chars / 200 overlap

### Agente OSINT autónomo
Loop ReAct de hasta 12 iteraciones con 7 herramientas. Genera un reporte estructurado en español.

| Herramienta | Qué investiga | API key |
|---|---|---|
| `whois_lookup` | WHOIS/RDAP para dominios e IPs | No (rdap.org) |
| `dns_lookup` | A, AAAA, MX, NS, TXT, CNAME, SOA | No (Google DoH) |
| `subdomain_discovery` | Subdominios via certificados TLS | No (crt.sh) |
| `geoip_lookup` | Geolocalización, ISP, ASN | No (ip-api.com) |
| `shodan_lookup` | Puertos abiertos, CVEs, banners | Opcional (sin key usa InternetDB gratis) |
| `virustotal_lookup` | Reputación en 70+ motores AV | Sí — free 500 req/día |
| `whatsapp_osint` | Estado, cuenta Business, dispositivos, privacidad | Sí — RapidAPI |

**Targets soportados:** dominio · IP · email · username · número de teléfono · hash de archivo

El reporte final incluye:
- Resumen ejecutivo
- Hallazgos técnicos (por herramienta)
- Indicadores de riesgo
- Conclusiones y recomendaciones

### Infraestructura
- **Health badge** — `GET /api/health` pingea Ollama y Chroma cada 30s con dots verde/rojo en el header
- **Polly resilience** — retry exponencial (3 intentos, delay 2s) + circuit breaker en llamadas a Ollama y embeddings
- **Deduplicación de tool calls** — el agente OSINT detecta y bloquea llamadas repetidas
- **TOOL_UNAVAILABLE** — tools sin API key retornan señal explícita para que el agente las saltee sin reintentar

---

## Requisitos

- [Ollama](https://ollama.com/download)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- ChromaDB (ver abajo)

```bash
# ChromaDB via pip
pip install chromadb
chroma run --host localhost --port 8000

# O via Docker
docker run -p 8000:8000 chromadb/chroma

# Modelos recomendados
ollama pull qwen3:4b
ollama pull nomic-embed-text
```

---

## Instalación

```bash
git clone https://github.com/diegojojoayandun/CyberGPT.git
cd CyberGPT

# Backend
cd backend
dotnet restore
dotnet run      # http://localhost:5000

# Frontend (nueva terminal)
cd frontend
npm install
npm run dev     # http://localhost:3000
```

---

## Configuración

`backend/appsettings.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen3:4b"
  },
  "Chroma": {
    "BaseUrl": "http://localhost:8000"
  },
  "Knowledge": {
    "Path": "../knowledge/{ATTACK,Malware,ActiveDirectory,WindowsInternals,DotNet,ReverseEngineering,OWASP,NotasPersonales}"
  },
  "Osint": {
    "ShodanApiKey":     "",
    "VirusTotalApiKey": "",
    "RapidApiKey":      ""
  }
}
```

| Key | Dónde conseguirla | Plan gratuito |
|---|---|---|
| `ShodanApiKey` | [shodan.io](https://shodan.io) | Opcional — sin key usa InternetDB |
| `VirusTotalApiKey` | [virustotal.com](https://virustotal.com) | 500 req/día |
| `RapidApiKey` | [rapidapi.com → whatsapp-osint](https://rapidapi.com/inutil-inutil-default/api/whatsapp-osint) | Plan Basic |

---

## API

### Chat
| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/chat/stream` | Chat con RAG, streaming SSE |
| `GET` | `/api/models` | Modelos instalados en Ollama |

### Sesiones
| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/sessions` | Listar sesiones |
| `GET` | `/api/sessions/{id}/messages` | Mensajes de una sesión |
| `DELETE` | `/api/sessions/{id}` | Eliminar sesión |

### Documentos
| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/documents/pdf` | Ingestar PDF/TXT/MD (multipart, 50 MB max) |

### OSINT
| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/osint/investigate` | Iniciar investigación OSINT (SSE streaming) |

### Sistema
| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/health` | Estado de Ollama y ChromaDB |

### Ejemplo: chat streaming

```bash
curl -N -X POST http://localhost:5000/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"message":"Explica Kerberoasting","model":"qwen3:4b","category":"mitre"}'
```

### Ejemplo: OSINT

```bash
curl -N -X POST http://localhost:5000/api/osint/investigate \
  -H "Content-Type: application/json" \
  -d '{"target":"example.com","targetType":"domain"}'
```

---

## Flujo RAG

```
Pregunta
  │
  ▼
RewriteQueryAsync()          ← LLM reformula para mejor recall
  │
  ├─► ChromaDB.QueryAsync()  ← semántico (coseno)  ─┐
  │                                                   ├─► RRF fusion (k=60)
  └─► SQLite FTS5.Search()   ← keyword BM25         ─┘
                                                      │
                                                      ▼
                                               Top-5 SourceChunks
                                                      │
                                                      ▼
                                          OllamaService.StreamAsync()
                                                      │
                                                      ▼
                                          SSE token-by-token → UI
```

## Flujo OSINT Agent

```
Target (dominio / IP / teléfono / hash)
  │
  ▼
OsintAgentService — ReAct loop (max 12 iteraciones)
  │
  ├─► LLM decide qué tool usar
  ├─► Tool ejecuta (WHOIS / DNS / Shodan / VT / WhatsApp / ...)
  ├─► Resultado vuelve al contexto del LLM
  └─► Repite hasta que el LLM escribe el reporte final
  │
  ▼
SSE events → OsintTimeline (frontend)
  { type: "tool_start" | "tool_done" | "thinking" | "report" | "done" }
```

---

## Base de conocimiento

Coloca documentos en `knowledge/` organizado por categorías:

```
knowledge/
  ATTACK/              # MITRE ATT&CK
  Malware/
  ActiveDirectory/
  WindowsInternals/
  DotNet/
  ReverseEngineering/
  OWASP/
  NotasPersonales/
```

O usa el panel de upload en el sidebar para ingestar en runtime.

---

## Autor

Diego Fernando Jojoa Yandun — [diegojojoayandun.site](https://diegojojoayandun.site)
