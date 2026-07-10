const API_BASE = "/api/admin";

async function request(url, options = {}) {
  const token = localStorage.getItem("gzs-token");
  const headers = { "Content-Type": "application/json", ...options.headers };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const response = await fetch(`${API_BASE}${url}`, { headers, ...options });
  if (response.status === 401) {
    localStorage.removeItem("gzs-token");
    localStorage.removeItem("gzs-user");
    window.location.hash = "#/login";
    throw new Error("Unauthorized");
  }
  if (!response.ok) {
    const error = await response
      .json()
      .catch(() => ({ message: "Network error" }));
    throw new Error(error.message || `HTTP ${response.status}`);
  }
  return response.json();
}

export const api = {
  // Dashboard
  getDashboard: () => request("/dashboard"),

  // Stations
  getStations: () => request("/stations"),
  getStation: (id) => request(`/stations/${id}`),
  createStation: (data) =>
    request("/stations", { method: "POST", body: JSON.stringify(data) }),
  updateStation: (id, data) =>
    request(`/stations/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteStation: (id) => request(`/stations/${id}`, { method: "DELETE" }),

  // Stakeholders
  getStakeholders: () => request("/stakeholders"),
  getStakeholdersByStation: (stationId) =>
    request(`/stakeholders/by-station/${stationId}`),
  createStakeholder: (data) =>
    request("/stakeholders", { method: "POST", body: JSON.stringify(data) }),
  updateStakeholder: (id, data) =>
    request(`/stakeholders/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  deleteStakeholder: (id) =>
    request(`/stakeholders/${id}`, { method: "DELETE" }),

  // Payments
  getPayments: () => request("/payments"),
  getPayment: (id) => request(`/payments/${id}`),
  createPayment: (data) =>
    request("/payments", { method: "POST", body: JSON.stringify(data) }),
  updatePayment: (id, data) =>
    request(`/payments/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deletePayment: (id) => request(`/payments/${id}`, { method: "DELETE" }),

  // Settings
  getSettings: () => request("/settings"),
  getSetting: (key) => request(`/settings/${encodeURIComponent(key)}`),
  updateSetting: (key, data) =>
    request(`/settings/${encodeURIComponent(key)}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  getSettingCategories: () => request("/settings/categories"),

  // Auth
  login: (username, password) =>
    fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
    }).then((r) =>
      r.ok
        ? r.json()
        : r.json().then((e) => {
            throw new Error(e.message);
          }),
    ),

  // Dispensers
  getDispensers: (stationId) => request(`/stations/${stationId}/dispensers`),
  createDispenser: (stationId, data) =>
    request(`/stations/${stationId}/dispensers`, {
      method: "POST",
      body: JSON.stringify(data),
    }),
  updateDispenser: (stationId, id, data) =>
    request(`/stations/${stationId}/dispensers/${id}`, {
      method: "PUT",
      body: JSON.stringify(data),
    }),
  deleteDispenser: (stationId, id) =>
    request(`/stations/${stationId}/dispensers/${id}`, { method: "DELETE" }),

  // Users
  getUsers: () => request("/users"),
  createUser: (data) =>
    request("/users", { method: "POST", body: JSON.stringify(data) }),
  toggleUser: (id) => request(`/users/${id}/toggle`, { method: "PUT" }),
};
