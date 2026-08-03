import { useState, useEffect, useCallback } from "react";
import { useTranslation } from "../i18n/LanguageContext";
import { api } from "../api/client";

export default function Reports() {
  const { t } = useTranslation();
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState(null);
  const [summary, setSummary] = useState(null);
  const [deptData, setDeptData] = useState([]);
  const [cashierData, setCashierData] = useState([]);

  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(null);

  const loadData = useCallback(async () => {
    try {
      const [s, d, c] = await Promise.all([
        api.getReportSummary(year, month),
        api.getReportByDepartment(year, month),
        api.getReportByCashier(year, month),
      ]);
      setSummary(s); setDeptData(Array.isArray(d) ? d : []); setCashierData(Array.isArray(c) ? c : []);
    } catch { }
  }, [year, month]);

  useEffect(() => { loadData(); }, [loadData]);

  const handleExport = async (period) => {
    setExporting(true); setError(null);
    try {
      const blob = await api.exportTransactions(period === "month" ? year : year, period === "month" ? month || now.getMonth() + 1 : null);
      const url = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = url;
      a.download = period === "month" ? `Otchet_${String(month || now.getMonth() + 1).padStart(2, "0")}.${year}.xlsx` : `Otchet_${year}.xlsx`;
      a.click(); URL.revokeObjectURL(url);
    } catch (e) { setError(e.message); }
    finally { setExporting(false); }
  };

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24, flexWrap: "wrap", gap: 12 }}>
        <h2 style={{ fontSize: 24, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#fff" }}>📥 {t("reports")}</h2>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <select className="input" style={{ width: 100 }} value={year} onChange={(e) => setYear(Number(e.target.value))}>
            {[2024, 2025, 2026].map((y) => <option key={y} value={y}>{y}</option>)}
          </select>
          <select className="input" style={{ width: 160 }} value={month || ""} onChange={(e) => setMonth(e.target.value ? Number(e.target.value) : null)}>
            <option value="">Весь год</option>
            {["Январь","Февраль","Март","Апрель","Май","Июнь","Июль","Август","Сентябрь","Октябрь","Ноябрь","Декабрь"].map((m, i) => <option key={i+1} value={i+1}>{m}</option>)}
          </select>
        </div>
      </div>

      {error && <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: "10px 14px", borderRadius: 10, marginBottom: 16 }}>{error}</div>}

      {/* Export cards */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))", gap: 16, marginBottom: 24 }}>
        {[
          { icon: "📅", title: "Excel за месяц", period: "month" },
          { icon: "📆", title: "Excel за год", period: "year" },
        ].map((r, i) => (
          <div key={i} className="card" style={{ textAlign: "center" }}>
            <div style={{ fontSize: 36, marginBottom: 8 }}>{r.icon}</div>
            <h4 style={{ fontSize: 14, fontWeight: 600, color: "#fff", marginBottom: 16 }}>{r.title}</h4>
            <button onClick={() => handleExport(r.period)} disabled={exporting} className="btn btn-primary" style={{ width: "100%", justifyContent: "center" }}>📥 Скачать</button>
          </div>
        ))}
      </div>

      {/* Summary */}
      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(350px, 1fr))", gap: 24 }}>
        <div className="card">
          <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>📊 Сводка</h3>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
            <div style={{ padding: 12, background: "var(--bg-hover)", borderRadius: 10, textAlign: "center" }}>
              <div style={{ fontSize: 20, fontWeight: 700, color: "var(--accent)" }}>{(summary?.totalRevenue || 0).toLocaleString()} UZS</div>
              <div style={{ fontSize: 11, color: "var(--text-muted)", marginTop: 4 }}>Общая выручка</div>
            </div>
            <div style={{ padding: 12, background: "var(--bg-hover)", borderRadius: 10, textAlign: "center" }}>
              <div style={{ fontSize: 20, fontWeight: 700, color: "#60a5fa" }}>{summary?.transactionCount || 0}</div>
              <div style={{ fontSize: 11, color: "var(--text-muted)", marginTop: 4 }}>Транзакций</div>
            </div>
          </div>
          {summary?.byPaymentMethod && summary.byPaymentMethod.length > 0 && (
            <div style={{ marginTop: 16 }}>
              <h4 style={{ fontSize: 13, color: "var(--text-secondary)", marginBottom: 8 }}>По способам оплаты:</h4>
              {summary.byPaymentMethod.map((pm, i) => (
                <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "4px 0", fontSize: 12, color: "var(--text-secondary)" }}>
                  <span>{pm.paymentMethod}</span>
                  <span style={{ color: "var(--accent)", fontWeight: 600 }}>{(pm.revenue || 0).toLocaleString()} UZS</span>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="card">
          <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", marginBottom: 16 }}>🏥 По отделениям</h3>
          {deptData.length === 0 ? <div className="empty-state" style={{ padding: 16 }}><p>Нет данных</p></div> :
            deptData.map((d, i) => (
              <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "8px 0", borderBottom: "1px solid var(--border)", fontSize: 13 }}>
                <span style={{ color: "var(--text-primary)" }}>{d.departmentName || `Отделение #${d.departmentId}`}</span>
                <span style={{ color: "var(--accent)", fontWeight: 600 }}>{(d.revenue || 0).toLocaleString()} UZS</span>
              </div>
            ))}
        </div>
      </div>

      {/* By cashier */}
      <div className="card" style={{ marginTop: 24, padding: 0, overflow: "hidden" }}>
        <h3 style={{ fontSize: 16, fontWeight: 600, fontFamily: "'Montserrat', sans-serif", color: "var(--accent)", padding: "20px 24px 0" }}>👨‍⚕️ По кассирам</h3>
        <table className="data-table">
          <thead><tr><th>Кассир</th><th>Кол-во</th><th>Сумма</th></tr></thead>
          <tbody>
            {cashierData.length === 0 ? <tr><td colSpan={3} style={{ textAlign: "center", color: "var(--text-muted)", padding: 24 }}>Нет данных</td></tr> :
              cashierData.map((c, i) => (
                <tr key={i}>
                  <td style={{ fontWeight: 600 }}>{c.cashier || "—"}</td>
                  <td>{c.transactionCount || 0}</td>
                  <td style={{ color: "var(--accent)", fontWeight: 600 }}>{(c.revenue || 0).toLocaleString()} UZS</td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
