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
    { path: "/profile", label: t("profile"), icon: "👤" },
  ];

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        background: "var(--bg-primary)",
      }}
    >
      {/* Sidebar */}
      <aside
        className="sidebar"
        style={{
          position: "fixed",
          top: 0,
          left: 0,
          height: "100%",
          width: sidebarOpen ? 260 : 60,
          transition: "width 0.2s ease",
          display: "flex",
          flexDirection: "column",
          zIndex: 30,
        }}
      >
        <div
          style={{
            padding: "24px 20px",
            borderBottom: "1px solid rgba(255,255,255,0.08)",
            background: "linear-gradient(135deg, #1e40af, #0f172a)",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <span style={{ fontSize: 28 }}>🅿️</span>
            {sidebarOpen && (
              <div>
                <h1
                  style={{
                    fontSize: 16,
                    fontWeight: 800,
                    color: "#fff",
                    whiteSpace: "nowrap",
                    letterSpacing: "-0.5px",
                  }}
                >
                  ODU Parking
                </h1>
                <p style={{ fontSize: 11, color: "rgba(255,255,255,0.5)", fontWeight: 500 }}>
                  Billing System
                </p>
              </div>
            )}
          </div>
        </div>

        <nav style={{ flex: 1, padding: 8, overflowY: "auto" }}>
          {menuItems.map((item, idx) => {
            if (item.type === "divider") {
              return (
                <div
                  key={`div-${idx}`}
                  style={{
                    height: 1,
                    background: "var(--border)",
                    margin: "8px 12px",
                  }}
                />
              );
            }
            if (item.type === "section") {
              return sidebarOpen ? (
                <div
                  key={`sec-${idx}`}
                  style={{
                    padding: "12px 16px 4px",
                    fontSize: 10,
                    fontWeight: 600,
                    textTransform: "uppercase",
                    letterSpacing: 1,
                    color: "var(--text-muted)",
                  }}
                >
                  {item.label}
                </div>
              ) : null;
            }
            return (
              <NavLink key={item.path} to={item.path} collapsed={!sidebarOpen}>
                <span style={{ fontSize: 18, width: 24, textAlign: "center" }}>
                  {item.icon}
                </span>
                {sidebarOpen && <span>{item.label}</span>}
              </NavLink>
            );
          })}
        </nav>

        <div style={{ padding: 16, borderTop: "1px solid rgba(255,255,255,0.08)" }}>
          {/* User info */}
          {sidebarOpen && (
            <div
              style={{
                marginBottom: 12,
                padding: "10px 12px",
                background: "rgba(255,255,255,0.05)",
                borderRadius: 10,
              }}
            >
              <div
                style={{
                  fontSize: 13,
                  fontWeight: 600,
                  color: "rgba(255,255,255,0.8)",
                }}
              >
                👤 {user?.email || "Admin"}
              </div>
            </div>
          )}
          <div style={{ display: "flex", gap: 4, marginBottom: 8 }}>
            {["uz", "ru", "en"].map((l) => (
              <button
                key={l}
                onClick={() => changeLanguage(l)}
                style={{
                  flex: 1,
                  padding: "4px 0",
                  fontSize: 11,
                  fontWeight: 500,
                  borderRadius: 6,
                  border: "none",
                  cursor: "pointer",
                  background: lang === l ? "var(--accent)" : "var(--bg-hover)",
                  color: lang === l ? "#fff" : "var(--text-secondary)",
                }}
              >
                {l.toUpperCase()}
              </button>
            ))}
          </div>
          <div style={{ display: "flex", gap: 6 }}>
            <a
              href="/swagger"
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-ghost btn-sm"
              style={{
                flex: 1,
                justifyContent: "center",
                textDecoration: "none",
              }}
            >
              📘 API
            </a>
            <button
              onClick={logout}
              className="btn btn-danger btn-sm"
              style={{ flex: 1, justifyContent: "center", gap: 4 }}
            >
              🚪 {sidebarOpen && t("logout")}
            </button>
          </div>
        </div>
      </aside>

      {/* Toggle */}
      <button
        onClick={() => setSidebarOpen(!sidebarOpen)}
        style={{
          position: "fixed",
          top: 16,
          zIndex: 40,
          left: sidebarOpen ? 248 : 48,
          width: 28,
          height: 28,
          borderRadius: "50%",
          background: "var(--bg-card)",
          border: "1px solid var(--border)",
          color: "var(--text-secondary)",
          cursor: "pointer",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: 12,
          transition: "left 0.2s ease",
        }}
      >
        {sidebarOpen ? "◀" : "▶"}
      </button>

      {/* Main */}
      <main
        style={{
          marginLeft: sidebarOpen ? 260 : 60,
          transition: "margin-left 0.2s ease",
          padding: "28px 32px",
          minHeight: "100vh",
          flex: 1,
        }}
      >
        {children}
      </main>
    </div>
  );
}
