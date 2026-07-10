import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    host: "0.0.0.0",
    port: 5174,
    allowedHosts: true,
    proxy: {
      "/api": {
        target: "http://localhost:5121",
        changeOrigin: true,
      },
      "/swagger": {
        target: "http://localhost:5121",
        changeOrigin: true,
      },
      "/payment": {
        target: "http://localhost:5121",
        changeOrigin: true,
      },
      "/Billing": {
        target: "http://localhost:5121",
        changeOrigin: true,
      },
    },
  },
});
