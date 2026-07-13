import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    allowedHosts: [
      "prolonged-implosion-parade.ngrok-free.dev",
      ".ngrok-free.dev",
      "localhost",
    ],
    proxy: {
      "/api": {
        target: "http://localhost:5114",
        changeOrigin: true,
        secure: false,
      },
      "/swagger": {
        target: "http://localhost:5114",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
