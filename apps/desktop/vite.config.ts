import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";

const configDirectory = fileURLToPath(new URL(".", import.meta.url));

export default defineConfig({
  plugins: [react()],
  base: "./",
  clearScreen: false,
  server: {
    strictPort: true,
    fs: {
      allow: [resolve(configDirectory, "../..")],
    },
  },
});
