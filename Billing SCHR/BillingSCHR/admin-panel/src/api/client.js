const BASE = window.location.pathname.startsWith('/billing-schr') ? '/billing-schr' : '';
const API_BASE = BASE + "/api";

async function request(url, options = {}) {
  const token = localStorage.getItem("billing-schr-token");
  const headers = { "Content-Type": "application/json", ...options.headers };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const response = await fetch(`${API_BASE}${url}`, { headers, ...options });
  if (response.status === 401) {
    localStorage.removeItem("billing-schr-token");
    localStorage.removeItem("billing-schr-user");
    window.location.hash = "#/login";
    throw new Error("Unauthorized");
  }
  if (!response.ok) {
    const error = await response
      .json()
      .catch(() => ({ message: "Network error" }));
    throw new Error(error.message || error.title || `HTTP ${response.status}`);
  }
  return response.json();
}

export const api = {
  // Dashboard stats (new API)
  getDashboardStats: () => request("/dashboard/stats"),

  // Dashboard (legacy - fallback)
  getDashboard: async () => {
    try {
      const stats = await request("/dashboard/stats");
      return stats;
    } catch {
      try {
        const [departments, companies, patients, transactions] = await Promise.all([
          request("/Departments").catch(() => []),
          request("/Companies").catch(() => []),
          request("/Patients").catch(() => []),
          request("/Transactions").catch(() => []),
        ]);
        const today = new Date().toISOString().slice(0, 10);
        const todayTx = Array.isArray(transactions)
          ? transactions.filter((t) => t.filledAt?.startsWith?.(today))
          : [];
        return {
          totalDepartments: Array.isArray(departments) ? departments.length : 0,
          totalCompanies: Array.isArray(companies) ? companies.length : 0,
          totalPatients: Array.isArray(patients) ? patients.length : 0,
          totalTransactions: Array.isArray(transactions) ? transactions.length : 0,
          todayTransactions: todayTx.length,
          todayRevenue: todayTx.reduce((s, t) => s + (t.totalSum || 0), 0),
        };
      } catch {
        return { totalDepartments: 0, totalCompanies: 0, totalPatients: 0, totalTransactions: 0, todayTransactions: 0, todayRevenue: 0 };
      }
    }
  },

  // Auth
  login: (login, password) =>
    fetch(BASE + "/api/Companies/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ login, password }),
    }).then(async (r) => {
      if (r.ok) return r.json();
      const text = await r.text();
      try { const json = JSON.parse(text); throw new Error(json.message || "Login failed"); }
      catch (e) { if (e.message && e.message !== "Login failed") throw e; throw new Error(text || "Login failed"); }
    }),

  // Departments
  getDepartments: () => request("/Departments"),
  getDepartment: (id) => request(`/Departments/${id}`),
  createDepartment: (data) => request("/Departments", { method: "POST", body: JSON.stringify(data) }),
  updateDepartment: (id, data) => request(`/Departments/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteDepartment: (id) => request(`/Departments/${id}`, { method: "DELETE" }),

  // Companies
  getCompanies: () => request("/Companies"),
  getCompany: (id) => request(`/Companies/${id}`),
  createCompany: (data) => request("/Companies", { method: "POST", body: JSON.stringify(data) }),
  updateCompany: (id, data) => request(`/Companies/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteCompany: (id) => request(`/Companies/${id}`, { method: "DELETE" }),

  // Patients
  getPatients: () => request("/Patients"),

  // Transactions
  getTransactions: () => request("/Transactions"),

  // Users
  getUsers: () => request("/users"),
  getUser: (id) => request(`/users/${id}`),
  createUser: (data) => request("/users", { method: "POST", body: JSON.stringify(data) }),
  updateUser: (id, data) => request(`/users/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deleteUser: (id) => request(`/users/${id}`, { method: "DELETE" }),

  // Reports
  getReportSummary: (year, month) => request(`/reports/summary?year=${year}${month ? '&month=' + month : ''}`),
  getReportByDepartment: (year, month) => request(`/reports/by-department?year=${year}${month ? '&month=' + month : ''}`),
  getReportByCashier: (year, month) => request(`/reports/by-cashier?year=${year}${month ? '&month=' + month : ''}`),

  // Audit
  getAuditLogs: () => request("/audit"),

  // Excel export
  exportTransactions: async (year, month) => {
    const token = localStorage.getItem("billing-schr-token");
    const params = new URLSearchParams({ year: String(year) });
    if (month) params.append("month", String(month));
    const r = await fetch(BASE + `/api/Transactions/export?${params}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!r.ok) throw new Error("Export failed");
    return r.blob();
  },

  // Plans (tariffs)
  getPlans: () => request("/Plans"),
  createPlan: (data) => request("/Plans", { method: "POST", body: JSON.stringify(data) }),
  updatePlan: (id, data) => request(`/Plans/${id}`, { method: "PUT", body: JSON.stringify(data) }),
  deletePlan: (id) => request(`/Plans/${id}`, { method: "DELETE" }),
};
