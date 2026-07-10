import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const STATUS_BADGE = {
  Completed: "badge-success",
  Paid: "badge-success",
  New: "badge-warning",
  Pending: "badge-warning",
  Failed: "badge-danger",
  Cancelled: "badge-danger",
  Refunded: "badge-info",
  Expired: "badge-danger",
};

function parseParking(paymentMethod) {
  try {
    return JSON.parse(paymentMethod);
  } catch {
    return null;
  }
}

function ParkingDetail({ p }) {
  if (!p) return <span>—</span>;
  const avto = p.AvtoRaqam || p.avtoRaqam || "—";
  const k = (p.Kirish || p.kirish || "").slice(11, 16) || "—";
  const c = (p.Chiqish || p.chiqish || "").slice(11, 16) || "—";
  const d = p.Davomiyligi || p.davomiyligi || "—";
  return (
    <div style={{ fontSize: 11, lineHeight: 1.5 }}>
      <div>
        🚗 <b>{avto}</b>
      </div>
      <div style={{ color: "var(--text-muted)" }}>
        🅿️ {k} → {c}
      </div>
      <div style={{ color: "var(--text-muted)" }}>⏱ {d}</div>
    </div>
  );
}

export default function Transactions() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState("");

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await api.getTransactions();
      setData(Array.isArray(r) ? r : []);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const filtered = search
    ? data.filter(
        (tx) =>
          String(tx.id).includes(search) ||
          tx.client?.fullName?.toLowerCase().includes(search.toLowerCase()) ||
          tx.status?.toLowerCase().includes(search.toLowerCase()) ||
          tx.paymentMethod?.toLowerCase().includes(search.toLowerCase()),
      )
    : data;

  const formatAmount = (v) =>
    v != null ? Number(v).toLocaleString() + " UZS" : "—";
  const formatDate = (d) => (d ? new Date(d).toLocaleString() : "—");

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
          💳 {t("transactions")}
        </h2>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <input
            className="input"
            style={{ width: 240, padding: "8px 12px" }}
            placeholder={"🔍 " + t("search") + "..."}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
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
          <div className="empty-state-icon">💳</div>
          <p>{t("noTransactions")}</p>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>{t("name") || "Mijoz"}</th>
                  <th>{t("amount")}</th>
                  <th>{t("paymentStatus")}</th>
                  <th>{t("type")}</th>
                  <th>{t("date")}</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((tx) => {
                  const isParking = tx.status === "parking";
                  const p = isParking ? parseParking(tx.paymentMethod) : null;
                  const badge = STATUS_BADGE[tx.paymentStatus] || "badge-info";
                  return (
                    <tr key={tx.id}>
                      <td style={{ color: "var(--text-muted)", fontSize: 12 }}>
                        #{tx.id}
                      </td>
                      <td>
                        {isParking && p ? (
                          <ParkingDetail p={p} />
                        ) : (
                          <span style={{ fontWeight: 600 }}>
                            {tx.client?.fullName || `#${tx.clientId}` || "—"}
                          </span>
                        )}
                      </td>
                      <td style={{ fontWeight: 600 }}>
                        {formatAmount(tx.totalSum)}
                      </td>
                      <td>
                        <span className={`badge ${badge}`}>
                          {tx.paymentStatus || "—"}
                        </span>
                      </td>
                      <td>
                        <span
                          className={`badge ${isParking ? "badge-accent" : "badge-info"}`}
                        >
                          {tx.status || "—"}
                        </span>
                      </td>
                      <td
                        style={{ color: "var(--text-secondary)", fontSize: 12 }}
                      >
                        {formatDate(tx.filledAt)}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <div
            style={{
              padding: "12px 16px",
              borderTop: "1px solid var(--border)",
              color: "var(--text-secondary)",
              fontSize: 12,
            }}
          >
            {t("total")}: {filtered.length} / {data.length}
            {" · "}
            {t("amount")}:{" "}
            {formatAmount(
              filtered.reduce((s, tx) => s + (tx.totalSum || 0), 0),
            )}
          </div>
        </div>
      )}
    </div>
  );
}
