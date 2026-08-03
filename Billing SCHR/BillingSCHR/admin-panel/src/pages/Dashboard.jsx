import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const card = (icon, value, label, color) => (
  <div key={label} style={{ background: "var(--bg-card)", backdropFilter: "blur(8px)", border: "1px solid var(--border)", borderRadius: 16, padding: "20px 24px", boxShadow: "var(--green-glow)", transition: "all 0.25s" }}>
    <div style={{ fontSize: 24, marginBottom: 8 }}>{icon}</div>
    <div style={{ fontSize: 28, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: color || "var(--accent)", letterSpacing: "-0.5px" }}>{value}</div>
    <div style={{ fontSize: 12, color: "var(--text-secondary)", marginTop: 4 }}>{label}</div>
  </div>
);

const pctBar = (label, pct, amount, color) => (
  <div key={label} style={{ padding: "10px 0" }}>
    <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 6, fontSize: 13 }}>
      <span style={{ color: "var(--text-primary)" }}>{label}</span>
      <span style={{ color: "var(--text-secondary)", fontSize: 12 }}>
        {amount?.toLocaleString?.() || "0"} UZS <strong style={{ color, marginLeft: 8 }}>{pct}%</strong>
      </span>
    </div>
    <div style={{ height: 6, background: "var(--bg-hover)", borderRadius: 3, overflow: "hidden" }}>
      <div style={{ height: "100%", width: `${pct}%`, background: color, borderRadius: 3, transition: "width 0.6s" }} />
    </div>
  </div>
);

export default function Dashboard() {
  const { t } = useTranslation();
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);

  const fetchStats = useCallback(async () => {
    setLoading(true);
    try {
      const s = await api.getDashboardStats();
      setStats(s);
    } catch { setStats(null); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchStats(); }, [fetchStats]);

  if (loading) return (
    <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 80, gap: 12 }}>
      <div className="spinner" /><span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
    </div>
  );

  const s = stats || {};
  const totalRevenue = (s.todayRevenue || 0) + (s.weekRevenue || 0) + (s.monthRevenue || 0);
  const payments = s.paymentDistribution || {};
  const totalPayAmount = Object.values(payments).reduce((sum, v) => sum + (v?.amount || 0), 0) || 1;
  const departments = s.departmentRevenue || [];
  const connections = s.connectionStatus || {};

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
        <h2 style={{ fontSize: 24, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#fff" }}>📊 {t("dashboard")}</h2>
        <button onClick={fetchStats} className="btn btn-ghost btn-sm" style={{ fontSize: 16 }}>🔄</button>
      </div>

      {/* Revenue widgets */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: 16, marginBottom: 24 }}>
        {card("📅", (s.todayRevenue || 0).toLocaleString() + " UZS", "Оборот за день", "#5b8c3e")}
        {card("📆", (s.weekRevenue || 0).toLocaleString() + " UZS", "Оборот за неделю", "#60a5fa")}
        {card("🗓️", (s.monthRevenue || 0).toLocaleString() + " UZS", "Оборот за месяц", "#3a8c5c")}
        {card("👥", s.totalPatients || 0, t("totalPatients"), "#8b6914")}
        {card("💳", s.totalTransactions || 0, t("totalTransactions"), "#c4952d")}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(360px, 1fr))", gap: 24 }}>
        {/* Payment distribution */}
        <div className="card">
          <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>
            💳 {t("paymentDistribution")}
          </h3>
          {[
            { key: "cash", label: "Наличные", color: "#3a8c5c" },
            { key: "uzcard", label: "Uzcard", color: "#3b82f6" },
            { key: "humo", label: "Humo", color: "#5b8c3e" },
            { key: "payme", label: "Payme", color: "#8b6914" },
            { key: "click", label: "Click", color: "#06b6d4" },
            { key: "transfer", label: "Перечисление", color: "#c4952d" },
          ].map(({ key, label, color }) => {
            const amt = payments[key]?.amount || 0;
            const pct = totalPayAmount > 0 ? Math.round((amt / totalPayAmount) * 100) : 0;
            return pctBar(label, pct, amt, color);
          })}
        </div>

        {/* Department revenue */}
        <div className="card">
          <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>
            🏥 Выручка по отделениям
          </h3>
          {departments.length === 0 ? (
            <div className="empty-state" style={{ padding: 24 }}>
              <div className="empty-state-icon">🏥</div><p>{t("noDepartments")}</p>
            </div>
          ) : (
            departments.map((d, i) => {
              const pct = totalRevenue > 0 ? Math.round(((d.amount || 0) / totalRevenue) * 100) : 0;
              const colors = ["#5b8c3e", "#3b82f6", "#3a8c5c", "#8b6914", "#c4952d", "#06b6d4"];
              return pctBar(d.departmentName || `Отделение #${d.departmentId}`, pct, d.amount || 0, colors[i % colors.length]);
            })
          )}
        </div>
      </div>

      {/* Connection status */}
      <div className="card" style={{ marginTop: 24 }}>
        <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>
          🔌 Статус подключений
        </h3>
        <div style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
          {[
            { key: "emis", label: "EMIS / МИС" },
            { key: "click", label: "Click" },
            { key: "uzcard", label: "Uzcard" },
            { key: "humo", label: "Humo" },
            { key: "ofd", label: "ОФД РУз" },
          ].map(({ key, label }) => (
            <div key={key} style={{
              display: "flex", alignItems: "center", gap: 8,
              padding: "10px 16px", borderRadius: 10,
              background: connections[key] ? "rgba(58,140,92,0.08)" : "rgba(248,113,113,0.08)",
              border: `1px solid ${connections[key] ? "rgba(58,140,92,0.2)" : "rgba(248,113,113,0.2)"}`,
            }}>
              <span>{connections[key] ? "🟢" : "🔴"}</span>
              <span style={{ fontSize: 13, fontWeight: 500, color: connections[key] ? "#3a8c5c" : "#f87171" }}>{label}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
