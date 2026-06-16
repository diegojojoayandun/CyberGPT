import ReactMarkdown from 'react-markdown'

export default function ChatMessage({ role, content }) {
  const isUser = role === 'user'
  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'} mb-4`}>
      <div className={`max-w-[80%] rounded-2xl px-4 py-3 text-sm leading-relaxed
        ${isUser ? 'glass-user text-cyber-accent' : 'glass text-gray-200'}
      `}>
        {isUser ? (
          <p>{content}</p>
        ) : (
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
        )}
      </div>
    </div>
  )
}
