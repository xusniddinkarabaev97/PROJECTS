import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const STATUS_MAP = {
  Created: { label: "Created", className: "badge-info" },
  Processing: { label: "Processing", className: "badge-warning" },
  Completed: { label: "Completed", className: "badge-success" },
  Failed: { label: "Failed", className: "badge-danger" },
  Cancelled: { label: "Cancelled", className: "badge-danger" },
  Refunded: { label: "Refunded", className: "badge-warning" },
};

export default function Transactions() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState("");

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.getTransactions({ page, search });
      setData(res.items || res.data || res || []);
      setTotalPages(res.totalPages || 1);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [page, search]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const formatDate = (d) => {
    if (!d) return "—";
    try {
      return new Date(d).toLocaleString();
    } catch {
      return String(d);
    }
  };

  const formatAmount = (v) => {
    if (v == null) return "—";
    return Number(v).toLocaleString() + " UZS";
  };

  return (
    <div>
      <div className="card-header">
        <h2
          style={{
            fontSize: 24,
            fontWeight: 700,
            color: "var(--text-primary)",
          }}
        >
          📋 {t("transactions")}
        </h2>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <input
            className="input"
            style={{ width: 240, padding: "8px 12px" }}
            placeholder="🔍 Search..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
          <button className="btn btn-ghost btn-sm" onClick={fetchData}>
            🔄
          </button>
        </div>
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
          <div className="empty-state-icon">📭</div>
          <p>{t("noTransactions") || "No transactions found"}</p>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>{t("transactionId") || "Transaction ID"}</th>
                  <th>{t("station") || "Station"}</th>
                  <th>{t("amount")}</th>
                  <th>{t("paymentSystem") || "System"}</th>
                  <th>{t("status")}</th>
                  <th>{t("date") || "Date"}</th>
                </tr>
              </thead>
              <tbody>
                {data.map((tx) => {
                  const st = STATUS_MAP[tx.status] || {
                    label: tx.status,
                    className: "badge-info",
                  };
                  return (
                    <tr key={tx.id || tx.transactionId}>
                      <td style={{ color: "var(--text-muted)", fontSize: 12 }}>
                        #{tx.id}
                      </td>
                      <td style={{ fontFamily: "monospace", fontSize: 12 }}>
                        {tx.transactionId?.slice(0, 20) || "—"}
                      </td>
                      <td>{tx.stationName || tx.contragentId || "—"}</td>
                      <td style={{ fontWeight: 600 }}>
                        {formatAmount(tx.amount)}
                      </td>
                      <td>{tx.paymentSystem || "—"}</td>
                      <td>
                        <span className={`badge ${st.className}`}>
                          {st.label}
                        </span>
                      </td>
                      <td
                        style={{ color: "var(--text-secondary)", fontSize: 12 }}
                      >
                        {formatDate(tx.createdAt)}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          {totalPages > 1 && (
            <div
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                gap: 8,
                padding: "16px 24px",
                borderTop: "1px solid var(--border)",
              }}
            >
              <button
                className="btn btn-ghost btn-sm"
                disabled={page <= 1}
                onClick={() => setPage(page - 1)}
              >
                ◀
              </button>
              <span style={{ color: "var(--text-secondary)", fontSize: 13 }}>
                {page} / {totalPages}
              </span>
              <button
                className="btn btn-ghost btn-sm"
                disabled={page >= totalPages}
                onClick={() => setPage(page + 1)}
              >
                ▶
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
