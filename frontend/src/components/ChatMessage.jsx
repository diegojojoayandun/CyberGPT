import { useState } from 'react'
import ReactMarkdown from 'react-markdown'

function SourcesPanel({ sources }) {
  const [open, setOpen] = useState(false)
  if (!sources || sources.length === 0) return null

  return (
    <div className="mt-2 border-t border-white/5 pt-2">
      <button
        onClick={() => setOpen(o => !o)}
        className="flex items-center gap-1.5 text-[11px] text-gray-500 hover:text-cyber-accent transition-colors"
      >
        <span className={`transition-transform duration-200 inline-block ${open ? 'rotate-90' : ''}`}>▶</span>
        <span>{sources.length} fuente{sources.length !== 1 ? 's' : ''} RAG</span>
      </button>

      {open && (
        <div className="mt-2 flex flex-col gap-2">
          {sources.map((src, i) => {
            // src can be a string (legacy) or object {fileName, category, content}
            const content  = typeof src === 'string' ? src : src.content  ?? src
            const fileName = typeof src === 'object' ? src.fileName : null
            const category = typeof src === 'object' ? src.category : null

            return (
              <div key={i} className="glass rounded-lg px-3 py-2 border-l-2 border-cyber-accent/30">
                <div className="flex items-center gap-2 mb-1">
                  <p className="text-[10px] text-gray-600 uppercase tracking-wider">Fragmento {i + 1}</p>
                  {fileName && (
                    <span className="text-[10px] text-cyber-accent/50 truncate max-w-[160px]" title={fileName}>
                      {fileName}
                    </span>
                  )}
                  {category && (
                    <span className="text-[10px] px-1.5 py-0.5 rounded-full border border-cyber-accent/20 text-cyber-accent/50">
                      {category}
                    </span>
                  )}
                </div>
                <p className="text-xs text-gray-400 leading-relaxed line-clamp-4">
                  {content.length > 300 ? content.slice(0, 300) + '…' : content}
                </p>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

export default function ChatMessage({ role, content, sources, streaming }) {
  const isUser = role === 'user'
  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'} mb-4`}>
      <div className={`max-w-[80%] rounded-2xl px-4 py-3 text-sm leading-relaxed
        ${isUser ? 'glass-user text-cyber-accent' : 'glass text-gray-200'}
      `}>
        {isUser ? (
          <p>{content}</p>
        ) : (
          <>
            <ReactMarkdown
              components={{
                code: ({children}) => (
                  <code className="bg-black/50 rounded px-1.5 py-0.5 text-cyber-green font-mono text-xs
                                   border border-white/5">
                    {children}
                  </code>
                ),
                pre: ({children}) => (
                  <pre className="bg-black/60 rounded-xl p-3 overflow-x-auto my-2
                                  border border-white/5">
                    {children}
                  </pre>
                ),
                strong: ({children}) => (
                  <strong className="text-cyber-accent font-semibold">{children}</strong>
                ),
                h1: ({children}) => <h1 className="text-cyber-accent text-base font-bold mt-3 mb-1">{children}</h1>,
                h2: ({children}) => <h2 className="text-cyber-accent text-sm font-bold mt-3 mb-1">{children}</h2>,
                h3: ({children}) => <h3 className="text-gray-300 text-sm font-semibold mt-2 mb-1">{children}</h3>,
                li: ({children}) => (
                  <li className="flex gap-2 my-0.5">
                    <span className="text-cyber-accent mt-0.5">▸</span>
                    <span>{children}</span>
                  </li>
                ),
                ul: ({children}) => <ul className="my-1 space-y-0.5">{children}</ul>,
              }}
            >
              {content}
            </ReactMarkdown>
            {streaming && (
              <span className="inline-block w-1.5 h-4 bg-cyber-accent ml-0.5 animate-pulse rounded-sm align-middle" />
            )}
            <SourcesPanel sources={sources} />
          </>
        )}
      </div>
    </div>
  )
}
