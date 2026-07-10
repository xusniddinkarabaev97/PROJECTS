import { useState, useEffect } from "react";
import { useAuth } from "./auth/AuthContext";
import Layout from "./components/Layout";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import Stations from "./pages/Stations";
import Companies from "./pages/Companies";
import Clients from "./pages/Clients";
import Transactions from "./pages/Transactions";
import Plans from "./pages/Plans";
import MapView from "./pages/Map";
import Reports from "./pages/Reports";
import Vehicles from "./pages/Vehicles";
import SharePercents from "./pages/SharePercents";
import Profile from "./pages/Profile";
import DahuaSettings from "./pages/DahuaSettings";
import DahuaDevices from "./pages/DahuaDevices";
import VehicleManagement from "./pages/VehicleManagement";
import ParkingSessionsMonitor from "./pages/ParkingSessionsMonitor";

const routes = {
  "/": Dashboard,
  "/stations": Stations,
  "/companies": Companies,
  "/clients": Clients,
  "/transactions": Transactions,
  "/plans": Plans,
  "/map": MapView,
  "/reports": Reports,
  "/vehicles": Vehicles,
  "/sharepercents": SharePercents,
  "/profile": Profile,
  "/dahua-settings": DahuaSettings,
  "/dahua-devices": DahuaDevices,
  "/vehicle-management": VehicleManagement,
  "/parking-sessions": ParkingSessionsMonitor,
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
