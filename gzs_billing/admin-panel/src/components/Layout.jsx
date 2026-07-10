import { useState } from "react";
import { NavLink } from "./NavLink";
import { useTranslation } from "../i18n/LanguageContext";
import { useAuth } from "../auth/AuthContext";

export default function Layout({ children }) {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const { t, lang, changeLanguage } = useTranslation();
  const { user, logout } = useAuth();
  const role = user?.role || "manager";

  const allMenuItems = [
    {
      path: "/",
      label: t("dashboard"),
      icon: "📊",
      roles: ["manager", "superadmin"],
    },
    {
      path: "/stations",
      label: t("stations"),
      icon: "⛽",
      roles: ["superadmin"],
    },
    {
      path: "/stakeholders",
      label: t("stakeholders"),
      icon: "👥",
      roles: ["superadmin"],
    },
    {
      path: "/payments",
      label: t("payments"),
      icon: "💳",
      roles: ["superadmin"],
    },
    {
      path: "/transactions",
      label: t("transactions"),
      icon: "📋",
      roles: ["manager", "superadmin"],
    },
    {
      path: "/sverka",
      label: t("sverka"),
      icon: "🔄",
      roles: ["manager", "superadmin"],
    },
    {
      path: "/reports",
      label: t("reports"),
      icon: "📈",
      roles: ["manager", "superadmin"],
    },
    {
      path: "/settings",
      label: t("settings"),
      icon: "⚙️",
      roles: ["superadmin"],
    },
  ];

  const menuItems = allMenuItems.filter((item) => item.roles.includes(role));

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
        {/* Logo */}
        <div
          style={{
            padding: "20px 16px",
            borderBottom: "1px solid var(--border)",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <span style={{ fontSize: 24 }}>⛽</span>
            {sidebarOpen && (
              <div>
                <h1
                  style={{
                    fontSize: 16,
                    fontWeight: 700,
                    color: "var(--text-primary)",
                    whiteSpace: "nowrap",
                  }}
                >
                  BillingGAZ
                </h1>
                <p style={{ fontSize: 11, color: "var(--text-muted)" }}>
                  {user?.username}{" "}
                  <span className="badge badge-info" style={{ marginLeft: 4 }}>
                    {role}
                  </span>
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Nav */}
        <nav style={{ flex: 1, padding: 8, overflowY: "auto" }}>
          {menuItems.map((item) => (
            <NavLink key={item.path} to={item.path} collapsed={!sidebarOpen}>
              <span style={{ fontSize: 18, width: 24, textAlign: "center" }}>
                {item.icon}
              </span>
              {sidebarOpen && <span>{item.label}</span>}
            </NavLink>
          ))}
        </nav>

        {/* Bottom */}
        <div style={{ padding: 12, borderTop: "1px solid var(--border)" }}>
          {/* Language */}
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
          {/* API + Logout */}
          <div style={{ display: "flex", gap: 6 }}>
            <a
              href="http://localhost:5036/swagger"
              target="_blank"
              rel="noopener noreferrer"
              className="btn btn-ghost btn-sm"
              style={{
                flex: 1,
                justifyContent: "center",
                textDecoration: "none",
              }}
            >
              📘
            </a>
            <button
              onClick={logout}
              className="btn btn-danger btn-sm"
              style={{ flex: 1, justifyContent: "center" }}
            >
              🚪
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
