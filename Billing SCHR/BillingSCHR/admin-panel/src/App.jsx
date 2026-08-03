import { useState, useEffect } from "react";
import { useAuth } from "./auth/AuthContext";
import Layout from "./components/Layout";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import Transactions from "./pages/Transactions";
import Fiscal from "./pages/Fiscal";
import Reconciliation from "./pages/Reconciliation";
import Reports from "./pages/Reports";
import Users from "./pages/Users";
import Integrations from "./pages/Integrations";
import AuditLogs from "./pages/AuditLogs";

const routes = {
  "/": Dashboard,
  "/transactions": Transactions,
  "/fiscal": Fiscal,
  "/reconciliation": Reconciliation,
  "/reports": Reports,
  "/users": Users,
  "/integrations": Integrations,
  "/audit": AuditLogs,
};

function getRoute(hash) {
  const path = (hash || "").replace(/^#/, "") || "/";
  return { page: routes[path] || routes["/"], key: path };
}

export default function App() {
  const { isAuthenticated } = useAuth();
  const [current, setCurrent] = useState(() => getRoute(window.location.hash));

  useEffect(() => {
    const onHashChange = () => setCurrent(getRoute(window.location.hash));
    window.addEventListener("hashchange", onHashChange);
    return () => window.removeEventListener("hashchange", onHashChange);
  }, []);

  if (!isAuthenticated) {
    return <Login />;
  }

  const Page = current.page;

  return (
    <Layout>
      <Page />
    </Layout>
  );
}
