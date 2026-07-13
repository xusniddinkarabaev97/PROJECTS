import { useState, useEffect, useCallback } from "react";
import { useTranslation } from "../i18n/LanguageContext";

const BASE_URL = "/api/v1/users";
const TOKEN_KEY = "gzs_billing_token";
const USER_KEY = "gzs_billing_user";

const ROLES = [
  "SuperAdmin",
  "Admin",
  "Manager",
  "Operator",
  "Shareholder",
  "ReadOnly",
];

const EMPTY_FORM = {
  username: "",
  fullName: "",
  email: "",
  password: "",
  role: "Operator",
  changePassword: false,
};

async function api(url, options = {}) {
  const token = localStorage.getItem(TOKEN_KEY);
  const headers = {
    "Content-Type": "application/json",
    ...options.headers,
  };
  if (token) headers["Authorization"] = "Bearer " + token;
  const config = { ...options, headers };
  if (config.body != null && typeof config.body === "object") {
    config.body = JSON.stringify(config.body);
  }
  let res;
  try {
    res = await fetch(url, config);
  } catch (networkErr) {
    throw new Error("Network error: " + networkErr.message);
  }
  if (res.status === 401) {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    window.location.href = "/login";
    throw new Error("Unauthorized");
  }
  if (res.status === 204) return null;
  let data;
  try {
    data = await res.json();
  } catch {
    throw new Error("Invalid JSON response (status " + res.status + ")");
  }
  if (!res.ok) {
    const msg =
      data?.message ||
      data?.error ||
      "Request failed with status " + res.status;
    const err = new Error(msg);
    err.status = res.status;
    err.data = data;
    throw err;
  }
  return data;
}

function getCurrentUser() {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

function getRoleBadge(role) {
  switch (role) {
    case "SuperAdmin":
      return {
        className: "badge",
        style: { background: "rgba(163,113,247,0.15)", color: "#a371f7" },
      };
    case "Admin":
      return { className: "badge badge-info" };
    case "Manager":
      return { className: "badge badge-success" };
    case "Operator":
      return { className: "badge badge-warning" };
    case "Shareholder":
      return {
        className: "badge",
        style: { background: "rgba(210,153,29,0.15)", color: "#d2991d" },
      };
    default:
      return { className: "badge badge-muted" };
  }
}

function getStatusBadge(status) {
  if (status === "inactive" || status === "deactivated") {
    return { className: "badge badge-muted", label: "Inactive" };
  }
  return { className: "badge badge-success", label: "Active" };
}

const fmtDate = (d) => {
  if (!d) return "-";
  try {
    const dt = new Date(d);
    return dt.toLocaleString("ru-RU", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return d;
  }
};

export default function UsersPage() {
  const { t } = useTranslation();
  const currentUser = getCurrentUser();
  const currentRole = currentUser?.role || "";
  const canManage = currentRole === "SuperAdmin" || currentRole === "Admin";

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const [toast, setToast] = useState(null);

  const showToast = useCallback((type, message) => {
    setToast({ type, message });
    setTimeout(() => setToast(null), 3500);
  }, []);

  const fetchData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await api(BASE_URL);
      const arr = Array.isArray(result)
        ? result
        : (result?.data ?? result?.items ?? []);
      setItems(arr);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const openCreate = () => {
    setEditingId(null);
    setForm({ ...EMPTY_FORM, role: "Operator" });
    setFormError("");
    setModalOpen(true);
  };

  const openEdit = (item) => {
    setEditingId(item.id);
    setForm({
      username: item.username || "",
      fullName: item.fullName || "",
      email: item.email || "",
      password: "",
      role: item.role || "Operator",
      changePassword: false,
    });
    setFormError("");
    setModalOpen(true);
  };

  const closeModal = () => {
    if (saving) return;
    setModalOpen(false);
    setEditingId(null);
    setForm(EMPTY_FORM);
    setFormError("");
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError("");

    if (form.password && form.password.length < 4) {
      setFormError(
        t("users.validation.passwordLength") ||
          "Password must be at least 4 characters",
      );
      return;
    }

    const payload = {
      username: form.username.trim(),
      fullName: form.fullName.trim(),
      email: form.email.trim(),
      role: form.role,
    };

    if (form.password) {
      payload.password = form.password;
    }

    try {
      setSaving(true);
      if (editingId) {
        await api(BASE_URL + "/" + editingId, {
          method: "PUT",
          body: payload,
        });
        showToast(
          "success",
          t("common.success") + " \u2014 " + (t("users.updated") || "Updated"),
        );
      } else {
        await api(BASE_URL, { method: "POST", body: payload });
        showToast(
          "success",
          t("common.success") + " \u2014 " + (t("users.created") || "Created"),
        );
      }
      closeModal();
      fetchData();
    } catch (err) {
      setFormError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const toggleStatus = async (user) => {
    const isActive =
      user.status !== "inactive" && user.status !== "deactivated";
    const action = isActive ? "deactivate" : "activate";
    const confirmMsg = isActive
      ? t("users.confirmDeactivate") ||
        "Are you sure you want to deactivate this user?"
      : t("users.confirmActivate") ||
        "Are you sure you want to activate this user?";

    if (!window.confirm(confirmMsg + " " + user.username)) return;

    try {
      if (isActive) {
        await api(BASE_URL + "/" + user.id + "/deactivate", {
          method: "PUT",
        });
      } else {
        await api(BASE_URL + "/" + user.id, {
          method: "PUT",
          body: { status: "active" },
        });
      }
      showToast(
        "success",
        t("common.success") +
          " \u2014 " +
          (action === "deactivate"
            ? t("users.deactivated") || "Deactivated"
            : t("users.activated") || "Activated"),
      );
      fetchData();
    } catch (err) {
      showToast("error", err.message);
    }
  };

  const updateField = (field) => (e) => {
    const val =
      e.target.type === "checkbox" ? e.target.checked : e.target.value;
    setForm((prev) => ({ ...prev, [field]: val }));
  };

  if (!canManage) {
    return (
      <div style={st.emptyState}>
        <div style={st.emptyIcon}>{String.fromCodePoint(0x1f512)}</div>
        <div style={st.emptyText}>
          {t("users.accessDenied") ||
            "Access denied. Only SuperAdmin or Admin can manage users."}
        </div>
      </div>
    );
  }

  return (
    <div>
      {toast && (
        <div style={st.toastContainer}>
          <div
            style={{
              ...st.toast,
              background: toast.type === "error" ? "#da3633" : "#238636",
            }}
          >
            {toast.message}
          </div>
        </div>
      )}

      <div className="page-header">
        <h1 style={st.pageTitle}>
          {t("users.pageTitle") || "Users / Foydalanuvchilar"}
        </h1>
        <button className="btn btn-primary" onClick={openCreate}>
          + {t("users.addUser") || "Add User"}
        </button>
      </div>

      {loading && (
        <div className="loading-container">
          <div className="spinner spinner-lg" />
          <span>{t("common.loading")}</span>
        </div>
      )}

      {!loading && error && (
        <div style={st.emptyState}>
          <div style={st.emptyIcon}>{String.fromCodePoint(0x26a0)}</div>
          <div style={{ ...st.emptyText, color: "#f85149" }}>
            {t("common.error")}
          </div>
          <div style={{ color: "#6e7681", fontSize: "0.85rem", marginTop: 8 }}>
            {error}
          </div>
          <button
            className="btn btn-outline"
            style={{ marginTop: 16 }}
            onClick={fetchData}
          >
            {t("common.refresh")}
          </button>
        </div>
      )}

      {!loading && !error && items.length === 0 && (
        <div style={st.emptyState}>
          <div style={st.emptyIcon}>{String.fromCodePoint(0x1f4cb)}</div>
          <div style={st.emptyText}>{t("common.noData")}</div>
        </div>
      )}

      {!loading && !error && items.length > 0 && (
        <div className="table-container">
          <table>
            <thead>
              <tr>
                <th>{t("users.username") || "Username"}</th>
                <th>{t("users.fullName") || "Full Name"}</th>
                <th>{t("users.email") || "Email"}</th>
                <th>{t("users.role") || "Role"}</th>
                <th>{t("common.status")}</th>
                <th>{t("users.lastLogin") || "Last Login"}</th>
                <th>{t("common.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {items.map((user) => {
                const roleBadge = getRoleBadge(user.role);
                const statusBadge = getStatusBadge(user.status);
                return (
                  <tr key={user.id}>
                    <td className="font-mono text-sm">
                      {user.username || "-"}
                    </td>
                    <td>{user.fullName || "-"}</td>
                    <td>{user.email || "-"}</td>
                    <td>
                      <span
                        className={roleBadge.className}
                        style={roleBadge.style || {}}
                      >
                        {user.role || "-"}
                      </span>
                    </td>
                    <td>
                      <span className={statusBadge.className}>
                        {statusBadge.label}
                      </span>
                    </td>
                    <td className="text-muted text-sm">
                      {fmtDate(user.lastLogin ?? user.last_login)}
                    </td>
                    <td>
                      <div style={st.actionBtns}>
                        <button
                          className="btn btn-outline btn-sm"
                          onClick={() => openEdit(user)}
                        >
                          {t("common.edit")}
                        </button>
                        <button
                          className={
                            user.status === "inactive" ||
                            user.status === "deactivated"
                              ? "btn btn-success btn-sm"
                              : "btn btn-outline btn-sm"
                          }
                          style={
                            user.status === "inactive" ||
                            user.status === "deactivated"
                              ? {}
                              : { color: "#f0883e", borderColor: "#f0883e" }
                          }
                          onClick={() => toggleStatus(user)}
                        >
                          {user.status === "inactive" ||
                          user.status === "deactivated"
                            ? t("users.activate") || "Activate"
                            : t("users.deactivate") || "Deactivate"}
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {modalOpen && (
        <>
          <div style={st.overlay} onClick={closeModal} />
          <div style={st.modal}>
            <div style={st.modalHeader}>
              <h2 style={st.modalTitle}>
                {editingId
                  ? t("common.edit") + " " + (t("users.user") || "User")
                  : t("common.create") + " " + (t("users.user") || "User")}
              </h2>
              <button
                style={st.closeBtn}
                onClick={closeModal}
                disabled={saving}
              >
                &times;
              </button>
            </div>

            <form onSubmit={handleSubmit}>
              <div style={st.modalBody}>
                <div className="form-group">
                  <label>{t("users.username") || "Username"} *</label>
                  <input
                    type="text"
                    value={form.username}
                    onChange={updateField("username")}
                    required
                    placeholder={t("users.username") || "Username"}
                    disabled={!!editingId}
                  />
                </div>

                <div className="form-group">
                  <label>{t("users.fullName") || "Full Name"} *</label>
                  <input
                    type="text"
                    value={form.fullName}
                    onChange={updateField("fullName")}
                    required
                    placeholder={t("users.fullName") || "Full Name"}
                  />
                </div>

                <div className="form-group">
                  <label>{t("users.email") || "Email"}</label>
                  <input
                    type="email"
                    value={form.email}
                    onChange={updateField("email")}
                    placeholder="email@example.com"
                  />
                </div>

                <div className="form-group">
                  <label>
                    {t("users.password") || "Password"}
                    {editingId ? "" : " *"}
                  </label>
                  <input
                    type="password"
                    value={form.password}
                    onChange={updateField("password")}
                    required={!editingId}
                    minLength={4}
                    placeholder={
                      editingId
                        ? t("users.passwordLeaveEmpty") ||
                          "Leave empty to keep current"
                        : t("users.password") || "Password"
                    }
                  />
                  {editingId && (
                    <label
                      style={{
                        display: "flex",
                        alignItems: "center",
                        gap: 8,
                        marginTop: 8,
                        fontSize: "0.82rem",
                        color: "#8b949e",
                        cursor: "pointer",
                      }}
                    >
                      <input
                        type="checkbox"
                        checked={form.changePassword}
                        onChange={updateField("changePassword")}
                        style={{ width: "auto", cursor: "pointer" }}
                      />
                      {t("users.changePassword") || "Change password"}
                    </label>
                  )}
                </div>

                <div className="form-group">
                  <label>{t("users.role") || "Role"} *</label>
                  <select
                    value={form.role}
                    onChange={updateField("role")}
                    required
                  >
                    {ROLES.map((r) => (
                      <option key={r} value={r}>
                        {r}
                      </option>
                    ))}
                  </select>
                </div>

                {formError && <div style={st.formError}>{formError}</div>}
              </div>

              <div style={st.modalFooter}>
                <button
                  type="button"
                  className="btn btn-outline"
                  onClick={closeModal}
                  disabled={saving}
                >
                  {t("common.cancel")}
                </button>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={saving}
                >
                  {saving ? t("common.loading") : t("common.save")}
                </button>
              </div>
            </form>
          </div>
        </>
      )}
    </div>
  );
}

const st = {
  pageTitle: {
    fontSize: "1.25rem",
    fontWeight: 600,
    color: "#c9d1d9",
    margin: 0,
  },
  actionBtns: {
    display: "flex",
    gap: 6,
    flexWrap: "nowrap",
  },
  emptyState: {
    textAlign: "center",
    padding: "60px 20px",
    color: "#6e7681",
  },
  emptyIcon: {
    fontSize: "3rem",
    marginBottom: 12,
    opacity: 0.4,
  },
  emptyText: {
    fontSize: "1rem",
    fontWeight: 500,
    color: "#6e7681",
  },
  overlay: {
    position: "fixed",
    inset: 0,
    background: "rgba(0,0,0,0.6)",
    zIndex: 200,
  },
  modal: {
    position: "fixed",
    top: "50%",
    left: "50%",
    transform: "translate(-50%, -50%)",
    width: 500,
    maxWidth: "95vw",
    maxHeight: "90vh",
    overflowY: "auto",
    background: "#161b22",
    border: "1px solid #30363d",
    borderRadius: 8,
    boxShadow: "0 8px 32px rgba(0,0,0,0.5)",
    zIndex: 201,
    display: "flex",
    flexDirection: "column",
  },
  modalHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "16px 20px",
    borderBottom: "1px solid #30363d",
  },
  modalTitle: {
    fontSize: "1.1rem",
    fontWeight: 600,
    color: "#c9d1d9",
    margin: 0,
  },
  closeBtn: {
    background: "none",
    border: "none",
    color: "#8b949e",
    fontSize: "1.5rem",
    cursor: "pointer",
    lineHeight: 1,
    padding: "0 4px",
    transition: "color 0.2s",
  },
  modalBody: {
    padding: "20px",
    flex: 1,
  },
  modalFooter: {
    display: "flex",
    justifyContent: "flex-end",
    gap: 10,
    padding: "16px 20px",
    borderTop: "1px solid #30363d",
  },
  formError: {
    background: "rgba(248,81,73,0.1)",
    border: "1px solid rgba(248,81,73,0.3)",
    borderRadius: 4,
    padding: "10px 14px",
    color: "#f85149",
    fontSize: "0.85rem",
    marginTop: 8,
  },
  toastContainer: {
    position: "fixed",
    top: 16,
    right: 16,
    zIndex: 500,
    display: "flex",
    flexDirection: "column",
    gap: 8,
  },
  toast: {
    padding: "12px 18px",
    borderRadius: 8,
    fontSize: "0.875rem",
    fontWeight: 500,
    color: "#fff",
    boxShadow: "0 2px 12px rgba(0,0,0,0.4)",
    maxWidth: 380,
    wordBreak: "break-word",
  },
};
