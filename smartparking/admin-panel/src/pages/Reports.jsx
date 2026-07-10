import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function Reports() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [stations, setStations] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState("all"); // all, today, week, month
  const [stationFilter, setStationFilter] = useState("all");

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const [txs, sts] = await Promise.all([
        api.getTransactions().catch(() => []),
        api.getStations().catch(() => []),
      ]);
      setData(Array.isArray(txs) ? txs : []);
      setStations(Array.isArray(sts) ? sts : []);
    } catch {
      setData([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Filtering
  const now = new Date();
  const filtered = data.filter((tx) => {
    const d = tx.filledAt ? new Date(tx.filledAt) : null;
    if (!d) return false;

    if (filter === "today") {
      return d.toDateString() === now.toDateString();
    }
    if (filter === "week") {
      const weekAgo = new Date(now);
      weekAgo.setDate(weekAgo.getDate() - 7);
      return d >= weekAgo;
    }
    if (filter === "month") {
      const monthAgo = new Date(now);
      monthAgo.setMonth(monthAgo.getMonth() - 1);
      return d >= monthAgo;
    }
    return true; // all
  });

  const stationFiltered =
    stationFilter === "all"
      ? filtered
      : filtered.filter((tx) => tx.stationId === parseInt(stationFilter));

  // Stats
  const totalRevenue = stationFiltered.reduce((s, tx) => s + (tx.totalSum || 0), 0);
  const completedCount = stationFiltered.filter(
    (tx) => tx.paymentStatus === "Completed" || tx.paymentStatus === "Paid",
  ).length;
  const pendingCount = stationFiltered.filter(
    (tx) => tx.paymentStatus === "New" || tx.paymentStatus === "Pending",
  ).length;
  const failedCount = stationFiltered.filter(
    (tx) => tx.paymentStatus === "Failed" || tx.paymentStatus === "Cancelled",
  ).length;

  // Revenue by station
  const revenueByStation = {};
  stationFiltered.forEach((tx) => {
    const key = tx.stationId || 0;
    if (!revenueByStation[key]) revenueByStation[key] = { count: 0, sum: 0 };
    revenueByStation[key].count++;
    revenueByStation[key].sum += tx.totalSum || 0;
  });

  // Daily revenue for chart (last 14 days)
  const dailyRevenue = [];
  for (let i = 13; i >= 0; i--) {
    const d = new Date(now);
    d.setDate(d.getDate() - i);
    const dateStr = d.toISOString().slice(0, 10);
    const dayTxs = stationFiltered.filter(
      (tx) => tx.filledAt?.startsWith?.(dateStr),
    );
    dailyRevenue.push({
      date: d.toLocaleDateString("ru-RU", { day: "numeric", month: "short" }),
      sum: dayTxs.reduce((s, tx) => s + (tx.totalSum || 0), 0),
      count: dayTxs.length,
    });
  }

  const maxDaily = Math.max(...dailyRevenue.map((d) => d.sum), 1);

  const formatAmount = (v) => (v != null ? Number(v).toLocaleString() : "0");

  if (loading) {
    return (
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
    );
  }

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>
          📈 {t("reports")}
        </h2>
        <div style={{ display: "flex", gap: 8 }}>
          <select
            className="input"
            style={{ width: 140, padding: "8px 12px" }}
            value={stationFilter}
            onChange={(e) => setStationFilter(e.target.value)}
          >
            <option value="all">{t("allStations")}</option>
            {stations.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
          <button className="btn btn-ghost btn-sm" onClick={fetchData}>
            🔄
          </button>
        </div>
      </div>

      {/* Time filter tabs */}
      <div className="tab-bar">
        {["all", "today", "week", "month"].map((f) => (
          <button
            key={f}
            className={`tab ${filter === f ? "active" : ""}`}
            onClick={() => setFilter(f)}
          >
            {t(f)}
          </button>
        ))}
      </div>

      {/* Summary cards */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))",
          gap: 12,
          marginBottom: 20,
        }}
      >
        <div className="stat-card">
          <div style={{ fontSize: 24 }}>💰</div>
          <div className="stat-value" style={{ fontSize: 22 }}>
            {formatAmount(totalRevenue)} UZS
          </div>
          <div className="stat-label">{t("totalRevenue")}</div>
        </div>
        <div className="stat-card">
          <div style={{ fontSize: 24 }}>📊</div>
          <div className="stat-value">{stationFiltered.length}</div>
          <div className="stat-label">{t("transactions")}</div>
        </div>
        <div className="stat-card">
          <div style={{ fontSize: 24 }}>✅</div>
          <div className="stat-value" style={{ color: "var(--success)" }}>
            {completedCount}
          </div>
          <div className="stat-label">{t("completed")}</div>
        </div>
        <div className="stat-card">
          <div style={{ fontSize: 24 }}>❌</div>
          <div className="stat-value" style={{ color: "var(--danger)" }}>
            {failedCount}
          </div>
          <div className="stat-label">{t("failed")}</div>
        </div>
      </div>

      {/* Bar chart - daily revenue */}
      <div className="card" style={{ marginBottom: 20 }}>
        <h3 style={{ marginBottom: 16, fontWeight: 600 }}>
          📅 {t("dailyRevenue")} (14 {t("days")})
        </h3>
        <div style={{ display: "flex", alignItems: "flex-end", gap: 4, height: 180, paddingTop: 8 }}>
          {dailyRevenue.map((d) => (
            <div
              key={d.date}
              style={{
                flex: 1,
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                height: "100%",
                justifyContent: "flex-end",
              }}
            >
              <div
                style={{
                  width: "100%",
                  maxWidth: 36,
                  height: `${(d.sum / maxDaily) * 140}px`,
                  minHeight: d.sum > 0 ? 4 : 0,
                  background: "var(--accent)",
                  borderRadius: "4px 4px 0 0",
                  transition: "height 0.3s",
                  cursor: "pointer",
                }}
                title={`${formatAmount(d.sum)} UZS (${d.count} tx)`}
              />
              <div
                style={{
                  fontSize: 10,
                  color: "var(--text-muted)",
                  marginTop: 4,
                  transform: "rotate(-40deg)",
                  transformOrigin: "top left",
                  whiteSpace: "nowrap",
                }}
              >
                {d.date}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Revenue by station */}
      {Object.keys(revenueByStation).length > 0 && (
        <div className="card">
          <h3 style={{ marginBottom: 16, fontWeight: 600 }}>
            🅿️ {t("revenueByStation")}
          </h3>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>{t("station")}</th>
                  <th>{t("transactions")}</th>
                  <th>{t("revenue")}</th>
                </tr>
              </thead>
              <tbody>
                {Object.entries(revenueByStation)
                  .sort((a, b) => b[1].sum - a[1].sum)
                  .map(([id, info]) => {
                    const st = stations.find((s) => s.id === parseInt(id));
                    return (
                      <tr key={id}>
                        <td style={{ fontWeight: 600 }}>
                          {st ? st.name : id === "0" ? t("noStation") : `#${id}`}
                        </td>
                        <td>
                          <span className="badge badge-info">{info.count}</span>
                        </td>
                        <td style={{ fontWeight: 600, color: "var(--success)" }}>
                          {formatAmount(info.sum)} UZS
                        </td>
                      </tr>
                    );
                  })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
