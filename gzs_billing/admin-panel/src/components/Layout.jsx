import { useState, useEffect } from "react";
import { NavLink } from "./NavLink";
import { useTranslation } from "../i18n/LanguageContext";
import { useAuth } from "../auth/AuthContext";

const icons = {
  dashboard: "layout-dashboard",
  stations: "fuel",
  stakeholders: "users",
  payments: "credit-card",
  transactions: "arrow-left-right",
  sverka: "refresh-cw",
  reports: "bar-chart-3",
  settings: "settings",
  users: "user-cog",
};

export default function Layout({ children }) {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const { t, lang, changeLanguage } = useTranslation();
  const { user, logout } = useAuth();
  const role = user?.role || "manager";

  useEffect(() => { window.lucide?.createIcons(); }, [sidebarOpen]);

  const menuItems = [
    { path: "/", label: t("dashboard"), icon: icons.dashboard, roles: ["manager", "superadmin"] },
    { path: "/stations", label: t("stations"), icon: icons.stations, roles: ["superadmin"] },
    { path: "/stakeholders", label: t("stakeholders"), icon: icons.stakeholders, roles: ["superadmin"] },
    { path: "/payments", label: t("payments"), icon: icons.payments, roles: ["superadmin"] },

    { path: "/sverka", label: t("sverka"), icon: icons.sverka, roles: ["manager", "superadmin"] },
    { path: "/reports", label: t("reports"), icon: icons.reports, roles: ["manager", "superadmin"] },
    { path: "/settings", label: t("settings"), icon: icons.settings, roles: ["superadmin"] },
    { path: "/users", label: t("users"), icon: icons.users, roles: ["superadmin"] },
  ].filter((item) => item.roles.includes(role));

  return (
    <div className="flex h-screen bg-[#11161d] text-slate-200">
      {/* Sidebar */}
      <aside className={`${sidebarOpen ? "w-64" : "w-16"} bg-[#161c24] border-r border-slate-800 flex flex-col justify-between p-3 transition-all duration-200 hidden md:flex`}>
        <div>
          <div className="flex items-center gap-3 mb-6 px-2">
            <div className="w-10 h-10 min-w-[40px] rounded-xl bg-gradient-to-tr from-teal-500 to-emerald-400 flex items-center justify-center text-slate-900 font-bold text-lg">
              GZS
            </div>
            {sidebarOpen && (
              <div className="overflow-hidden">
                <h2 className="text-sm font-semibold text-white whitespace-nowrap">{t("dashboard")}</h2>
                <p className="text-xs text-slate-400 whitespace-nowrap">GZS Billing</p>
              </div>
            )}
          </div>
          <nav className="space-y-1">
            {menuItems.map((item) => (
              <NavLink key={item.path} to={item.path} collapsed={!sidebarOpen}>
                <i data-lucide={item.icon} className="w-5 h-5 min-w-[20px]"></i>
                {sidebarOpen && <span className="text-sm font-medium whitespace-nowrap">{item.label}</span>}
              </NavLink>
            ))}
          </nav>
        </div>
        <div className="space-y-1 border-t border-slate-800 pt-3">
          <a href="/swagger" target="_blank" rel="noopener" className="flex items-center gap-3 px-3 py-2.5 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800/50 transition text-sm">
            <i data-lucide="book-open" className="w-5 h-5"></i>
            {sidebarOpen && <span>API Docs</span>}
          </a>
          <button onClick={logout} className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-rose-400/80 hover:text-rose-400 hover:bg-rose-500/5 transition text-sm cursor-pointer">
            <i data-lucide="log-out" className="w-5 h-5"></i>
            {sidebarOpen && <span>{t("logout")}</span>}
          </button>
        </div>
      </aside>

      {/* Main */}
      <main className="flex-1 flex flex-col overflow-auto bg-gradient-to-b from-[#181f29] to-[#11161d]">
        {/* Top bar */}
        <header className="flex items-center justify-between px-6 py-3 border-b border-slate-800/60 bg-[#161c24]/50">
          <button onClick={() => setSidebarOpen(!sidebarOpen)} className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800/50 transition cursor-pointer">
            <i data-lucide={sidebarOpen ? "panel-left-close" : "panel-left-open"} className="w-5 h-5"></i>
          </button>
          <div className="flex items-center gap-3">
            <span className="text-xs text-slate-500 hidden sm:inline">{user?.fullName || user?.username}</span>
            <div className="flex gap-1">
              {["uz", "ru", "en"].map((l) => (
                <button key={l} onClick={() => changeLanguage(l)}
                  className={`px-2 py-1 text-xs rounded-md font-medium transition cursor-pointer ${lang === l ? "bg-teal-500/20 text-teal-400 border border-teal-500/30" : "text-slate-500 hover:text-slate-300"}`}>
                  {l.toUpperCase()}
                </button>
              ))}
            </div>
          </div>
        </header>
        <div className="p-6 lg:p-8 flex-1">{children}</div>
      </main>
    </div>
  );
}
