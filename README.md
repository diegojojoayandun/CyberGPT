# CyberGPT

Plataforma RAG de ciberseguridad construida con **.NET 8**, **React + Vite + Tailwind**, **Ollama** y **ChromaDB**.

Consulta documentación técnica (MITRE ATT&CK, OWASP, Active Directory, Malware, Windows Internals) mediante búsqueda híbrida y generación aumentada por recuperación (RAG) 100 % local.

---

## Stack

| Capa | Tecnología |
|------|-----------|
| Frontend | React 18 · Vite · Tailwind CSS · glassmorphism UI |
| Backend | ASP.NET Core 8 Web API · C# |
| LLM | Ollama — cualquier modelo instalado (default: `qwen3:1.7b`) |
| Embeddings | `nomic-embed-text` vía Ollama |
| Vector DB | ChromaDB (servidor local Python) |
| Keyword search | SQLite FTS5 (BM25) |
| Chat history | SQLite · EF Core |

---

## Requisitos

- [Ollama](https://ollama.com/download) para Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- Python 3.11+ (para ChromaDB)

---

## Setup

### 1. Modelos LLM

```bash
# Modelo base (rápido, 1.7B)
ollama pull qwen3:1.7b

# Modelo de embeddings (requerido)
ollama pull nomic-embed-text

# Opcional: modelo especializado en ciberseguridad
ollama pull hf.co/WhiteRabbitNeo/WhiteRabbitNeo-2.5-Qwen-2.5-Coder-7B-Q4_K_M-GGUF
```

### 2. ChromaDB

```bash
pip install chromadb
chroma run --host localhost --port 8000
```

### 3. Backend

```bash
cd backend
dotnet run
# API en http://localhost:5000
```

### 4. Frontend

```bash
cd frontend
npm install
npm run dev
# UI en http://localhost:3000
```

### 5. Indexar conocimiento

Con el backend y ChromaDB corriendo, llama una vez al endpoint de ingesta:

```bash
curl -X POST http://localhost:5000/api/ingest/folder
```

Esto indexa todos los archivos de `knowledge/` en ChromaDB **y** en SQLite FTS5.

---

## Estructura

```
CyberGPT/
├── frontend/
│   └── src/
│       ├── components/
│       │   ├── ChatMessage.jsx    # Markdown, cursor streaming, panel de fuentes RAG
│       │   ├── ChatInput.jsx      # Input + chips de filtro por categoría
│       │   ├── Sidebar.jsx        # Historial de sesiones + prompts rápidos
│       │   └── ModelPicker.jsx    # Selector de modelo Ollama en tiempo real
│       ├── services/api.js        # fetch REST + SSE streaming
│       └── App.jsx
├── backend/
│   ├── Controllers/
│   │   ├── ChatController.cs      # POST /api/chat  +  POST /api/chat/stream (SSE)
│   │   ├── SessionsController.cs  # CRUD de conversaciones
│   │   ├── ModelsController.cs    # GET /api/models (proxy de Ollama /api/tags)
│   │   ├── IngestController.cs    # POST /api/ingest/folder|file
│   │   └── DocumentsController.cs
│   ├── Services/
│   │   ├── OllamaService.cs       # GenerateAsync, StreamAsync, RewriteQueryAsync
│   │   ├── RagService.cs          # Hybrid search + RRF fusion
│   │   ├── ChromaService.cs       # Vector search + filtro por categoría
│   │   ├── KeywordSearchService.cs# SQLite FTS5 (BM25)
│   │   ├── EmbeddingService.cs
│   │   ├── SessionService.cs
│   │   └── DocumentIngester.cs    # Chunking + escritura en Chroma y FTS5
│   ├── Models/
│   │   ├── ChatModels.cs          # ChatRequest, ChatResponse, SessionInfo
│   │   └── SourceChunk.cs         # Content + FileName + Category
│   └── Program.cs
├── knowledge/                     # Tus documentos (no se suben a Git)
│   ├── ATTACK/
│   ├── Malware/
│   ├── ActiveDirectory/
│   ├── WindowsInternals/
│   ├── DotNet/
│   ├── OWASP/
│   └── NotasPersonales/
├── Prompts/
│   └── cybergpt.txt               # System prompt especializado
└── appsettings.json
```

---

## Flujo RAG

```
Pregunta
  │
  ▼
RewriteQueryAsync()          ← LLM reformula la pregunta para mejor recall
  │
  ├─► ChromaDB.QueryAsync()  ← búsqueda semántica (coseno)    ─┐
  │                                                              ├─► RRF fusion
  └─► SQLite FTS5.Search()   ← búsqueda de palabras clave BM25 ─┘
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

**Reciprocal Rank Fusion (k=60):** los documentos que aparecen en ambas listas (semántica + keyword) obtienen score mayor, mejorando la precisión final sin re-entrenamiento.

---

## Características

### Interfaz

- Respuestas en streaming token a token (SSE)
- Panel de fuentes RAG colapsable por mensaje — muestra filename y categoría de cada fragmento usado
- Historial de conversaciones: crear, cargar y eliminar sesiones desde el sidebar
- Chips de filtro por categoría: Todo / MITRE / Active Directory / Malware / OWASP / Windows
- Selector de modelo en tiempo real — lista los modelos instalados en Ollama, resalta automáticamente los especializados en ciberseguridad (WhiteRabbitNeo, HackerGPT, etc.)

### RAG

- **Query rewriting** — reformulación automática de la pregunta antes de buscar
- **Búsqueda híbrida** — semántica (Chroma) + keyword BM25 (SQLite FTS5) fusionadas con RRF
- **Metadata filtering** — filtro por categoría/fuente en Chroma y FTS5
- **Chunking con overlap** — 1000 chars / 200 overlap, soporta PDF, TXT, MD, CS, JSON, YAML

### Modelos

- Cualquier modelo instalado en Ollama es seleccionable desde la UI sin reiniciar
- `"think": false` aplicado automáticamente solo a modelos qwen3 (chain-of-thought desactivado para respuestas más rápidas)
- El query rewriting siempre usa el modelo por defecto (rápido) independientemente del modelo seleccionado para la respuesta

---

## API

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/chat` | Chat completo (respuesta JSON) |
| POST | `/api/chat/stream` | Chat con streaming SSE |
| GET | `/api/models` | Lista modelos instalados en Ollama |
| GET | `/api/sessions` | Lista conversaciones recientes |
| GET | `/api/sessions/{id}/messages` | Mensajes de una sesión |
| DELETE | `/api/sessions/{id}` | Eliminar sesión |
| POST | `/api/ingest/folder` | Indexar carpeta `knowledge/` |
| POST | `/api/ingest/file` | Indexar un archivo específico |

### Ejemplo: chat con streaming

```bash
curl -N -X POST http://localhost:5000/api/chat/stream \
  -H "Content-Type: application/json" \
  -d '{"message":"Explica Kerberoasting","model":"qwen3:1.7b","category":"mitre"}'
```

Respuesta SSE:
```
data: {"sources":[{"fileName":"attack.pdf","category":"mitre","content":"..."}],"sessionId":"..."}
data: {"token":"Kerberoasting"}
data: {"token":" es una técnica..."}
data: {"done":true}
```

---

## Configuración

`backend/appsettings.json`:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen3:1.7b"
  },
  "Chroma": {
    "BaseUrl": "http://localhost:8000"
  },
  "Knowledge": {
    "Path": "../knowledge/{ATTACK,Malware,ActiveDirectory,WindowsInternals,DotNet,ReverseEngineering,OWASP,NotasPersonales}"
  }
}
```

---

## Autor

Diego Fernando Jojoa Yandun — [diegojojoayandun.site](https://diegojojoayandun.site)
