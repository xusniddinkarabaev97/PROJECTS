import { useState } from "react";
import { NavLink } from "./NavLink";
import { useTranslation } from "../i18n/LanguageContext";
import { useAuth } from "../auth/AuthContext";

export default function Layout({ children }) {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const { t, lang, changeLanguage } = useTranslation();
  const { user, logout } = useAuth();

  const menuItems = [
    { path: "/", label: t("dashboard"), icon: "📊" },
    { path: "/transactions", label: t("transactions"), icon: "💳" },
    { path: "/fiscal", label: t("fiscal"), icon: "🧾" },
    { path: "/reconciliation", label: t("reconciliation"), icon: "🔍" },
    { path: "/reports", label: t("reports"), icon: "📥" },
    { path: "/users", label: t("users"), icon: "👥" },
    { path: "/integrations", label: t("integrations"), icon: "🔌" },
    { path: "/audit", label: t("auditLogs"), icon: "📋" },
  ];

  return (
    <div style={{ minHeight: "100vh", display: "flex", background: "linear-gradient(135deg, #0d1410 0%, #162218 50%, #0f1712 100%)" }}>
      {/* Sidebar */}
      <aside className="sidebar" style={{ position: "fixed", top: 0, left: 0, height: "100%", width: sidebarOpen ? 260 : 60, transition: "width 0.2s ease", display: "flex", flexDirection: "column", zIndex: 30 }}>
        <div style={{ padding: "20px 16px", borderBottom: "1px solid var(--border)" }}>
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <span style={{ fontSize: 28, filter: "drop-shadow(0 0 8px rgba(91,140,62,0.4))" }}>🎖️</span>
            {sidebarOpen && (
              <div>
                <h1 style={{ fontSize: 14, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#ffffff", whiteSpace: "nowrap", letterSpacing: "0.5px", textTransform: "uppercase" }}>BillingSCHR</h1>
                <p style={{ fontSize: 10, color: "var(--accent)", fontWeight: 600, letterSpacing: "1px" }}>БИЛЛИНГ МПР</p>
              </div>
            )}
          </div>
        </div>

        <nav style={{ flex: 1, padding: 8, overflowY: "auto" }}>
          {menuItems.map((item) => (
            <NavLink key={item.path} to={item.path} collapsed={!sidebarOpen}>
              <span style={{ fontSize: 18, width: 24, textAlign: "center" }}>{item.icon}</span>
              {sidebarOpen && <span>{item.label}</span>}
            </NavLink>
          ))}
        </nav>

        <div style={{ padding: 12, borderTop: "1px solid var(--border)" }}>
          {sidebarOpen && (
            <div style={{ marginBottom: 10, padding: "10px 12px", background: "rgba(91, 140, 62, 0.06)", borderRadius: 10, border: "1px solid var(--border)" }}>
              <div style={{ fontSize: 12, fontWeight: 600, color: "var(--accent)", marginBottom: 2 }}>👤 {user?.login || "admin"}</div>
              <div style={{ fontSize: 11, color: "var(--text-muted)" }}>Admin</div>
            </div>
          )}
          <div style={{ display: "flex", gap: 4, marginBottom: 8 }}>
            {["uz", "ru", "en"].map((l) => (
              <button key={l} onClick={() => changeLanguage(l)} style={{ flex: 1, padding: "4px 0", fontSize: 11, fontWeight: 600, borderRadius: 6, border: lang === l ? "1px solid var(--accent)" : "1px solid transparent", cursor: "pointer", background: lang === l ? "linear-gradient(135deg, #5b8c3e 0%, #3d6b25 100%)" : "rgba(255,255,255,0.03)", color: lang === l ? "#fff" : "var(--text-secondary)" }}>{l.toUpperCase()}</button>
            ))}
          </div>
          <div style={{ display: "flex", gap: 6 }}>
            <a href="/swagger" target="_blank" rel="noopener noreferrer" className="btn btn-ghost btn-sm" style={{ flex: 1, justifyContent: "center", textDecoration: "none" }}>📘 API</a>
            <button onClick={logout} className="btn btn-danger btn-sm" style={{ flex: 1, justifyContent: "center", gap: 4 }}>🚪 {sidebarOpen && t("logout")}</button>
          </div>
        </div>
      </aside>

      <button onClick={() => setSidebarOpen(!sidebarOpen)} style={{ position: "fixed", top: 16, zIndex: 40, left: sidebarOpen ? 248 : 48, width: 28, height: 28, borderRadius: "50%", background: "rgba(18, 28, 20, 0.9)", border: "1px solid var(--border)", color: "var(--accent)", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 12, transition: "left 0.2s ease", backdropFilter: "blur(8px)" }}>{sidebarOpen ? "◀" : "▶"}</button>

      <main style={{ marginLeft: sidebarOpen ? 260 : 60, transition: "margin-left 0.2s ease", padding: "28px 32px", minHeight: "100vh", flex: 1 }}>{children}</main>
    </div>
  );
}
