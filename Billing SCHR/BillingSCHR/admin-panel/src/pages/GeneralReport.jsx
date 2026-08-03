import { useState, useEffect } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function GeneralReport() {
  const { t } = useTranslation();
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    api.getDashboard()
      .then(setStats)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <Spinner label={t("loading")} />;
  }

  if (error) {
    return <ErrorBox message={error} />;
  }

  const cards = [
    { label: t("totalDepartments"), value: stats?.totalDepartments ?? 0, icon: "🏥", color: "#1f6feb" },
    { label: t("totalCompanies"), value: stats?.totalCompanies ?? 0, icon: "🏢", color: "#7c3aed" },
    { label: t("totalPatients"), value: stats?.totalPatients ?? 0, icon: "👤", color: "#238636" },
    { label: t("totalTransactions"), value: stats?.totalTransactions ?? 0, icon: "💳", color: "#a371f7" },
    { label: t("todayTransactions"), value: stats?.todayTransactions ?? 0, icon: "📅", color: "#d2991d" },
    { label: t("todayRevenue"), value: `${(stats?.todayRevenue ?? 0).toLocaleString()} UZS`, icon: "💰", color: "#238636" },
  ];

  return (
    <div>
      {/* Заголовок */}
      <div style={{ marginBottom: 32 }}>
        <h2 style={{ fontSize: 26, fontWeight: 800, color: "var(--text-primary)", margin: 0 }}>
          📊 {t("generalReport")}
        </h2>
        <p style={{ color: "var(--text-secondary)", fontSize: 13, marginTop: 6 }}>
          {t("generalReportDesc")}
        </p>
      </div>

      {/* Карточки статистики */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))", gap: 16, marginBottom: 32 }}>
        {cards.map((card) => (
          <div key={card.label} className="stat-card">
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
              <span style={{ fontSize: 24 }}>{card.icon}</span>
              <div style={{ width: 8, height: 8, borderRadius: "50%", background: card.color }} />
            </div>
            <div className="stat-value" style={{ marginTop: 12 }}>{card.value}</div>
            <div className="stat-label">{card.label}</div>
          </div>
        ))}
      </div>

      {/* Блоки с деталями */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(380px, 1fr))", gap: 20 }}>
        <TransactionsBlock />
        <DepartmentsBlock />
        <PlansBlock />
      </div>
    </div>
  );
}

/* ─── Блок: Последние транзакции ─── */
function TransactionsBlock() {
  const [tx, setTx] = useState([]);
  useEffect(() => {
    api.getTransactions().then(r => setTx(Array.isArray(r) ? r.slice(-8).reverse() : [])).catch(() => {});
  }, []);

  const formatAmt = (v) => (v != null ? Number(v).toLocaleString() + " UZS" : "—");
  const formatDt = (d) => (d ? new Date(d).toLocaleString("ru-RU", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit" }) : "—");

  return (
    <div className="card">
      <h3 style={{ fontSize: 15, fontWeight: 700, color: "var(--text-primary)", marginBottom: 16 }}>💳 Последние транзакции</h3>
      {tx.length === 0 ? (
        <p style={{ color: "var(--text-muted)", fontSize: 13 }}>Нет данных</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
          <thead>
            <tr style={{ borderBottom: "1px solid var(--border)", color: "var(--text-muted)", textAlign: "left" }}>
              <th style={{ padding: "6px 4px" }}>Пациент</th>
              <th style={{ padding: "6px 4px" }}>Сумма</th>
              <th style={{ padding: "6px 4px" }}>Статус</th>
              <th style={{ padding: "6px 4px" }}>Дата</th>
            </tr>
          </thead>
          <tbody>
            {tx.map(t => (
              <tr key={t.id} style={{ borderBottom: "1px solid var(--border)" }}>
                <td style={{ padding: "6px 4px", fontWeight: 500 }}>{t.patient?.fullName || `#${t.patientId}`}</td>
                <td style={{ padding: "6px 4px", fontWeight: 600 }}>{formatAmt(t.totalSum)}</td>
                <td style={{ padding: "6px 4px" }}>
                  <span className={`badge ${t.paymentStatus === "Completed" ? "badge-success" : t.paymentStatus === "Pending" ? "badge-warning" : "badge-info"}`}>
                    {t.paymentStatus || "—"}
                  </span>
                </td>
                <td style={{ padding: "6px 4px", color: "var(--text-muted)" }}>{formatDt(t.filledAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

/* ─── Блок: Отделения ─── */
function DepartmentsBlock() {
  const [depts, setDepts] = useState([]);
  useEffect(() => {
    api.getDepartments().then(r => setDepts(Array.isArray(r) ? r : [])).catch(() => {});
  }, []);

  return (
    <div className="card">
      <h3 style={{ fontSize: 15, fontWeight: 700, color: "var(--text-primary)", marginBottom: 16 }}>🏥 Отделения госпиталя</h3>
      {depts.length === 0 ? (
        <p style={{ color: "var(--text-muted)", fontSize: 13 }}>Нет данных</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
          <thead>
            <tr style={{ borderBottom: "1px solid var(--border)", color: "var(--text-muted)", textAlign: "left" }}>
              <th style={{ padding: "6px 4px" }}>Название</th>
              <th style={{ padding: "6px 4px" }}>Заведующий</th>
              <th style={{ padding: "6px 4px" }}>Коек</th>
            </tr>
          </thead>
          <tbody>
            {depts.map(d => (
              <tr key={d.id} style={{ borderBottom: "1px solid var(--border)" }}>
                <td style={{ padding: "6px 4px", fontWeight: 500 }}>{d.name}</td>
                <td style={{ padding: "6px 4px" }}>{d.headDoctor || "—"}</td>
                <td style={{ padding: "6px 4px" }}>{d.bedCount ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

/* ─── Блок: Тарифы ─── */
function PlansBlock() {
  const [plans, setPlans] = useState([]);
  useEffect(() => {
    api.getPlans().then(r => setPlans(Array.isArray(r) ? r : [])).catch(() => {});
  }, []);

  const formatAmt = (v) => (v != null ? Number(v).toLocaleString() + " UZS" : "—");

  return (
    <div className="card">
      <h3 style={{ fontSize: 15, fontWeight: 700, color: "var(--text-primary)", marginBottom: 16 }}>📋 Тарифные планы</h3>
      {plans.length === 0 ? (
        <p style={{ color: "var(--text-muted)", fontSize: 13 }}>Нет данных</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 12 }}>
          <thead>
            <tr style={{ borderBottom: "1px solid var(--border)", color: "var(--text-muted)", textAlign: "left" }}>
              <th style={{ padding: "6px 4px" }}>Услуга</th>
              <th style={{ padding: "6px 4px" }}>Категория</th>
              <th style={{ padding: "6px 4px" }}>Цена</th>
            </tr>
          </thead>
          <tbody>
            {plans.map(p => (
              <tr key={p.id} style={{ borderBottom: "1px solid var(--border)" }}>
                <td style={{ padding: "6px 4px", fontWeight: 500 }}>{p.name}</td>
                <td style={{ padding: "6px 4px" }}>{p.category || "—"}</td>
                <td style={{ padding: "6px 4px", fontWeight: 600 }}>{formatAmt(p.basePrice)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

/* ─── Shared ─── */
function Spinner({ label }) {
  return (
    <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}>
      <div className="spinner" />
      <span style={{ color: "var(--text-secondary)" }}>{label}</span>
    </div>
  );
}

function ErrorBox({ message }) {
  return (
    <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: "16px", borderRadius: 8 }}>
      <strong>⚠️ Ошибка</strong>
      <p style={{ marginTop: 8 }}>{message}</p>
    </div>
  );
}
