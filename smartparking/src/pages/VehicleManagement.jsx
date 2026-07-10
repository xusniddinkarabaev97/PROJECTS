import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const emptyVehicle = { plateNumber: "", ownerName: "", phone: "", category: "regular", notes: "", validFrom: "", validUntil: "" };

export default function VehicleManagement() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyVehicle);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState(null);
  const [filter, setFilter] = useState("all");

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.getDahuaVehicles(filter === "all" ? "" : filter);
      setData(Array.isArray(res) ? res : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => { fetchData(); }, [fetchData]);

  const openAdd = () => { setForm(emptyVehicle); setModal("add"); };
  const openEdit = (item) => {
    setForm({
      plateNumber: item.plateNumber || "",
      ownerName: item.ownerName || "",
      phone: item.phone || "",
      category: item.category || "regular",
      notes: item.notes || "",
      validFrom: item.validFrom ? item.validFrom.slice(0, 10) : "",
      validUntil: item.validUntil ? item.validUntil.slice(0, 10) : "",
    });
    setModal({ type: "edit", id: item.id });
  };

  const handleSave = async () => {
    if (!form.plateNumber.trim()) return;
    setSaving(true);
    try {
      const payload = { ...form, isEnabled: true };
      if (modal.type === "edit") {
        await api.updateDahuaVehicle(modal.id, { ...payload, id: modal.id });
      } else {
        await api.createDahuaVehicle(payload);
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
      await api.deleteDahuaVehicle(deleteId);
      setDeleteId(null);
      fetchData();
    } catch (err) { setError(err.message); }
  };

  const CAT_COLORS = { regular: "badge-info", employee: "badge-success", vip: "badge-accent", blocked: "badge-danger" };

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>🚙 {t("vehicleManagement")}</h2>
        <button className="btn btn-primary" onClick={openAdd}>+ {t("add")}</button>
      </div>

      <div className="tab-bar">
        {["all", "regular", "employee", "vip", "blocked"].map((f) => (
          <button key={f} className={`tab ${filter === f ? "active" : ""}`} onClick={() => setFilter(f)}>
            {t(f)}
          </button>
        ))}
      </div>

      {error && (
        <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: "12px 16px", borderRadius: 8, marginBottom: 16, display: "flex", justifyContent: "space-between" }}>
          <span>{error}</span>
          <button onClick={() => setError(null)} style={{ color: "var(--danger)", fontWeight: 700, cursor: "pointer", background: "none", border: "none" }}>×</button>
        </div>
      )}

      {loading ? (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}>
          <div className="spinner" /><span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
        </div>
      ) : data.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">🚙</div>
          <p>{t("noVehiclesInList")}</p>
          <button className="btn btn-primary" style={{ marginTop: 16 }} onClick={openAdd}>{t("addFirstVehicle")}</button>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>{t("plateNumber")}</th><th>{t("owner")}</th><th>{t("phone")}</th><th>{t("category")}</th><th>{t("validity")}</th><th>{t("actions")}</th>
                </tr>
              </thead>
              <tbody>
                {data.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <span style={{ fontWeight: 700, fontFamily: "monospace", fontSize: 14, background: "var(--bg-primary)", padding: "3px 10px", borderRadius: 6, border: "1px solid var(--border)" }}>
                        {item.plateNumber}
                      </span>
                    </td>
                    <td>{item.ownerName || "—"}</td>
                    <td>{item.phone || "—"}</td>
                    <td><span className={`badge ${CAT_COLORS[item.category] || "badge-info"}`}>{t(item.category)}</span></td>
                    <td style={{ fontSize: 12, color: "var(--text-muted)" }}>
                      {item.validFrom ? new Date(item.validFrom).toLocaleDateString() : "∞"} → {item.validUntil ? new Date(item.validUntil).toLocaleDateString() : "∞"}
                    </td>
                    <td>
                      <div style={{ display: "flex", gap: 6 }}>
                        <button className="btn btn-ghost btn-sm" onClick={() => openEdit(item)}>✏️</button>
                        <button className="btn btn-danger btn-sm" onClick={() => setDeleteId(item.id)}>🗑️</button>
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
            <h3 className="modal-title">{modal.type === "edit" ? t("edit") : t("add")} {t("vehicle")}</h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              <input className="input" placeholder={t("plateNumber") + " *"} value={form.plateNumber} onChange={(e) => setForm({ ...form, plateNumber: e.target.value })} />
              <input className="input" placeholder={t("owner")} value={form.ownerName} onChange={(e) => setForm({ ...form, ownerName: e.target.value })} />
              <input className="input" placeholder={t("phone")} value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
              <select className="input" value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })}>
                <option value="regular">🟢 {t("regular")}</option>
                <option value="employee">🔵 {t("employee")}</option>
                <option value="vip">🟣 {t("vip")}</option>
                <option value="blocked">🔴 {t("blocked")}</option>
              </select>
              <div style={{ display: "flex", gap: 8 }}>
                <input className="input" type="date" placeholder={t("validFrom")} value={form.validFrom} onChange={(e) => setForm({ ...form, validFrom: e.target.value })} />
                <input className="input" type="date" placeholder={t("validUntil")} value={form.validUntil} onChange={(e) => setForm({ ...form, validUntil: e.target.value })} />
              </div>
              <input className="input" placeholder={t("notes")} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
              <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                <button className="btn btn-primary" style={{ flex: 1, justifyContent: "center" }} onClick={handleSave} disabled={saving}>{saving ? t("saving") : t("save")}</button>
                <button className="btn btn-ghost" style={{ flex: 1, justifyContent: "center" }} onClick={() => setModal(null)}>{t("cancel")}</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {deleteId && (
        <div className="modal-overlay" onClick={() => setDeleteId(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">{t("deleteConfirmTitle")}</h3>
            <p style={{ color: "var(--text-secondary)", marginBottom: 20 }}>{t("confirm")}?</p>
            <div style={{ display: "flex", gap: 8 }}>
              <button className="btn btn-danger" style={{ flex: 1, justifyContent: "center" }} onClick={handleDelete}>{t("delete")}</button>
              <button className="btn btn-ghost" style={{ flex: 1, justifyContent: "center" }} onClick={() => setDeleteId(null)}>{t("cancel")}</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
