import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  build: {
    rolldownOptions: {
      output: {
        // React changes less often than our code: its own chunk stays cached between releases.
        // Ant Design is split by the pages that use it (routes load pages lazily).
        codeSplitting: {
          groups: [{ name: 'react', test: /node_modules[\\/](react|react-dom|react-router|scheduler)[\\/]/ }],
        },
      },
    },
  },
  server: {
    port: 5174,
    proxy: { '/api': 'http://localhost:5080' },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.js'],
    // Ant Design forms and dialogs are slow to drive in jsdom, especially with many workers.
    testTimeout: 60000,
    // jsdom + Ant Design is CPU-heavy: more workers than half the cores only makes every test slower.
    maxWorkers: '50%',
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{js,jsx}'],
      exclude: ['src/main.jsx', 'src/test/**', 'src/**/*.test.{js,jsx}'],
      thresholds: { lines: 90, branches: 90, functions: 90, statements: 90 },
      reporter: ['text', 'html', 'lcov'],
    },
  },
})
