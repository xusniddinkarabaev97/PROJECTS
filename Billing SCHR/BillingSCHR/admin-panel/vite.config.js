import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  base: "/billing-schr/admin/",
  server: {
    host: "0.0.0.0",
    port: 5174,
    allowedHosts: true,
    proxy: {
      "/api": {
        target: "http://localhost:5122",
        changeOrigin: true,
      },
      "/swagger": {
        target: "http://localhost:5122",
        changeOrigin: true,
      },
      "/payment": {
        target: "http://localhost:5122",
        changeOrigin: true,
      },
      "/Billing": {
        target: "http://localhost:5122",
        changeOrigin: true,
      },
    },
  },
});
