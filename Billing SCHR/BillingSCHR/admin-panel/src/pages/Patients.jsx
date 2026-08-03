import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function Patients() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState("");

  const fetchData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.getPatients();
      // API returns paginated: { data: [...], totalCount, ... }
      setData(Array.isArray(res) ? res : (res.data || []));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchData(); }, [fetchData]);

  const filtered = search
    ? data.filter(
        (p) =>
          String(p.id).includes(search) ||
          p.fullName?.toLowerCase().includes(search.toLowerCase()) ||
          p.militaryRank?.toLowerCase().includes(search.toLowerCase()) ||
          p.militaryUnit?.toLowerCase().includes(search.toLowerCase()) ||
          p.phone?.includes(search),
      )
    : data;

  const formatDate = (d) => {
    if (!d) return "—";
    try { return new Date(d).toLocaleString(); } catch { return String(d); }
  };

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>👤 {t("patients")}</h2>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <input
            className="input"
            style={{ width: 240, padding: "8px 12px" }}
            placeholder={"🔍 " + t("search") + "..."}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <button className="btn btn-ghost btn-sm" onClick={fetchData}>🔄</button>
        </div>
      </div>

      {error && (
        <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: "12px 16px", borderRadius: 8, marginBottom: 16, display: "flex", justifyContent: "space-between" }}>
          <span>{error}</span>
          <button onClick={() => setError(null)} style={{ color: "var(--danger)", fontWeight: 700, cursor: "pointer", background: "none", border: "none" }}>×</button>
        </div>
      )}

      {loading ? (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}>
          <div className="spinner" /><span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
        </div>
      ) : data.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">👤</div>
          <p>{t("noPatients")}</p>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>ID</th><th>{t("fullName")}</th><th>{t("militaryRank")}</th><th>{t("militaryUnit")}</th><th>{t("phone")}</th><th>{t("bloodType")}</th><th>{t("source")}</th><th>{t("registeredAt")}</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((item) => (
                  <tr key={item.id}>
                    <td style={{ color: "var(--text-muted)", fontSize: 12 }}>#{item.id}</td>
                    <td style={{ fontWeight: 600 }}>{item.fullName || "—"}</td>
                    <td>{item.militaryRank || "—"}</td>
                    <td>{item.militaryUnit || "—"}</td>
                    <td>{item.phone || "—"}</td>
                    <td>{item.bloodType || "—"}</td>
                    <td><span className="badge badge-info">{item.source || "manual"}</span></td>
                    <td style={{ color: "var(--text-secondary)", fontSize: 12 }}>{formatDate(item.registeredAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div style={{ padding: "12px 16px", borderTop: "1px solid var(--border)", color: "var(--text-secondary)", fontSize: 12 }}>
            {t("total")}: {filtered.length} / {data.length}
          </div>
        </div>
      )}
    </div>
  );
}
