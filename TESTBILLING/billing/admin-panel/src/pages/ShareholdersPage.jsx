import { useState, useEffect, useCallback } from "react";
import { useTranslation } from "../i18n/LanguageContext";

const BASE_URL = "/api/v1/shareholders";
const TOKEN_KEY = "gzs_billing_token";

const EMPTY_FORM = {
  fullName: "",
  company: "",
  sharePercentage: "",
  phone: "",
  email: "",
  contractNumber: "",
  contractDate: "",
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
    localStorage.removeItem("gzs_billing_user");
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

const fmtDate = (d) => {
  if (!d) return "-";
  try {
    const dt = new Date(d);
    return dt.toLocaleDateString("ru-RU", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
  } catch {
    return d;
  }
};

function getStatusLabel(status, t) {
  if (status === "inactive") return t ? t("common.inactive") : "Inactive";
  return t ? t("common.active") : "Active";
}

export default function ShareholdersPage() {
  const { t } = useTranslation();

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");

  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);

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
    setForm(EMPTY_FORM);
    setFormError("");
    setModalOpen(true);
  };

  const openEdit = (item) => {
    setEditingId(item.id);
    setForm({
      fullName: item.fullName || "",
      company: item.company || "",
      sharePercentage: item.sharePercentage ?? item.share_percentage ?? "",
      phone: item.phone || "",
      email: item.email || "",
      contractNumber: item.contractNumber ?? item.contract_number ?? "",
      contractDate: item.contractDate
        ? item.contractDate.slice(0, 10)
        : item.contract_date
          ? item.contract_date.slice(0, 10)
          : "",
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
    const share = Number(form.sharePercentage);
    if (Number.isNaN(share) || share < 0 || share > 100) {
      setFormError(
        t("shareholders.validation.sharePercent") ||
          "Share % must be between 0 and 100",
      );
      return;
    }
    const payload = {
      fullName: form.fullName.trim(),
      company: form.company.trim(),
      sharePercentage: share,
      phone: form.phone.trim(),
      email: form.email.trim(),
      contractNumber: form.contractNumber.trim(),
      contractDate: form.contractDate || null,
    };
    try {
      setSaving(true);
      if (editingId) {
        await api(BASE_URL + "/" + editingId, { method: "PUT", body: payload });
        showToast(
          "success",
          t("common.success") +
            " \u2014 " +
            (t("shareholders.updated") || "Updated"),
        );
      } else {
        await api(BASE_URL, { method: "POST", body: payload });
        showToast(
          "success",
          t("common.success") +
            " \u2014 " +
            (t("shareholders.created") || "Created"),
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

  const confirmDelete = (item) => {
    setDeleteTarget(item);
  };

  const cancelDelete = () => {
    if (deleting) return;
    setDeleteTarget(null);
  };

  const executeDelete = async () => {
    if (!deleteTarget) return;
    try {
      setDeleting(true);
      await api(BASE_URL + "/" + deleteTarget.id, { method: "DELETE" });
      showToast(
        "success",
        t("common.success") +
          " \u2014 " +
          (t("shareholders.deleted") || "Deleted"),
      );
      setDeleteTarget(null);
      fetchData();
    } catch (err) {
      showToast("error", err.message);
    } finally {
      setDeleting(false);
    }
  };

  const updateField = (field) => (e) => {
    setForm((prev) => ({ ...prev, [field]: e.target.value }));
  };

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
          {t("shareholders.pageTitle") || "Ulishdorlar / Shareholders"}
        </h1>
        <button className="btn btn-primary" onClick={openCreate}>
          + {t("shareholders.addShareholder") || "Add Shareholder"}
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
                <th>{t("shareholders.fullName") || "Full Name"}</th>
                <th>{t("shareholders.company") || "Company"}</th>
                <th>{t("shareholders.sharePercent") || "Share %"}</th>
                <th>{t("shareholders.phone") || "Phone"}</th>
                <th>{t("shareholders.email") || "Email"}</th>
                <th>{t("shareholders.contractNumber") || "Contract #"}</th>
                <th>{t("shareholders.contractDate") || "Contract Date"}</th>
                <th>{t("common.status")}</th>
                <th>{t("common.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id}>
                  <td>{item.fullName || "-"}</td>
                  <td>{item.company || "-"}</td>
                  <td className="font-mono">
                    {item.sharePercentage ?? item.share_percentage ?? "-"}%
                  </td>
                  <td>{item.phone || "-"}</td>
                  <td>{item.email || "-"}</td>
                  <td className="font-mono text-sm">
                    {item.contractNumber ?? item.contract_number ?? "-"}
                  </td>
                  <td className="text-muted text-sm">
                    {fmtDate(item.contractDate ?? item.contract_date)}
                  </td>
                  <td>
                    <span
                      className={
                        "badge " +
                        (item.status === "inactive"
                          ? "badge-muted"
                          : "badge-success")
                      }
                    >
                      {getStatusLabel(item.status, t)}
                    </span>
                  </td>
                  <td>
                    <div style={st.actionBtns}>
                      <button
                        className="btn btn-outline btn-sm"
                        onClick={() => openEdit(item)}
                      >
                        {t("common.edit")}
                      </button>
                      <button
                        className="btn btn-danger btn-sm"
                        onClick={() => confirmDelete(item)}
                      >
                        {t("common.delete")}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
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
                  ? t("common.edit") +
                    " " +
                    (t("shareholders.shareholder") || "Shareholder")
                  : t("common.create") +
                    " " +
                    (t("shareholders.shareholder") || "Shareholder")}
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
                  <label>{t("shareholders.fullName") || "Full Name"} *</label>
                  <input
                    type="text"
                    value={form.fullName}
                    onChange={updateField("fullName")}
                    required
                    placeholder={t("shareholders.fullName") || "Full Name"}
                  />
                </div>

                <div className="form-group">
                  <label>{t("shareholders.company") || "Company"}</label>
                  <input
                    type="text"
                    value={form.company}
                    onChange={updateField("company")}
                    placeholder={t("shareholders.company") || "Company"}
                  />
                </div>

                <div className="form-group">
                  <label>
                    {t("shareholders.sharePercent") || "Share %"} * (0\u2013100)
                  </label>
                  <input
                    type="number"
                    value={form.sharePercentage}
                    onChange={updateField("sharePercentage")}
                    required
                    min={0}
                    max={100}
                    step="0.01"
                    placeholder="0 \u2013 100"
                  />
                </div>

                <div className="form-group">
                  <label>{t("shareholders.phone") || "Phone"}</label>
                  <input
                    type="text"
                    value={form.phone}
                    onChange={updateField("phone")}
                    placeholder="+998 XX XXX XX XX"
                  />
                </div>

                <div className="form-group">
                  <label>{t("shareholders.email") || "Email"}</label>
                  <input
                    type="email"
                    value={form.email}
                    onChange={updateField("email")}
                    placeholder="email@example.com"
                  />
                </div>

                <div className="form-group">
                  <label>
                    {t("shareholders.contractNumber") || "Contract #"}
                  </label>
                  <input
                    type="text"
                    value={form.contractNumber}
                    onChange={updateField("contractNumber")}
                    placeholder="#"
                  />
                </div>

                <div className="form-group">
                  <label>
                    {t("shareholders.contractDate") || "Contract Date"}
                  </label>
                  <input
                    type="date"
                    value={form.contractDate}
                    onChange={updateField("contractDate")}
                  />
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

      {deleteTarget && (
        <>
          <div style={st.overlay} onClick={cancelDelete} />
          <div style={st.modalSmall}>
            <div style={st.modalHeader}>
              <h2 style={st.modalTitle}>{t("common.confirmDelete")}</h2>
              <button
                style={st.closeBtn}
                onClick={cancelDelete}
                disabled={deleting}
              >
                &times;
              </button>
            </div>
            <div style={st.modalBody}>
              <p style={{ color: "#c9d1d9", fontSize: "0.95rem", margin: 0 }}>
                {t("shareholders.confirmDeleteMsg") ||
                  "Are you sure you want to delete this shareholder?"}{" "}
                <strong>{deleteTarget.fullName}</strong>?
              </p>
            </div>
            <div style={st.modalFooter}>
              <button
                className="btn btn-outline"
                onClick={cancelDelete}
                disabled={deleting}
              >
                {t("common.cancel")}
              </button>
              <button
                className="btn btn-danger"
                onClick={executeDelete}
                disabled={deleting}
              >
                {deleting
                  ? t("common.loading")
                  : t("common.yes") + ", " + t("common.delete")}
              </button>
            </div>
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
    width: 520,
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
  modalSmall: {
    position: "fixed",
    top: "50%",
    left: "50%",
    transform: "translate(-50%, -50%)",
    width: 440,
    maxWidth: "95vw",
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
