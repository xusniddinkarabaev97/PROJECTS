import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function ParkingSessionsMonitor() {
  const { t } = useTranslation();
  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState("active");

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.getParkingSessions(filter === "all" ? "" : filter);
      setSessions(Array.isArray(res) ? res : []);
    } catch {
      setSessions([]);
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 10000); // auto-refresh every 10s
    return () => clearInterval(interval);
  }, [fetchData]);

  const formatTime = (d) => {
    if (!d) return "—";
    try { return new Date(d).toLocaleString(); } catch { return String(d); }
  };

  const formatDuration = (ts) => {
    if (!ts) return "—";
    const h = Math.floor(ts / 3600);
    const m = Math.floor((ts % 3600) / 60);
    return `${h}h ${m}m`;
  };

  // Calculate duration for active sessions
  const getActiveDuration = (s) => {
    if (s.status !== "active" || !s.entryTime) return null;
    return Math.floor((Date.now() - new Date(s.entryTime).getTime()) / 1000);
  };

  const CAT_COLORS = { regular: "badge-info", employee: "badge-success", vip: "badge-accent", blocked: "badge-danger" };
  const STATUS_COLORS = { active: "badge-success", completed: "badge-info", cancelled: "badge-warning", expired: "badge-danger" };

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>
          🚦 {t("parkingSessions")}
        </h2>
        <button className="btn btn-ghost btn-sm" onClick={fetchData}>🔄</button>
      </div>

      <div className="tab-bar">
        {["active", "completed", "all"].map((f) => (
          <button key={f} className={`tab ${filter === f ? "active" : ""}`} onClick={() => setFilter(f)}>
            {t(f)} {f === "active" ? `(${sessions.filter(s => s.status === "active").length})` : ""}
          </button>
        ))}
      </div>

      {loading && sessions.length === 0 ? (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}>
          <div className="spinner" /><span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
        </div>
      ) : sessions.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">🚦</div>
          <p>{t("noSessions")}</p>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th><th>{t("plateNumber")}</th><th>{t("category")}</th><th>{t("status")}</th><th>{t("entry")}</th><th>{t("exit")}</th><th>{t("duration")}</th><th>{t("fee")}</th><th>{t("station")}</th>
                </tr>
              </thead>
              <tbody>
                {sessions.map((s) => (
                  <tr key={s.id} style={s.status === "active" ? { background: "rgba(35,134,54,0.05)" } : {}}>
                    <td style={{ color: "var(--text-muted)", fontSize: 12 }}>#{s.id}</td>
                    <td>
                      <span style={{ fontWeight: 700, fontFamily: "monospace" }}>{s.plateNumber}</span>
                    </td>
                    <td>
                      <span className={`badge ${CAT_COLORS[s.vehicleCategory] || "badge-info"}`}>
                        {t(s.vehicleCategory || "regular")}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${STATUS_COLORS[s.status] || "badge-info"}`}>
                        {t(s.status)}
                      </span>
                    </td>
                    <td style={{ fontSize: 12 }}>{formatTime(s.entryTime)}</td>
                    <td style={{ fontSize: 12 }}>{s.exitTime ? formatTime(s.exitTime) : "🟢 " + t("active")}</td>
                    <td style={{ fontWeight: 600 }}>
                      {s.duration ? formatDuration(s.duration) : getActiveDuration(s) ? formatDuration(getActiveDuration(s)) : "—"}
                    </td>
                    <td style={{ fontWeight: 600, color: s.parkingFee > 0 ? "var(--warning)" : "var(--text-secondary)" }}>
                      {s.parkingFee != null ? `${Number(s.parkingFee).toLocaleString()} UZS` : "—"}
                    </td>
                    <td style={{ fontSize: 12 }}>{s.station?.name || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div style={{ padding: "12px 16px", borderTop: "1px solid var(--border)", color: "var(--text-secondary)", fontSize: 12 }}>
            {t("total")}: {sessions.length} · {t("active")}: {sessions.filter(s => s.status === "active").length}
          </div>
        </div>
      )}
    </div>
  );
}
