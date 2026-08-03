import { useTranslation } from "../i18n/LanguageContext";

export default function Fiscal() {
  const { t } = useTranslation();
  const statCard = (icon, label, value, color) => (
    <div style={{ background: "var(--bg-card)", backdropFilter: "blur(8px)", border: "1px solid var(--border)", borderRadius: 16, padding: "20px 24px", boxShadow: "var(--green-glow)" }}>
      <div style={{ fontSize: 24, marginBottom: 8 }}>{icon}</div>
      <div style={{ fontSize: 28, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color }}>{value}</div>
      <div style={{ fontSize: 12, color: "var(--text-secondary)", marginTop: 4 }}>{label}</div>
    </div>
  );

  return (
    <div>
      <h2 style={{ fontSize: 24, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#fff", marginBottom: 24 }}>🧾 {t("fiscal")}</h2>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(200px, 1fr))", gap: 16, marginBottom: 24 }}>
        {statCard("✅", "Отправлено в ОФД", "0", "#3a8c5c")}
        {statCard("⏳", "В очереди", "0", "#5b8c3e")}
        {statCard("❌", "Ошибки отправки", "0", "#f87171")}
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(380px, 1fr))", gap: 24 }}>
        {/* Z-reports */}
        <div className="card">
          <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>🖨️ Z-отчёты</h3>
          <div style={{ display: "flex", gap: 8, marginBottom: 16 }}>
            <button className="btn btn-primary btn-sm">➕ Закрыть смену</button>
            <button className="btn btn-ghost btn-sm">📥 Экспорт</button>
          </div>
          <table className="data-table">
            <thead><tr><th>№</th><th>Кассир</th><th>Дата</th><th>Сумма</th><th>Статус ОФД</th></tr></thead>
            <tbody>
              <tr><td style={{ color: "var(--text-muted)" }}>—</td><td>—</td><td>—</td><td>—</td><td><span className="badge badge-warning">Нет данных</span></td></tr>
            </tbody>
          </table>
        </div>

        {/* Fiscal receipts queue */}
        <div className="card">
          <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>📡 Очередь фискальных чеков</h3>
          <p style={{ fontSize: 13, color: "var(--text-secondary)", marginBottom: 16 }}>Чеки, ожидающие отправки в ОФД при сбоях интернета</p>
          <table className="data-table">
            <thead><tr><th>Чек №</th><th>Сумма</th><th>Попытки</th><th>Статус</th></tr></thead>
            <tbody>
              <tr><td colSpan={4} style={{ textAlign: "center", padding: 24, color: "var(--text-muted)" }}>Очередь пуста</td></tr>
            </tbody>
          </table>
        </div>
      </div>

      {/* FD/FPD registry */}
      <div className="card" style={{ marginTop: 24 }}>
        <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>📋 Реестр фискальных признаков</h3>
        <table className="data-table">
          <thead><tr><th>Чек №</th><th>FD</th><th>FPD</th><th>Дата</th><th>Сумма</th></tr></thead>
          <tbody>
            <tr><td colSpan={5} style={{ textAlign: "center", padding: 24, color: "var(--text-muted)" }}>Нет фискализированных чеков</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}
