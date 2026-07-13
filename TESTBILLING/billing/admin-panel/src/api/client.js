const BASE_URL = "/api";
const TOKEN_KEY = "gzs_billing_token";
const USER_KEY = "gzs_billing_user";

function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

function setToken(token) {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
}

function clearToken() {
  localStorage.removeItem(TOKEN_KEY);
}

function getCurrentUser() {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

function setCurrentUser(user) {
  if (user) {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  } else {
    localStorage.removeItem(USER_KEY);
  }
}

function clearCurrentUser() {
  localStorage.removeItem(USER_KEY);
}

function logout() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
  window.location.href = "/login";
}

async function login(username, password) {
  const response = await request("/v1/auth/login", {
    method: "POST",
    body: { username, password },
  });

  if (response.token) {
    setToken(response.token);
  }

  const user = response.user || response;
  if (user) {
    setCurrentUser(user);
  }

  return response;
}

async function request(endpoint, options = {}) {
  const { method = "GET", body, params, headers: extraHeaders = {} } = options;

  let url = `${BASE_URL}${endpoint}`;

  if (params) {
    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== "") {
        searchParams.append(key, String(value));
      }
    });
    const qs = searchParams.toString();
    if (qs) {
      url += `?${qs}`;
    }
  }

  const headers = {
    "Content-Type": "application/json",
    ...extraHeaders,
  };

  const token = getToken();
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const config = {
    method,
    headers,
  };

  if (body && method !== "GET") {
    config.body = JSON.stringify(body);
  }

  let response;
  try {
    response = await fetch(url, config);
  } catch (networkError) {
    throw new Error(`Network error: ${networkError.message}`);
  }

  if (response.status === 401) {
    clearToken();
    clearCurrentUser();
    window.location.href = "/login";
    throw new Error("Unauthorized. Please log in again.");
  }

  if (response.status === 204) {
    return null;
  }

  let data;
  try {
    data = await response.json();
  } catch {
    throw new Error(`Invalid JSON response (status ${response.status})`);
  }

  if (!response.ok) {
    const message =
      data?.message ||
      data?.error ||
      `Request failed with status ${response.status}`;
    const err = new Error(message);
    err.status = response.status;
    err.data = data;
    throw err;
  }

  return data;
}

const api = {
  login,
  logout,
  getCurrentUser,

  getDashboard() {
    return request("/v1/payments/stats/dashboard");
  },

  createPayment({ amount, currency, paymentSystem, carNumber, metadata }) {
    return request("/v1/test/test-imitation", {
      method: "POST",
      body: {
        amount,
        currency: currency || "UZS",
        paymentSystem,
        carNumber,
        metadata: metadata || {},
      },
    });
  },

  getTransactions(params) {
    return request("/v1/payments", { params });
  },

  // Stations
  getStations() {
    return request("/v1/stations");
  },

  createStation(data) {
    return request("/v1/stations", {
      method: "POST",
      body: data,
    });
  },

  updateStation(id, data) {
    return request("/v1/stations/" + id, {
      method: "PUT",
      body: data,
    });
  },

  deleteStation(id) {
    return request("/v1/stations/" + id, {
      method: "DELETE",
    });
  },

  createColumn(sid, data) {
    return request("/v1/stations/" + sid + "/columns", {
      method: "POST",
      body: data,
    });
  },

  updateColumn(sid, cid, data) {
    return request("/v1/stations/" + sid + "/columns/" + cid, {
      method: "PUT",
      body: data,
    });
  },

  deleteColumn(sid, cid) {
    return request("/v1/stations/" + sid + "/columns/" + cid, {
      method: "DELETE",
    });
  },

  // Users
  getUsers() {
    return request("/v1/users");
  },

  createUser(data) {
    return request("/v1/users", {
      method: "POST",
      body: data,
    });
  },

  updateUser(id, data) {
    return request("/v1/users/" + id, {
      method: "PUT",
      body: data,
    });
  },

  deactivateUser(id) {
    return request("/v1/users/" + id + "/deactivate", {
      method: "PUT",
    });
  },

  // Shareholders
  getShareholders() {
    return request("/v1/shareholders");
  },

  createShareholder(data) {
    return request("/v1/shareholders", {
      method: "POST",
      body: data,
    });
  },

  updateShareholder(id, data) {
    return request("/v1/shareholders/" + id, {
      method: "PUT",
      body: data,
    });
  },

  deleteShareholder(id) {
    return request("/v1/shareholders/" + id, {
      method: "DELETE",
    });
  },
};

export { getToken, setToken, clearToken, logout, getCurrentUser, request };
export default api;
