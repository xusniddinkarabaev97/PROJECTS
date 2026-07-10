import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const emptyCompany = {
  name: "",
  inn: "",
  email: "",
  phone: "",
  address: "",
  jwtAuthToken: "",
};

export default function Companies() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyCompany);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.getCompanies();
      setData(Array.isArray(res) ? res : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const openAdd = () => {
    setForm(emptyCompany);
    setModal("add");
  };
  const openEdit = (item) => {
    setForm({
      name: item.name || "",
      inn: item.inn || "",
      email: item.email || "",
      phone: item.phone || "",
      address: item.address || "",
      jwtAuthToken: item.jwtAuthToken || "",
    });
    setModal({ type: "edit", id: item.id });
  };

  const handleSave = async () => {
    if (!form.name.trim()) return;
    if (!form.inn.trim()) return;
    setSaving(true);
    try {
      if (modal.type === "edit") {
        await api.updateCompany(modal.id, { ...form, id: modal.id });
      } else {
        await api.createCompany(form);
      }
      setModal(null);
      fetchData();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!deleteId) return;
    try {
      await api.deleteCompany(deleteId);
      setDeleteId(null);
      fetchData();
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div>
      <div className="card-header">
        <h2
          style={{
            fontSize: 24,
            fontWeight: 700,
            color: "var(--text-primary)",
          }}
        >
          🏢 {t("companies")}
        </h2>
        <button className="btn btn-primary" onClick={openAdd}>
          + {t("add")}
        </button>
      </div>

      {error && (
        <div
          style={{
            background: "var(--danger-bg)",
            border: "1px solid var(--danger)",
            color: "var(--danger)",
            padding: "12px 16px",
            borderRadius: 8,
            marginBottom: 16,
            display: "flex",
            justifyContent: "space-between",
          }}
        >
          <span>{error}</span>
          <button
            onClick={() => setError(null)}
            style={{
              color: "var(--danger)",
              fontWeight: 700,
              cursor: "pointer",
              background: "none",
              border: "none",
            }}
          >
            ×
          </button>
        </div>
      )}

      {loading ? (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: 60,
            gap: 12,
          }}
        >
          <div className="spinner" />
          <span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
        </div>
      ) : data.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">🏢</div>
          <p>{t("noCompanies")}</p>
          <button
            className="btn btn-primary"
            style={{ marginTop: 16 }}
            onClick={openAdd}
          >
            {t("addFirstCompany")}
          </button>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>{t("name")}</th>
                  <th>{t("inn")}</th>
                  <th>{t("email")}</th>
                  <th>{t("token")}</th>
                  <th>{t("actions")}</th>
                </tr>
              </thead>
              <tbody>
                {data.map((item) => (
                  <tr key={item.id}>
                    <td style={{ color: "var(--text-muted)", fontSize: 12 }}>
                      #{item.id}
                    </td>
                    <td style={{ fontWeight: 600 }}>{item.name}</td>
                    <td>{item.inn || "—"}</td>
                    <td>{item.email || "—"}</td>
                    <td
                      style={{
                        fontFamily: "monospace",
                        fontSize: 11,
                        maxWidth: 200,
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                      }}
                    >
                      {item.jwtAuthToken ? "••••••••" : "—"}
                    </td>
                    <td>
                      <div style={{ display: "flex", gap: 6 }}>
                        <button
                          className="btn btn-ghost btn-sm"
                          onClick={() => openEdit(item)}
                        >
                          ✏️
                        </button>
                        <button
                          className="btn btn-danger btn-sm"
                          onClick={() => setDeleteId(item.id)}
                        >
                          🗑️
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {modal && (
        <div className="modal-overlay" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">
              {modal.type === "edit" ? t("editCompany") : t("newCompany")}
            </h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              <input
                className="input"
                placeholder={t("name")}
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
              <input
                className="input"
                placeholder={t("inn")}
                value={form.inn}
                onChange={(e) => setForm({ ...form, inn: e.target.value })}
              />
              <input
                className="input"
                placeholder={t("email")}
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
              />
              <input
                className="input"
                placeholder={t("phone")}
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
              />
              <input
                className="input"
                placeholder={t("address")}
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
              />
              <input
                className="input"
                placeholder={t("password")}
                type="password"
                value={form.jwtAuthToken}
                onChange={(e) =>
                  setForm({ ...form, jwtAuthToken: e.target.value })
                }
              />
              <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                <button
                  className="btn btn-primary"
                  style={{ flex: 1, justifyContent: "center" }}
                  onClick={handleSave}
                  disabled={saving}
                >
                  {saving ? t("saving") : t("save")}
                </button>
                <button
                  className="btn btn-ghost"
                  style={{ flex: 1, justifyContent: "center" }}
                  onClick={() => setModal(null)}
                >
                  {t("cancel")}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {deleteId && (
        <div className="modal-overlay" onClick={() => setDeleteId(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">{t("deleteConfirmTitle")}</h3>
            <p style={{ color: "var(--text-secondary)", marginBottom: 20 }}>
              {t("deleteCompanyConfirm")}
            </p>
            <div style={{ display: "flex", gap: 8 }}>
              <button
                className="btn btn-danger"
                style={{ flex: 1, justifyContent: "center" }}
                onClick={handleDelete}
              >
                {t("delete")}
              </button>
              <button
                className="btn btn-ghost"
                style={{ flex: 1, justifyContent: "center" }}
                onClick={() => setDeleteId(null)}
              >
                {t("cancel")}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
