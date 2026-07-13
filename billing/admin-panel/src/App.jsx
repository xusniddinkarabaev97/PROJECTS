import { useState, useEffect } from "react";
import { useAuth } from "./auth/AuthContext";
import Login from "./pages/Login";
import Layout from "./components/Layout";
import Dashboard from "./pages/Dashboard";
import Stations from "./pages/Stations";
import Stakeholders from "./pages/Stakeholders";
import Settings from "./pages/Settings";
import Payments from "./pages/Payments";
import PaymentDetail from "./pages/PaymentDetail";
import Transactions from "./pages/Transactions";
import Sverka from "./pages/Sverka";
import Reports from "./pages/Reports";
import StationDetail from "./pages/StationDetail";

const routes = {
  "/": Dashboard,
  "/login": null,
  "/stations": Stations,
  "/stakeholders": Stakeholders,
  "/settings": Settings,
  "/payments": Payments,
  "/transactions": Transactions,
  "/sverka": Sverka,
  "/reports": Reports,
};

export default function App() {
  const { isAuthenticated } = useAuth();
  const [route, setRoute] = useState(window.location.hash.slice(1) || "/");

  useEffect(() => {
    const onHashChange = () => setRoute(window.location.hash.slice(1) || "/");
    window.addEventListener("hashchange", onHashChange);
    return () => window.removeEventListener("hashchange", onHashChange);
  }, []);

  // Login page is always accessible
  if (route === "/login") {
    return <Login />;
  }

  // Auth guard: redirect to login if not authenticated
  if (!isAuthenticated) {
    window.location.hash = "#/login";
    return null;
  }

  // Match /payments/:id
  const paymentDetailMatch = route.match(/^\/payments\/(.+)$/);
  if (paymentDetailMatch) {
    return (
      <Layout>
        <PaymentDetail id={paymentDetailMatch[1]} />
      </Layout>
    );
  }

  const stationDetailMatch = route.match(/^\/stations\/(\d+)$/);
  if (stationDetailMatch) {
    return (
      <Layout>
        <StationDetail id={stationDetailMatch[1]} />
      </Layout>
    );
  }

  const Page = routes[route] || Dashboard;

  return (
    <Layout>
      <Page />
    </Layout>
  );
}
