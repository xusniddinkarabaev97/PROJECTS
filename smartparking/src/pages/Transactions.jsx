import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

function parsePaymentMethod(pm) {
  try {
    const p = JSON.parse(pm);
    // avto.itpanda.uz format
    if (p.AvtoRaqam || p.avtoRaqam) return { entry: p.Kirish || p.kirish, exit: p.Chiqish || p.chiqish };
    // UParking format
    if (p.parkingStart) return { entry: p.parkingStart, exit: p.parkingEnd };
  } catch {}
  return {};
}

function fmtDate(v) {
  if (!v) return "—";
  const d = new Date(v);
  return isNaN(d.getTime()) ? v : d.toLocaleString("ru-RU", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function fmtAmount(v) {
  return v != null ? Number(v).toLocaleString() + " UZS" : "—";
}

export default function Transactions() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [period, setPeriod] = useState("day");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const fetchData = useCallback(async () => {
    setLoading(true); setError(null);
    try { const r = await api.getTransactions(); setData(Array.isArray(r) ? r : []); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  // Filter by period
  const now = new Date();
  const from = period === "week" ? new Date(now - 7*86400000)
             : period === "month" ? new Date(now - 30*86400000)
             : new Date(now.setHours(0,0,0,0));

  const filtered = data.filter(tx => new Date(tx.filledAt) >= from)
    .sort((a, b) => new Date(b.filledAt) - new Date(a.filledAt));

  const paged = filtered.slice((page - 1) * pageSize, page * pageSize);
  const totalPages = Math.ceil(filtered.length / pageSize);

  const downloadExcel = async () => {
    try {
      const res = await fetch("/Billing/Transactions?handler=Export&period=" + period);
      if (!res.ok) throw new Error(res.status + " " + res.statusText);
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = "transactions_" + period + ".csv";
      a.click();
      URL.revokeObjectURL(url);
    } catch(e) {
      alert("Ошибка: " + e.message);
    }
  };

  if (loading) return (
    <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}>
      <div className="spinner" style={{ width: 24, height: 24 }}></div>
      <span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
    </div>
  );

  return (
    <div>
      <div className="card-header">
        <div>
          <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)", margin: 0 }}>💳 {t("transactions")}</h2>
        </div>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <div className="btn-group" style={{ display: "flex" }}>
            {["day","week","month"].map(p => (
              <button key={p} onClick={() => { setPeriod(p); setPage(1); }}
                style={{
                  padding: "6px 14px", fontSize: 13, fontWeight: 500, cursor: "pointer",
                  border: "1px solid var(--border)", background: period === p ? "var(--accent)" : "transparent",
                  color: period === p ? "#fff" : "var(--text-secondary)", borderRadius: 0
                }}>
                {{day:"📅 День",week:"📆 Нед",month:"🗓 Мес"}[p]}
              </button>
            ))}
          </div>
          <button className="btn btn-success btn-sm" onClick={downloadExcel}>📥 Excel</button>
          <button className="btn btn-ghost btn-sm" onClick={fetchData}>🔄</button>
        </div>
      </div>

      {error && (
        <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: 12, borderRadius: 8, marginBottom: 16 }}>
          {error}
        </div>
      )}

      {filtered.length === 0 ? (
        <div className="empty-state"><div className="empty-state-icon">💳</div><p>{t("noTransactions")}</p></div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th style={{ width: 80 }}>№</th>
                  <th>Въезд</th>
                  <th>Выезд</th>
                  <th style={{ width: 130 }}>Сумма</th>
                  <th style={{ width: 100 }}>Статус</th>
                </tr>
              </thead>
              <tbody>
                {paged.map((tx) => {
                  const times = parsePaymentMethod(tx.paymentMethod);
                  return (
                    <tr key={tx.id}>
                      <td style={{ color: "var(--text-muted)", fontSize: 12, fontFamily: "monospace" }}>#{tx.id}</td>
                      <td style={{ fontSize: 13, whiteSpace: "nowrap" }}>{fmtDate(times.entry)}</td>
                      <td style={{ fontSize: 13, whiteSpace: "nowrap" }}>{fmtDate(times.exit)}</td>
                      <td style={{ fontWeight: 600, whiteSpace: "nowrap" }}>{fmtAmount(tx.totalSum)}</td>
                      <td>
                        <span className={`badge ${tx.paymentStatus === "Completed" ? "badge-success" : tx.paymentStatus === "Failed" ? "badge-danger" : "badge-warning"}`}>
                          {tx.paymentStatus === "Completed" ? "✅" : tx.paymentStatus === "Failed" ? "❌" : "⏳"}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {totalPages > 1 && (
            <div style={{ display: "flex", justifyContent: "center", gap: 4, padding: 12, borderTop: "1px solid var(--border)" }}>
              {Array.from({ length: totalPages }, (_, i) => (
                <button key={i} onClick={() => setPage(i + 1)}
                  style={{ padding: "6px 14px", borderRadius: 6, border: i + 1 === page ? "1px solid var(--accent)" : "1px solid var(--border)", background: i + 1 === page ? "var(--accent)" : "transparent", color: i + 1 === page ? "#fff" : "var(--text-secondary)", cursor: "pointer", fontSize: 13 }}>
                  {i + 1}
                </button>
              ))}
            </div>
          )}

          <div style={{ padding: "12px 16px", borderTop: "1px solid var(--border)", color: "var(--text-muted)", fontSize: 12, display: "flex", justifyContent: "space-between" }}>
            <span>{t("total")}: {filtered.length}</span>
            <span>Сумма: {fmtAmount(filtered.reduce((s, tx) => s + (tx.totalSum || 0), 0))}</span>
          </div>
        </div>
      )}
    </div>
  );
}
