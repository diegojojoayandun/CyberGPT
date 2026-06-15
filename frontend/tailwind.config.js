/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,jsx}'],
  theme: {
    extend: {
      colors: {
        cyber: {
          dark:   '#0a0e1a',
          panel:  '#111827',
          border: '#1f2937',
          accent: '#22d3ee',
          green:  '#4ade80',
          red:    '#f87171',
        }
      },
      fontFamily: {
        mono: ['"JetBrains Mono"', '"Fira Code"', 'monospace']
      }
    }
  },
  plugins: []
}
