import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Cererile către /api merg la API-ul .NET. Browserul vede o singură adresă,
// deci cookie-ul de autentificare funcționează fără CORS.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5269',
    },
  },
})
