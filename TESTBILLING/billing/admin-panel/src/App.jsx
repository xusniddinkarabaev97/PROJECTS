import { useState, useEffect } from "react";
import {
  Routes,
  Route,
  NavLink,
  useLocation,
  useNavigate,
} from "react-router-dom";
import { useTranslation } from "./i18n/LanguageContext";
import api, { logout as apiLogout } from "./api/client";
import Dashboard from "./pages/Dashboard";
import Transactions from "./pages/Transactions";
import Sverka from "./pages/Sverka";
import Reports from "./pages/Reports";
import StationsPage from "./pages/StationsPage";
import UsersPage from "./pages/UsersPage";
import ShareholdersPage from "./pages/ShareholdersPage";
import TestImitation from "./pages/TestImitation";
import LoginPage from "./pages/LoginPage";
import PaymentPage from "./pages/PaymentPage";

const menuItems = [
  { path: "/", icon: "📊", key: "dashboard" },
  { path: "/transactions", icon: "💳", key: "transactions" },
  { path: "/sverka", icon: "🔄", key: "sverka" },
  { path: "/reports", icon: "📈", key: "reports" },
  { path: "/stations", icon: "⛽", key: "stations" },
  { path: "/users", icon: "👥", key: "users" },
  { path: "/shareholders", icon: "💰", key: "shareholders" },
  { path: "/test-imitation", icon: "🧪", key: "testImitation" },
];

const pageTitles = {
  "/": "dashboard",
  "/transactions": "transactions",
  "/sverka": "sverka",
  "/reports": "reports",
  "/stations": "stations",
  "/users": "users",
  "/shareholders": "shareholders",
  "/test-imitation": "testImitation",
};

export default function App() {
  const { t, lang, toggleLanguage } = useTranslation();
  const location = useLocation();
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const [isAuthenticated, setIsAuthenticated] = useState(() => {
    return !!localStorage.getItem("gzs_billing_token");
  });

  useEffect(() => {
    if (!isAuthenticated && location.pathname !== "/login") {
      navigate("/login", { replace: true });
    }
  }, [isAuthenticated, location.pathname, navigate]);

  const user = (() => {
    try {
      const raw = localStorage.getItem("gzs_billing_user");
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  })();

  const userName = user?.displayName || user?.name || user?.username || "User";

  const handleLogout = () => {
    setIsAuthenticated(false);
    apiLogout();
  };

  const handleLoginSuccess = () => {
    setIsAuthenticated(true);
  };

  // Public payment page — no auth, no layout, has own Routes for useParams
  if (location.pathname.startsWith("/pay/")) {
    return (
      <Routes>
        <Route path="/pay/:columnId" element={<PaymentPage />} />
      </Routes>
    );
  }

  // Login page renders full-screen, without sidebar/topbar
  if (location.pathname === "/login") {
    return <LoginPage onLoginSuccess={handleLoginSuccess} />;
  }

  // Not authenticated — render nothing while redirecting
  if (!isAuthenticated) {
    return null;
  }

  const currentTitle = t(
    `sidebar.${pageTitles[location.pathname] || "dashboard"}`,
  );

  const closeSidebar = () => setSidebarOpen(false);

  return (
    <div className="app-layout">
      {/* Mobile overlay */}
      <div
        className={`sidebar-overlay${sidebarOpen ? " open" : ""}`}
        onClick={closeSidebar}
      />

      {/* Sidebar */}
      <aside className={`sidebar${sidebarOpen ? " open" : ""}`}>
        <div className="sidebar-logo">GZS BILLING</div>
        <ul className="sidebar-nav">
          {menuItems.map((item) => (
            <li key={item.path}>
              <NavLink
                to={item.path}
                end={item.path === "/"}
                onClick={closeSidebar}
                className={({ isActive }) => (isActive ? "active" : "")}
              >
                <span className="nav-icon">{item.icon}</span>
                {t(`sidebar.${item.key}`)}
              </NavLink>
            </li>
          ))}
        </ul>
      </aside>

      {/* Main area */}
      <div className="main-area">
        {/* Topbar */}
        <header className="topbar">
          <div className="topbar-left">
            <button
              className="hamburger"
              onClick={() => setSidebarOpen((prev) => !prev)}
              aria-label="Toggle menu"
            >
              {"☰"}
            </button>
            <h1 className="page-title">{currentTitle}</h1>
          </div>
          <div className="topbar-right">
            <span style={topbarStyles.welcome}>
              {t("topbar.welcome")}, {userName}
            </span>
            <button className="lang-switch" onClick={toggleLanguage}>
              {lang === "uz" ? "RU" : "UZ"}
            </button>
            <button className="btn btn-outline btn-sm" onClick={handleLogout}>
              {t("topbar.logout")}
            </button>
          </div>
        </header>

        {/* Page content */}
        <main className="main-content">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/transactions" element={<Transactions />} />
            <Route path="/sverka" element={<Sverka />} />
            <Route path="/reports" element={<Reports />} />
            <Route path="/stations" element={<StationsPage />} />
            <Route path="/users" element={<UsersPage />} />
            <Route path="/shareholders" element={<ShareholdersPage />} />
            <Route path="/test-imitation" element={<TestImitation />} />
          </Routes>
        </main>
      </div>
    </div>
  );
}

const topbarStyles = {
  welcome: {
    color: "#8b949e",
    fontSize: "0.85rem",
    whiteSpace: "nowrap",
  },
};
