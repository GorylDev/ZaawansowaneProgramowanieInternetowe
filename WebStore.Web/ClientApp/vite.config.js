import { defineConfig } from 'vite';

export default defineConfig({
  build: { outDir: '../wwwroot', emptyOutDir: true },
  server: { port: 5173, strictPort: true },
});
