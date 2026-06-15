import ReactMarkdown from 'react-markdown'

export default function ChatMessage({ role, content }) {
  const isUser = role === 'user'
  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'} mb-4`}>
      <div className={`max-w-[80%] rounded-lg px-4 py-3 text-sm leading-relaxed
        ${isUser
          ? 'bg-cyber-accent/10 border border-cyber-accent/30 text-cyber-accent'
          : 'bg-cyber-panel border border-cyber-border text-gray-200'
        }`}>
        {isUser ? (
          <p>{content}</p>
        ) : (
          <ReactMarkdown
            components={{
              code: ({children}) => (
                <code className="bg-black/40 rounded px-1 text-cyber-green font-mono text-xs">
                  {children}
                </code>
              ),
              pre: ({children}) => (
                <pre className="bg-black/60 rounded p-3 overflow-x-auto my-2 border border-cyber-border">
                  {children}
                </pre>
              )
            }}
          >
            {content}
          </ReactMarkdown>
        )}
      </div>
    </div>
  )
}
