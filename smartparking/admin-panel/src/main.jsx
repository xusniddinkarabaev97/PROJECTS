import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { AuthProvider } from "./auth/AuthContext";
import { LanguageProvider } from "./i18n/LanguageContext";
import App from "./App";
import "./index.css";

const rootEl = document.getElementById("root");
try {
  createRoot(rootEl).render(
    <StrictMode>
      <AuthProvider>
        <LanguageProvider>
          <App />
        </LanguageProvider>
      </AuthProvider>
    </StrictMode>,
  );
} catch (e) {
  rootEl.innerHTML = "<div style='color:red;padding:20px;font-family:sans-serif'><h2>App Error</h2><pre>" + e.message + "</pre></div>";
}
