import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/parcels': 'http://localhost:8080',
      '/parcel': 'http://localhost:8080'
    }
  }
})
