import { defineConfig } from "vite";

export default defineConfig({
  root: "injector-ui",
  clearScreen: false,
  server: { port: 5173, strictPort: true },
  build: {
    outDir: "../dist-injector",
    emptyOutDir: true,
    target: "chrome105",
    sourcemap: false,
  },
});
