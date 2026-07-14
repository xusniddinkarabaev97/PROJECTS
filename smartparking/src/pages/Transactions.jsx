import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const STATUS_MAP = {
  Completed: { cls: "badge-success", label: "Completed" },
  Paid: { cls: "badge-success", label: "Paid" },
  New: { cls: "badge-warning", label: "New" },
  Pending: { cls: "badge-warning", label: "Pending" },
  Failed: { cls: "badge-danger", label: "Failed" },
  Cancelled: { cls: "badge-danger", label: "Cancelled" },
};

export default function Transactions() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 15;

  const fetchData = useCallback(async () => {
    setLoading(true); setError(null);
    try { const r = await api.getTransactions(); setData(Array.isArray(r) ? r : []); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const filtered = search ? data.filter((tx) =>
    String(tx.id).includes(search) ||
    tx.client?.fullName?.toLowerCase().includes(search.toLowerCase()) ||
    tx.paymentStatus?.toLowerCase().includes(search.toLowerCase())
  ) : data;

  const paged = filtered.slice((page - 1) * pageSize, page * pageSize);
  const totalPages = Math.ceil(filtered.length / pageSize);

  const fmt = (v) => v != null ? Number(v).toLocaleString() + " UZS" : "—";
  const fmtDate = (d) => d ? new Date(d).toLocaleString("ru-RU", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" }) : "—";

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
          <p style={{ fontSize: 13, color: "var(--text-muted)", margin: "4px 0 0 0" }}>{t("total")}: {filtered.length}</p>
        </div>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <input className="input" style={{ width: 220, padding: "8px 12px" }} placeholder={"🔍 " + t("search")} value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <button className="btn btn-ghost btn-sm" onClick={fetchData}>🔄</button>
        </div>
      </div>

      {error && (
        <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: 12, borderRadius: 8, marginBottom: 16, display: "flex", justifyContent: "space-between" }}>
          <span>{error}</span>
          <button onClick={() => setError(null)} style={{ color: "var(--danger)", fontWeight: 700, background: "none", border: "none", cursor: "pointer" }}>×</button>
        </div>
      )}

      {data.length === 0 ? (
        <div className="empty-state"><div className="empty-state-icon">💳</div><p>{t("noTransactions")}</p></div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th style={{ width: 80 }}>ID</th>
                  <th>{t("name") || "Mijoz"}</th>
                  <th style={{ width: 130 }}>{t("amount")}</th>
                  <th style={{ width: 120 }}>{t("paymentStatus")}</th>
                  <th style={{ width: 100 }}>{t("type")}</th>
                  <th style={{ width: 160 }}>{t("date")}</th>
                </tr>
              </thead>
              <tbody>
                {paged.map((tx) => {
                  const s = STATUS_MAP[tx.paymentStatus] || STATUS_MAP.New;
                  return (
                    <tr key={tx.id}>
                      <td style={{ color: "var(--text-muted)", fontSize: 12, fontFamily: "monospace" }}>#{tx.id}</td>
                      <td style={{ fontWeight: 600 }}>
                        {tx.status === "parking" ? (
                          <span>🚗 {(() => { try { const p = JSON.parse(tx.paymentMethod); return p.AvtoRaqam || p.avtoRaqam || "—"; } catch { return "—"; } })()}</span>
                        ) : tx.client?.fullName || `#${tx.clientId || "—"}`}
                      </td>
                      <td style={{ fontWeight: 600, whiteSpace: "nowrap" }}>{fmt(tx.totalSum)}</td>
                      <td><span className={`badge ${s.cls}`}>{s.label}</span></td>
                      <td>
                        <span className="badge badge-accent" style={{ background: "rgba(31,111,235,0.12)", color: "#58a6ff" }}>
                          {tx.status || "—"}
                        </span>
                      </td>
                      <td style={{ color: "var(--text-secondary)", fontSize: 12, whiteSpace: "nowrap" }}>{fmtDate(tx.filledAt)}</td>
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
          <div style={{ padding: "12px 16px", borderTop: "1px solid var(--border)", color: "var(--text-muted)", fontSize: 12 }}>
            {t("total")}: {filtered.length} · {t("amount")}: {fmt(filtered.reduce((s, tx) => s + (tx.totalSum || 0), 0))}
          </div>
        </div>
      )}
    </div>
  );
}
