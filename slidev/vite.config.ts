import { defineConfig } from 'vite'
import { resolve } from 'path'

export default defineConfig({
  define: {
    'process.env.AUDIENCE': JSON.stringify(process.env.AUDIENCE || ''),
  },
  resolve: {
    alias: {
      '@': resolve(__dirname),
    },
  },
})
