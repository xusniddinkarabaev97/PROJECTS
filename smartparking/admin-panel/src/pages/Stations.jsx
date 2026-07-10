import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const emptyStation = {
  name: "", region: "", district: "", address: "",
  companyId: 1, latitude: "", longitude: "",
};

export default function Stations() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyStation);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.getStations();
      setData(Array.isArray(res) ? res : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const openAdd = () => {
    setForm(emptyStation);
    setModal("add");
  };
  const openEdit = (item) => {
    setForm({
      name: item.name || "",
      region: item.region || "",
      district: item.district || "",
      address: item.address || "",
      companyId: item.companyId || 1,
      latitude: item.latitude?.toString() || "",
      longitude: item.longitude?.toString() || "",
    });
    setModal({ type: "edit", id: item.id });
  };

  const handleSave = async () => {
    if (!form.name.trim()) return;
    setSaving(true);
    try {
      const payload = {
        ...form,
        latitude: form.latitude ? parseFloat(form.latitude) : null,
        longitude: form.longitude ? parseFloat(form.longitude) : null,
      };
      if (modal.type === "edit") {
        await api.updateStation(modal.id, { ...payload, id: modal.id });
      } else {
        await api.createStation(payload);
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
      await api.deleteStation(deleteId);
      setDeleteId(null);
      fetchData();
    } catch (err) {
      setError(err.message);
    }
  };

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>🅿️ {t("stations")}</h2>
        <button className="btn btn-primary" onClick={openAdd}>+ {t("add")}</button>
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
          <div className="empty-state-icon">🅿️</div>
          <p>{t("noStations")}</p>
          <button className="btn btn-primary" style={{ marginTop: 16 }} onClick={openAdd}>{t("addFirstStation")}</button>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th><th>{t("name")}</th><th>{t("region")}</th><th>{t("district")}</th><th>{t("address")}</th><th>{t("actions")}</th>
                </tr>
              </thead>
              <tbody>
                {data.map((item) => (
                  <tr key={item.id}>
                    <td style={{ color: "var(--text-muted)", fontSize: 12 }}>#{item.id}</td>
                    <td style={{ fontWeight: 600 }}>{item.name}</td>
                    <td>{item.region || "—"}</td>
                    <td>{item.district || "—"}</td>
                    <td style={{ maxWidth: 200, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{item.address || "—"}</td>
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

      {/* Add/Edit Modal */}
      {modal && (
        <div className="modal-overlay" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <h3 className="modal-title">{modal.type === "edit" ? t("editStation") : t("newStation")}</h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              <input className="input" placeholder={t("name")} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              <input className="input" placeholder={t("region")} value={form.region} onChange={(e) => setForm({ ...form, region: e.target.value })} />
              <input className="input" placeholder={t("district")} value={form.district} onChange={(e) => setForm({ ...form, district: e.target.value })} />
              <input className="input" placeholder={t("address")} value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
              <div style={{ display: "flex", gap: 8 }}>
                <input className="input" placeholder={t("latitude")} type="number" step="any" value={form.latitude} onChange={(e) => setForm({ ...form, latitude: e.target.value })} />
                <input className="input" placeholder={t("longitude")} type="number" step="any" value={form.longitude} onChange={(e) => setForm({ ...form, longitude: e.target.value })} />
              </div>
              <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                <button className="btn btn-primary" style={{ flex: 1, justifyContent: "center" }} onClick={handleSave} disabled={saving}>
                  {saving ? t("saving") : t("save")}
                </button>
                <button className="btn btn-ghost" style={{ flex: 1, justifyContent: "center" }} onClick={() => setModal(null)}>{t("cancel")}</button>
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
            <p style={{ color: "var(--text-secondary)", marginBottom: 20 }}>{t("deleteStationConfirm")}</p>
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
