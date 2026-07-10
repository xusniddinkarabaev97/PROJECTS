import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function Clients() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.getClients();
      setData(Array.isArray(res) ? res : []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const formatDate = (d) => {
    if (!d) return "—";
    try { return new Date(d).toLocaleString(); } catch { return String(d); }
  };

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>👥 {t("clients")}</h2>
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
          <div className="empty-state-icon">👥</div>
          <p>{t("noClients")}</p>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th><th>{t("fullName")}</th><th>{t("phone")}</th><th>{t("email")}</th><th>{t("source")}</th><th>{t("isVerified")}</th><th>{t("registeredAt")}</th>
                </tr>
              </thead>
              <tbody>
                {data.map((item) => (
                  <tr key={item.id}>
                    <td style={{ color: "var(--text-muted)", fontSize: 12 }}>#{item.id}</td>
                    <td style={{ fontWeight: 600 }}>{item.fullName || "—"}</td>
                    <td>{item.phone || "—"}</td>
                    <td>{item.email || "—"}</td>
                    <td><span className="badge badge-info">{item.source || "payme"}</span></td>
                    <td>
                      <span className={`badge ${item.isVerified ? "badge-success" : "badge-warning"}`}>
                        {item.isVerified ? "✅" : "⏳"}
                      </span>
                    </td>
                    <td style={{ color: "var(--text-secondary)", fontSize: 12 }}>{formatDate(item.registeredAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
