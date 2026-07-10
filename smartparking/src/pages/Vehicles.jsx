import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

function parseParking(paymentMethod) {
  try {
    return JSON.parse(paymentMethod);
  } catch {
    return null;
  }
}

export default function Vehicles() {
  const { t } = useTranslation();
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const res = await api.getTransactions();
      setTransactions(Array.isArray(res) ? res : []);
    } catch {
      setTransactions([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // Extract unique vehicles from parking transactions
  const parkingTxs = transactions.filter((tx) => tx.status === "parking");

  const vehicleMap = {};
  parkingTxs.forEach((tx) => {
    const p = parseParking(tx.paymentMethod);
    if (!p) return;
    const plate = p.AvtoRaqam || p.avtoRaqam || "—";
    if (!vehicleMap[plate]) {
      vehicleMap[plate] = {
        plate,
        visits: 0,
        totalPaid: 0,
        lastVisit: null,
        chekIds: [],
      };
    }
    vehicleMap[plate].visits++;
    if (tx.paymentStatus === "Completed" || tx.paymentStatus === "Paid") {
      vehicleMap[plate].totalPaid += tx.totalSum || 0;
    }
    const d = tx.filledAt ? new Date(tx.filledAt) : null;
    if (d && (!vehicleMap[plate].lastVisit || d > vehicleMap[plate].lastVisit)) {
      vehicleMap[plate].lastVisit = d;
    }
    const cp = parseParking(tx.paymentMethod);
    const cid = cp?.ChekId || cp?.chekId;
    if (cid) vehicleMap[plate].chekIds.push(cid);
  });

  const vehicles = Object.values(vehicleMap);

  const filtered = search
    ? vehicles.filter((v) =>
        v.plate.toLowerCase().includes(search.toLowerCase()),
      )
    : vehicles;

  const formatDate = (d) => {
    if (!d) return "—";
    try { return d.toLocaleString(); } catch { return String(d); }
  };

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>
          🚗 {t("vehicles")}
        </h2>
        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <input
            className="input"
            style={{ width: 240, padding: "8px 12px" }}
            placeholder={"🔍 " + t("searchPlate") + "..."}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <button className="btn btn-ghost btn-sm" onClick={fetchData}>
            🔄
          </button>
        </div>
      </div>

      {loading ? (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}>
          <div className="spinner" />
          <span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
        </div>
      ) : vehicles.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">🚗</div>
          <p>{t("noVehicles")}</p>
        </div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>🚗 {t("plateNumber")}</th>
                  <th>{t("visits")}</th>
                  <th>{t("totalPaid")}</th>
                  <th>{t("lastVisit")}</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((v) => (
                  <tr key={v.plate}>
                    <td>
                      <span
                        style={{
                          fontWeight: 700,
                          fontSize: 15,
                          fontFamily: "monospace",
                          background: "var(--bg-primary)",
                          padding: "3px 10px",
                          borderRadius: 6,
                          border: "1px solid var(--border)",
                        }}
                      >
                        {v.plate}
                      </span>
                    </td>
                    <td>
                      <span className="badge badge-info">{v.visits}</span>
                    </td>
                    <td style={{ fontWeight: 600, color: "var(--success)" }}>
                      {Number(v.totalPaid).toLocaleString()} UZS
                    </td>
                    <td style={{ color: "var(--text-secondary)", fontSize: 12 }}>
                      {formatDate(v.lastVisit)}
                    </td>
                  </tr>
                ))}
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
            {t("total")}: {filtered.length} / {vehicles.length} {t("vehicles").toLowerCase()}
          </div>
        </div>
      )}
    </div>
  );
}
