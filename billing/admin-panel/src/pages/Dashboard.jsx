import { useState, useEffect } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function Dashboard() {
  const { t } = useTranslation();
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    api
      .getDashboard()
      .then(setStats)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          padding: "60px 0",
          gap: 12,
        }}
      >
        <div className="spinner" />
        <span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon">⚠️</div>
        <p style={{ color: "var(--danger)", fontWeight: 500 }}>{t("error")}</p>
        <p style={{ marginTop: 8 }}>{error}</p>
      </div>
    );
  }

  const cards = [
    {
      label: t("totalStations"),
      value: stats?.totalFillingStations ?? 0,
      icon: "⛽",
      color: "#1f6feb",
    },
    {
      label: t("totalStakeholders"),
      value: stats?.totalStakeholders ?? 0,
      icon: "👥",
      color: "#238636",
    },
    {
      label: t("totalTransactions"),
      value: stats?.totalTransactions ?? 0,
      icon: "💳",
      color: "#a371f7",
    },
    {
      label: t("todayAmount"),
      value: `${(stats?.todayTotalAmount ?? 0).toLocaleString()} UZS`,
      icon: "💰",
      color: "#d2991d",
    },
  ];

  return (
    <div>
      <h2
        style={{
          fontSize: 24,
          fontWeight: 700,
          marginBottom: 24,
          color: "var(--text-primary)",
        }}
      >
        📊 {t("dashboard")}
      </h2>
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
          gap: 16,
        }}
      >
        {cards.map((card) => (
          <div key={card.label} className="stat-card">
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "flex-start",
              }}
            >
              <span style={{ fontSize: 24 }}>{card.icon}</span>
              <div
                style={{
                  width: 8,
                  height: 8,
                  borderRadius: "50%",
                  background: card.color,
                }}
              />
            </div>
            <div className="stat-value" style={{ marginTop: 12 }}>
              {card.value}
            </div>
            <div className="stat-label">{card.label}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
