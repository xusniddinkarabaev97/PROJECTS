import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function Transactions() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState("");
  const [exporting, setExporting] = useState(false);
  const [statusFilter, setStatusFilter] = useState("all");
  const [selectedTx, setSelectedTx] = useState(null);

  const fetchData = useCallback(async () => {
    setLoading(true); setError(null);
    try { const r = await api.getTransactions(); setData(Array.isArray(r) ? r : []); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { fetchData(); }, [fetchData]);

  const handleExport = async (period) => {
    setExporting(true);
    try {
      const now = new Date(); const year = now.getFullYear();
      const month = period === "month" ? now.getMonth() + 1 : null;
      const blob = await api.exportTransactions(year, month);
      const url = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = url;
      a.download = period === "month" ? `Otchet_${String(month).padStart(2,"0")}.${year}.xlsx` : `Otchet_${year}.xlsx`;
      a.click(); URL.revokeObjectURL(url);
    } catch (e) { setError(e.message); }
    finally { setExporting(false); }
  };

  let filtered = data;
  if (search) filtered = filtered.filter((tx) =>
    String(tx.id).includes(search) || String(tx.patientId).includes(search) ||
    tx.patient?.fullName?.toLowerCase().includes(search.toLowerCase())
  );
  if (statusFilter !== "all") filtered = filtered.filter((tx) => {
    if (statusFilter === "completed") return tx.paymentStatus === "Completed";
    if (statusFilter === "refund") return tx.paymentStatus === "Refunded";
    if (statusFilter === "pending") return tx.paymentStatus === "Pending" || tx.paymentStatus === 0;
    return true;
  });

  const formatAmount = (v) => v != null ? Number(v).toLocaleString() + " UZS" : "—";
  const getStatusBadge = (s) => {
    if (s === "Completed" || s === "Paid") return <span className="badge badge-success">Success</span>;
    if (s === "Refunded") return <span className="badge badge-warning">Refunded</span>;
    return <span className="badge badge-info">Pending</span>;
  };

  const btnExport = { background: "linear-gradient(135deg, #00a86b 0%, #00804f 100%)", color: "#fff", padding: "7px 16px", borderRadius: 10, fontWeight: 600, fontSize: 13, border: "none", cursor: "pointer", whiteSpace: "nowrap", display: "inline-flex", alignItems: "center", gap: 6, transition: "all 0.2s" };

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#fff" }}>💳 {t("transactions")}</h2>
        <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
          <select className="input" style={{ width: 140, padding: "7px 12px" }} value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
            <option value="all">Все статусы</option>
            <option value="completed">Success</option>
            <option value="refund">Refunded</option>
            <option value="pending">Pending</option>
          </select>
          <input className="input" style={{ width: 180, padding: "7px 12px" }} placeholder={t("search") + "..."} value={search} onChange={(e) => setSearch(e.target.value)} />
          <button onClick={() => handleExport("month")} disabled={exporting} style={btnExport}>{exporting ? "..." : "📥"} Excel (oy)</button>
          <button onClick={() => handleExport("year")} disabled={exporting} style={btnExport}>{exporting ? "..." : "📥"} Excel (yil)</button>
          <button className="btn btn-ghost btn-sm" onClick={fetchData} style={{ fontSize: 16 }}>🔄</button>
        </div>
      </div>

      {error && <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: "12px 16px", borderRadius: 12, marginBottom: 16, display: "flex", justifyContent: "space-between", backdropFilter: "blur(8px)" }}><span>{error}</span><button onClick={() => setError(null)} style={{ color: "var(--danger)", fontWeight: 700, cursor: "pointer", background: "none", border: "none", fontSize: 18 }}>×</button></div>}

      {loading ? (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}><div className="spinner" /><span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span></div>
      ) : data.length === 0 ? (
        <div className="empty-state"><div className="empty-state-icon">💳</div><p>{t("noTransactions")}</p></div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead><tr><th>ID</th><th>Дата/Время</th><th>Пациент</th><th>Сумма</th><th>Оплата</th><th>Статус</th><th>Кассир</th></tr></thead>
              <tbody>
                {filtered.map((tx) => (
                  <tr key={tx.id} onClick={() => setSelectedTx(tx)} style={{ cursor: "pointer" }}>
                    <td style={{ color: "var(--text-muted)", fontSize: 12 }}>#{tx.id}</td>
                    <td style={{ fontSize: 12, color: "var(--text-secondary)" }}>{tx.filledAt ? new Date(tx.filledAt).toLocaleString("ru-RU", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" }) : "—"}</td>
                    <td style={{ fontWeight: 600 }}>{tx.patient?.fullName || `ID: ${tx.patientId}`}</td>
                    <td style={{ fontWeight: 600, color: "var(--accent)" }}>{formatAmount(tx.totalSum)}</td>
                    <td style={{ fontSize: 12 }}>{tx.paymentMethod || "—"}</td>
                    <td>{getStatusBadge(tx.paymentStatus)}</td>
                    <td style={{ fontSize: 12, color: "var(--text-secondary)" }}>{tx.doctorName || "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div style={{ padding: "14px 16px", borderTop: "1px solid var(--border)", color: "var(--text-secondary)", fontSize: 12, background: "rgba(91,140,62,0.05)", display: "flex", justifyContent: "space-between" }}>
            <span>Показано: <strong style={{ color: "var(--accent)" }}>{filtered.length}</strong> / {data.length}</span>
            <span>Сумма: <strong style={{ color: "var(--accent)" }}>{formatAmount(filtered.reduce((s, tx) => s + (tx.totalSum || 0), 0))}</strong></span>
          </div>
        </div>
      )}

      {/* Detail modal */}
      {selectedTx && (
        <div className="modal-overlay" onClick={() => setSelectedTx(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 480 }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 20 }}>
              <h3 className="modal-title">🧾 Чек #{selectedTx.id}</h3>
              <button onClick={() => setSelectedTx(null)} style={{ background: "none", border: "none", color: "var(--text-secondary)", fontSize: 22, cursor: "pointer" }}>×</button>
            </div>
            <div style={{ fontSize: 13, lineHeight: 2 }}>
              <div><strong>Дата:</strong> {selectedTx.filledAt ? new Date(selectedTx.filledAt).toLocaleString("ru-RU") : "—"}</div>
              <div><strong>Пациент:</strong> {selectedTx.patient?.fullName || `ID: ${selectedTx.patientId}`}</div>
              <div><strong>Диагноз:</strong> {selectedTx.diagnosis || "—"}</div>
              <div><strong>Лечение:</strong> {selectedTx.treatmentDescription || "—"}</div>
              <div><strong>Врач:</strong> {selectedTx.doctorName || "—"}</div>
              <div><strong>Сумма:</strong> <span style={{ color: "var(--accent)", fontWeight: 700, fontSize: 16 }}>{formatAmount(selectedTx.totalSum)}</span></div>
              <div><strong>Способ оплаты:</strong> {selectedTx.paymentMethod || "—"}</div>
              <div><strong>Статус:</strong> {getStatusBadge(selectedTx.paymentStatus)}</div>
              {selectedTx.paymentMethod === "cash" && <div style={{ marginTop: 12, padding: 12, background: "rgba(91,140,62,0.08)", borderRadius: 10, textAlign: "center", fontSize: 24 }}>🧾</div>}
            </div>
            <button onClick={() => setSelectedTx(null)} className="btn btn-primary" style={{ width: "100%", marginTop: 20, justifyContent: "center" }}>Закрыть</button>
          </div>
        </div>
      )}
    </div>
  );
}
