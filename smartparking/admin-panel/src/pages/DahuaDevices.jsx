import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const emptyDevice = {
  name: "", channelId: "", ipAddress: "", apiBaseUrl: "",
  deviceType: "camera", direction: "entry", barrierChannel: "", stationId: "",
};

export default function DahuaDevices() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [stations, setStations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyDevice);
  const [saving, setSaving] = useState(false);
  const [deleteId, setDeleteId] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [devices, sts] = await Promise.all([
        api.getDahuaDevices().catch(() => []),
        api.getStations().catch(() => []),
      ]);
      setData(Array.isArray(devices) ? devices : []);
      setStations(Array.isArray(sts) ? sts : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const openAdd = () => { setForm(emptyDevice); setModal("add"); };
  const openEdit = (item) => {
    setForm({
      name: item.name || "",
      channelId: item.channelId || "",
      ipAddress: item.ipAddress || "",
      apiBaseUrl: item.apiBaseUrl || "",
      deviceType: item.deviceType || "camera",
      direction: item.direction || "entry",
      barrierChannel: item.barrierChannel?.toString() || "",
      stationId: item.stationId?.toString() || "",
    });
    setModal({ type: "edit", id: item.id });
  };

  const handleSave = async () => {
    if (!form.name.trim() || !form.channelId.trim()) return;
    setSaving(true);
    try {
      const payload = {
        ...form,
        barrierChannel: form.barrierChannel ? parseInt(form.barrierChannel) : null,
        stationId: form.stationId ? parseInt(form.stationId) : null,
      };
      if (modal.type === "edit") {
        await api.updateDahuaDevice(modal.id, { ...payload, id: modal.id });
      } else {
        await api.createDahuaDevice(payload);
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
      await api.deleteDahuaDevice(deleteId);
      setDeleteId(null);
      fetchData();
    } catch (err) { setError(err.message); }
  };

  const getStationName = (id) => stations.find((s) => s.id === id)?.name || `#${id}`;

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>📷 {t("dahuaDevices")}</h2>
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
          <div className="empty-state-icon">📷</div>
          <p>{t("noDevices")}</p>
          <button className="btn btn-primary" style={{ marginTop: 16 }} onClick={openAdd}>{t("addFirstDevice")}</button>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>{t("name")}</th><th>{t("channelId")}</th><th>IP</th><th>{t("type")}</th><th>{t("direction")}</th><th>{t("station")}</th><th>{t("barrier")}</th><th>{t("actions")}</th>
                </tr>
              </thead>
              <tbody>
                {data.map((item) => (
                  <tr key={item.id}>
                    <td style={{ fontWeight: 600 }}>{item.name}</td>
                    <td style={{ fontFamily: "monospace", fontSize: 12 }}>{item.channelId}</td>
                    <td style={{ fontFamily: "monospace", fontSize: 12 }}>{item.ipAddress || "—"}</td>
                    <td><span className="badge badge-info">{item.deviceType}</span></td>
                    <td><span className={`badge ${item.direction === "entry" ? "badge-success" : item.direction === "exit" ? "badge-warning" : "badge-accent"}`}>{item.direction}</span></td>
                    <td>{item.stationId ? getStationName(item.stationId) : "—"}</td>
                    <td>{item.barrierChannel ? `CH${item.barrierChannel}` : "—"}</td>
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
            <h3 className="modal-title">{modal.type === "edit" ? t("edit") : t("add")} {t("device")}</h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              <input className="input" placeholder={t("name")} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              <div style={{ display: "flex", gap: 8 }}>
                <input className="input" style={{ flex: 1 }} placeholder={t("channelId")} value={form.channelId} onChange={(e) => setForm({ ...form, channelId: e.target.value })} />
                <input className="input" style={{ flex: 1 }} placeholder={t("ipAddress")} value={form.ipAddress} onChange={(e) => setForm({ ...form, ipAddress: e.target.value })} />
              </div>
              <select className="input" value={form.deviceType} onChange={(e) => setForm({ ...form, deviceType: e.target.value })}>
                <option value="camera">📷 Camera (ANPR)</option>
                <option value="access_controller">🚧 Access Controller</option>
                <option value="display">📟 Display</option>
              </select>
              <select className="input" value={form.direction} onChange={(e) => setForm({ ...form, direction: e.target.value })}>
                <option value="entry">🟢 Entry</option>
                <option value="exit">🔴 Exit</option>
                <option value="both">🔵 Both</option>
              </select>
              <div style={{ display: "flex", gap: 8 }}>
                <select className="input" style={{ flex: 1 }} value={form.stationId} onChange={(e) => setForm({ ...form, stationId: e.target.value })}>
                  <option value="">{t("selectStation")}</option>
                  {stations.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
                <input className="input" style={{ flex: 1 }} placeholder={t("barrierChannel")} type="number" value={form.barrierChannel} onChange={(e) => setForm({ ...form, barrierChannel: e.target.value })} />
              </div>
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
