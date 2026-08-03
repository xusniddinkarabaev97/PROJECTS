import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function AuditLogs() {
  const { t } = useTranslation();
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    try { setLogs(await api.getAuditLogs()); }
    catch { setLogs([]); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { fetchLogs(); }, [fetchLogs]);

  const actionBadge = (action) => {
    const map = { Login: "badge-info", CreateTransaction: "badge-success", Refund: "badge-warning", PriceChange: "badge-accent", RoleChange: "badge-accent", Logout: "badge-info" };
    return map[action] || "badge-info";
  };

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
        <h2 style={{ fontSize: 24, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#fff" }}>📋 {t("auditLogs")}</h2>
        <button onClick={fetchLogs} className="btn btn-ghost btn-sm" style={{ fontSize: 16 }}>🔄</button>
      </div>

      <div style={{ marginBottom: 16, padding: "10px 14px", background: "rgba(91,140,62,0.06)", border: "1px solid var(--border)", borderRadius: 10, fontSize: 12, color: "var(--text-secondary)" }}>
        🔒 Журнал работает в режиме <strong style={{ color: "var(--accent)" }}>Append-Only</strong> — записи нельзя редактировать или удалить.
      </div>

      {loading ? (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}><div className="spinner" /><span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span></div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead><tr><th>Дата/Время</th><th>Пользователь</th><th>Действие</th><th>Детали</th><th>IP</th></tr></thead>
              <tbody>
                {logs.length === 0 ? (
                  <tr><td colSpan={5} style={{ textAlign: "center", padding: 24, color: "var(--text-muted)" }}>Записей пока нет</td></tr>
                ) : logs.map((log) => (
                  <tr key={log.id}>
                    <td style={{ color: "var(--text-secondary)", fontSize: 12, whiteSpace: "nowrap" }}>{log.timestamp ? new Date(log.timestamp).toLocaleString("ru-RU") : "—"}</td>
                    <td style={{ fontWeight: 600 }}>{log.userLogin || "—"}</td>
                    <td><span className={`badge ${actionBadge(log.action)}`}>{log.action || "—"}</span></td>
                    <td style={{ color: "var(--text-secondary)", fontSize: 12, maxWidth: 200, overflow: "hidden", textOverflow: "ellipsis" }}>{log.details || "—"}</td>
                    <td style={{ fontFamily: "monospace", fontSize: 11, color: "var(--text-muted)" }}>{log.ipAddress || "—"}</td>
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
