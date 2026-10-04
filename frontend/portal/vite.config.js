import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  css: {
    // CSS Modules keep the BEM names as written (styles['offer-card__title']). Don't set
    // modules.localsConvention: with Vite 8 it leaves the class maps empty in production builds.
    // Component stylesheets: `@use 'styles/mixins' as *;`
    preprocessorOptions: { scss: { loadPaths: [fileURLToPath(new URL('./src', import.meta.url))] } },
  },
  build: {
    rolldownOptions: {
      output: {
        // React and Redux change less often than our code: their own chunks stay cached between releases.
        codeSplitting: {
          groups: [
            { name: 'react', test: /node_modules[\\/](react|react-dom|react-router|scheduler)[\\/]/ },
            { name: 'redux', test: /node_modules[\\/](@reduxjs|redux|react-redux|immer|reselect)[\\/]/ },
          ],
        },
      },
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
      '/hubs': { target: 'http://localhost:5080', ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.js'],
    // Slower machines (and coverage) need more than the 5 s default for the longer user flows.
    testTimeout: 30_000,
    css: { modules: { classNameStrategy: 'non-scoped' } },
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{js,jsx}'],
      exclude: ['src/main.jsx', 'src/test/**', 'src/**/*.test.{js,jsx}'],
      thresholds: { lines: 90, branches: 90, functions: 90, statements: 90 },
      reporter: ['text', 'html', 'lcov'],
    },
  },
})
