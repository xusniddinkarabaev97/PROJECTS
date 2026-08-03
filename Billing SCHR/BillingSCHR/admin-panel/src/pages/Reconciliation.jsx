import { useTranslation } from "../i18n/LanguageContext";

export default function Reconciliation() {
  const { t } = useTranslation();

  return (
    <div>
      <h2 style={{ fontSize: 24, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#fff", marginBottom: 24 }}>🔍 {t("reconciliation")}</h2>

      {/* Upload settlement file */}
      <div className="card" style={{ marginBottom: 24 }}>
        <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>📤 Загрузка файла сверки (File Settlement)</h3>
        <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "flex-end" }}>
          <div>
            <label style={{ display: "block", fontSize: 12, color: "var(--text-secondary)", marginBottom: 4 }}>Банк-эквайер</label>
            <select className="input" style={{ width: 180 }}>
              <option>Uzcard</option><option>Humo</option>
            </select>
          </div>
          <div>
            <label style={{ display: "block", fontSize: 12, color: "var(--text-secondary)", marginBottom: 4 }}>Дата сверки</label>
            <input className="input" type="date" style={{ width: 180 }} />
          </div>
          <div style={{ border: "1px dashed var(--border)", borderRadius: 10, padding: "10px 20px", textAlign: "center", cursor: "pointer" }}>
            <div style={{ fontSize: 20 }}>📁</div>
            <div style={{ fontSize: 11, color: "var(--text-muted)" }}>Выбрать файл</div>
          </div>
          <button className="btn btn-primary">🔄 {t("reconciliationRun")}</button>
        </div>
      </div>

      {/* Results */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(380px, 1fr))", gap: 24 }}>
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", padding: "20px 24px 0" }}>🏦 Uzcard</h3>
          <table className="data-table">
            <thead><tr><th>Показатель</th><th>Банк</th><th>Система</th><th>Расхождение</th></tr></thead>
            <tbody>
              <tr><td style={{ fontWeight: 600 }}>Транзакций</td><td>—</td><td>—</td><td style={{ color: "var(--text-muted)" }}>—</td></tr>
              <tr><td style={{ fontWeight: 600 }}>Сумма</td><td>—</td><td>—</td><td style={{ color: "var(--text-muted)" }}>—</td></tr>
              <tr><td style={{ fontWeight: 600 }}>Статус</td><td colSpan={3}><span className="badge badge-warning">Ожидание сверки</span></td></tr>
            </tbody>
          </table>
        </div>

        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", padding: "20px 24px 0" }}>🏦 Humo</h3>
          <table className="data-table">
            <thead><tr><th>Показатель</th><th>Банк</th><th>Система</th><th>Расхождение</th></tr></thead>
            <tbody>
              <tr><td style={{ fontWeight: 600 }}>Транзакций</td><td>—</td><td>—</td><td style={{ color: "var(--text-muted)" }}>—</td></tr>
              <tr><td style={{ fontWeight: 600 }}>Сумма</td><td>—</td><td>—</td><td style={{ color: "var(--text-muted)" }}>—</td></tr>
              <tr><td style={{ fontWeight: 600 }}>Статус</td><td colSpan={3}><span className="badge badge-warning">Ожидание сверки</span></td></tr>
            </tbody>
          </table>
        </div>
      </div>

      {/* Discrepancies */}
      <div className="card" style={{ marginTop: 24, padding: 0, overflow: "hidden" }}>
        <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", padding: "20px 24px 0" }}>⚠️ Расхождения</h3>
        <table className="data-table">
          <thead><tr><th>Банк</th><th>ID транзакции</th><th>Сумма в банке</th><th>Сумма в системе</th><th>Тип</th></tr></thead>
          <tbody>
            <tr><td colSpan={5} style={{ textAlign: "center", padding: 24, color: "var(--text-muted)" }}>Расхождений не найдено</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}
