import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const emptyForm = { shareholderName: "", percent: "", planId: "" };

export default function SharePercents() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [sp, pl] = await Promise.all([
        api.getSharePercents().catch(() => []),
        api.getPlans().catch(() => []),
      ]);
      setData(Array.isArray(sp) ? sp : []);
      setPlans(Array.isArray(pl) ? pl : []);
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
    setForm(emptyForm);
    setModal("add");
  };
  const openEdit = (item) => {
    setForm({
      shareholderName: item.shareholderName || "",
      percent: item.percent?.toString() || "",
      planId: item.planId?.toString() || "",
    });
    setModal({ type: "edit", id: item.id });
  };

  const handleSave = async () => {
    if (!form.shareholderName.trim() || !form.planId) return;
    setSaving(true);
    try {
      const payload = {
        shareholderName: form.shareholderName,
        percent: parseFloat(form.percent) || 0,
        planId: parseInt(form.planId),
      };
      if (modal.type === "edit") {
        await api.updateSharePercent(modal.id, { ...payload, id: modal.id });
      } else {
        await api.createSharePercent(payload);
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
      await api.deleteSharePercent(deleteId);
      setDeleteId(null);
      fetchData();
    } catch (err) {
      setError(err.message);
    }
  };

  const getPlanName = (planId) => {
    const p = plans.find((pl) => pl.id === planId);
    return p ? p.name : `#${planId}`;
  };

  // Group by plan
  const grouped = {};
  data.forEach((sp) => {
    const key = sp.planId || 0;
    if (!grouped[key]) grouped[key] = [];
    grouped[key].push(sp);
  });

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>
          📊 {t("sharePercents")}
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
          <div className="empty-state-icon">📊</div>
          <p>{t("noSharePercents")}</p>
          <button className="btn btn-primary" style={{ marginTop: 16 }} onClick={openAdd}>
            {t("addFirstShare")}
          </button>
        </div>
      ) : (
        Object.entries(grouped).map(([planId, items]) => (
          <div key={planId} className="card" style={{ marginBottom: 16 }}>
            <h3 style={{ marginBottom: 12, fontWeight: 600, color: "var(--accent)" }}>
              📋 {getPlanName(parseInt(planId))}
              <span style={{ fontSize: 12, color: "var(--text-muted)", marginLeft: 8 }}>
                ({items.reduce((s, i) => s + (i.percent || 0), 0).toFixed(1)}%)
              </span>
            </h3>
            <div className="card" style={{ padding: 0, overflow: "hidden", background: "var(--bg-primary)" }}>
              <div style={{ overflowX: "auto" }}>
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>{t("shareholder")}</th>
                      <th>{t("percent")}</th>
                      <th>{t("actions")}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((sp) => (
                      <tr key={sp.id}>
                        <td style={{ fontWeight: 600 }}>{sp.shareholderName}</td>
                        <td>
                          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                            <div
                              style={{
                                flex: 1,
                                height: 8,
                                background: "var(--bg-hover)",
                                borderRadius: 4,
                                overflow: "hidden",
                                maxWidth: 120,
                              }}
                            >
                              <div
                                style={{
                                  width: `${Math.min(sp.percent || 0, 100)}%`,
                                  height: "100%",
                                  background: "var(--accent)",
                                  borderRadius: 4,
                                }}
                              />
                            </div>
                            <span style={{ fontWeight: 600, fontSize: 13 }}>
                              {sp.percent}%
                            </span>
                          </div>
                        </td>
                        <td>
                          <div style={{ display: "flex", gap: 6 }}>
                            <button className="btn btn-ghost btn-sm" onClick={() => openEdit(sp)}>
                              ✏️
                            </button>
                            <button className="btn btn-danger btn-sm" onClick={() => setDeleteId(sp.id)}>
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
          </div>
        ))
      )}

      {/* Add/Edit Modal */}
      {modal && (
        <div className="modal-overlay" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">
              {modal.type === "edit" ? t("edit") : t("add")} {t("sharePercent")}
            </h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              <input
                className="input"
                placeholder={t("shareholder")}
                value={form.shareholderName}
                onChange={(e) => setForm({ ...form, shareholderName: e.target.value })}
              />
              <input
                className="input"
                placeholder={t("percent")}
                type="number"
                step="0.1"
                min="0"
                max="100"
                value={form.percent}
                onChange={(e) => setForm({ ...form, percent: e.target.value })}
              />
              <select
                className="input"
                value={form.planId}
                onChange={(e) => setForm({ ...form, planId: e.target.value })}
              >
                <option value="">{t("selectPlan")}</option>
                {plans.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
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

      {/* Delete Confirm Modal */}
      {deleteId && (
        <div className="modal-overlay" onClick={() => setDeleteId(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">{t("deleteConfirmTitle")}</h3>
            <p style={{ color: "var(--text-secondary)", marginBottom: 20 }}>
              {t("confirm")}?
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
