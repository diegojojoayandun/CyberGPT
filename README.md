# CyberGPT 🛡️

Plataforma RAG de ciberseguridad construida con **.NET 8**, **React + Vite + Tailwind**, **Ollama** y **ChromaDB**.

Consulta documentación técnica (MITRE ATT&CK, OWASP, AD, Malware) mediante búsqueda semántica y generación aumentada por recuperación (RAG).

## Stack

| Capa | Tecnología |
|------|-----------|
| Frontend | React 18 · Vite · Tailwind CSS |
| Backend | ASP.NET Core 8 Web API · C# |
| LLM | Ollama · qwen3:4b (local, sin GPU requerida) |
| Vector DB | ChromaDB (servidor local Python) |
| Chat history | SQLite · EF Core |

## Requisitos

- [Ollama](https://ollama.com/download) para Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- Python 3.11+ (para ChromaDB)

## Setup (sin Docker)

### 1. Modelo LLM
```bash
ollama pull qwen3:4b
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
# Corre en http://localhost:5000
```

### 4. Frontend
```bash
cd frontend
npm install
npm run dev
# Corre en http://localhost:3000
```

Abre 4 terminales, una por cada servicio. Eso es todo.

## Estructura

```
CyberGPT/
├── frontend/          # React + Vite + Tailwind
│   └── src/
│       ├── components/   # ChatMessage, ChatInput, Sidebar
│       ├── services/     # api.js (fetch al backend)
│       └── App.jsx
├── backend/           # ASP.NET Core 8 Web API
│   ├── Controllers/   # ChatController, DocumentsController
│   ├── Services/      # OllamaService, ChromaService, RagService
│   ├── Models/        # ChatModels
│   └── Program.cs
├── knowledge/         # Tus documentos técnicos (no se suben a Git)
│   ├── ATTACK/
│   ├── Malware/
│   ├── ActiveDirectory/
│   ├── WindowsInternals/
│   ├── DotNet/
│   ├── OWASP/
│   └── NotasPersonales/
├── prompts/
│   └── cybergpt.txt   # System prompt especializado
└── README.md
```

## Flujo RAG

```
Pregunta → Backend → ChromaDB (búsqueda semántica) → contexto relevante
                   → Ollama (qwen3:4b + contexto) → respuesta técnica
```

## Características

- 💬 Interfaz tipo ChatGPT con tema terminal dark
- 📚 RAG sobre documentación técnica propia
- 🔍 Búsqueda semántica con embeddings
- 🧠 System prompt especializado en ciberseguridad
- ⚡ 100% local, sin APIs externas de pago

## Para el CV

> Desarrollé CyberGPT, una plataforma RAG para ciberseguridad construida con .NET 8, React y Ollama, capaz de consultar documentación MITRE ATT&CK, OWASP y conocimiento técnico personalizado mediante búsqueda semántica.

## Autor

Diego Fernando Jojoa Yandun — [diegojojoayandun.site](https://diegojojoayandun.site)
