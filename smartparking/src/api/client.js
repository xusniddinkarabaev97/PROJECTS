const API_BASE = "/smartparking/api";

async function request(url, options = {}) {
  const token = localStorage.getItem("bgz-token");
  const headers = { "Content-Type": "application/json", ...options.headers };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const response = await fetch(`${API_BASE}${url}`, { headers, ...options });
  if (response.status === 401) {
    localStorage.removeItem("bgz-token");
    localStorage.removeItem("bgz-user");
    window.location.hash = "#/login";
    throw new Error("Unauthorized");
  }
  if (!response.ok) {
    const text = await response.text();
    let message = `HTTP ${response.status}`;
    if (text) {
      try {
        const err = JSON.parse(text);
        message = err.message || err.title || message;
      } catch {}
    }
    throw new Error(message);
  }
  const text = await response.text();
  if (!text) return null;
  return JSON.parse(text);
}

export const api = {
  // Dashboard stats — build from multiple endpoints
  getDashboard: async () => {
    try {
      const [stations, companies, clients, transactions] = await Promise.all([
        request("/Stations").catch(() => []),
        request("/Companies").catch(() => []),
        request("/Clients").catch(() => []),
        request("/Transactions").catch(() => []),
      ]);
      const today = new Date().toISOString().slice(0, 10);
      const todayTx = Array.isArray(transactions)
        ? transactions.filter((t) => t.filledAt?.startsWith?.(today))
        : [];
      return {
        totalStations: Array.isArray(stations) ? stations.length : 0,
        totalCompanies: Array.isArray(companies) ? companies.length : 0,
        totalClients: Array.isArray(clients) ? clients.length : 0,
        totalTransactions: Array.isArray(transactions)
          ? transactions.length
          : 0,
        todayTransactions: todayTx.length,
        todayRevenue: todayTx.reduce((s, t) => s + (t.totalSum || 0), 0),
      };
    } catch {
      return {
        totalStations: 0,
        totalCompanies: 0,
        totalClients: 0,
        totalTransactions: 0,
        todayTransactions: 0,
        todayRevenue: 0,
      };
    }
  },

  // Auth
  login: (email, password) =>
    fetch("/smartparking/api/Companies/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    }).then(async (r) => {
      if (r.ok) return r.json();
      const text = await r.text();
      try {
        const json = JSON.parse(text);
        throw new Error(json.message || json.title || "Login failed");
      } catch (e) {
        if (e.message && e.message !== "Login failed") throw e;
        throw new Error(text || "Login failed");
      }
    }),

  // Stations
  getStations: () => request("/Stations"),
  getStation: (id) => request(`/Stations/${id}`),
  createStation: (data) =>
    request("/Stations", { method: "POST", body: JSON.stringify(data) }),
  updateStation: (id, data) =>
    request(`/Stations/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteStation: (id) => request(`/Stations/${id}`, { method: "DELETE" }),

  // Companies
  getCompanies: () => request("/Companies"),
  getCompany: (id) => request(`/Companies/${id}`),
  createCompany: (data) =>
    request("/Companies", { method: "POST", body: JSON.stringify(data) }),
  updateCompany: (id, data) =>
    request(`/Companies/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteCompany: (id) => request(`/Companies/${id}`, { method: "DELETE" }),

  // Clients
  getClients: () => request("/Clients"),

  // Transactions
  getTransactions: () => request("/Transactions"),

  // Plans (tariffs)
  getPlans: () => request("/Plan"),
  createPlan: (data) =>
    request("/Plan", { method: "POST", body: JSON.stringify(data) }),
  updatePlan: (id, data) =>
    request(`/Plan/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deletePlan: (id) => request(`/Plan/${id}`, { method: "DELETE" }),

  // Share Percents
  getSharePercents: () => request("/SharePercent"),
  getSharePercent: (id) => request(`/SharePercent/${id}`),
  createSharePercent: (data) =>
    request("/SharePercent", { method: "POST", body: JSON.stringify(data) }),
  updateSharePercent: (id, data) =>
    request(`/SharePercent/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  deleteSharePercent: (id) =>
    request(`/SharePercent/${id}`, { method: "DELETE" }),

  // Dahua Integration
  getDahuaSettings: () => request("/DahuaIntegration/settings"),
  saveDahuaSettings: (data) =>
    request("/DahuaIntegration/settings", {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  testDahuaConnection: (data) =>
    request("/DahuaIntegration/test-connection", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  // Dahua Devices
  getDahuaDevices: () => request("/DahuaIntegration/devices"),
  createDahuaDevice: (data) =>
    request("/DahuaIntegration/devices", {
      method: "POST",
      body: JSON.stringify(data),
    }),
  updateDahuaDevice: (id, data) =>
    request(`/DahuaIntegration/devices/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  deleteDahuaDevice: (id) =>
    request(`/DahuaIntegration/devices/${id}`, { method: "DELETE" }),

  // Vehicle Lists
  getDahuaVehicles: (category) =>
    request(
      `/DahuaIntegration/vehicles${category ? `?category=${category}` : ""}`,
    ),
  createDahuaVehicle: (data) =>
    request("/DahuaIntegration/vehicles", {
      method: "POST",
      body: JSON.stringify(data),
    }),
  updateDahuaVehicle: (id, data) =>
    request(`/DahuaIntegration/vehicles/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  deleteDahuaVehicle: (id) =>
    request(`/DahuaIntegration/vehicles/${id}`, { method: "DELETE" }),

  // Parking Sessions
  getParkingSessions: (status) =>
    request(`/DahuaIntegration/sessions${status ? `?status=${status}` : ""}`),
  getParkingSession: (id) => request(`/DahuaIntegration/sessions/${id}`),

  // Barrier Control
  openBarrier: (data) =>
    request("/DahuaIntegration/barrier/open", {
      method: "POST",
      body: JSON.stringify(data),
    }),

  // Events Log
  getDahuaEvents: (page = 1, pageSize = 50) =>
    request(`/DahuaIntegration/events?page=${page}&pageSize=${pageSize}`),
};
