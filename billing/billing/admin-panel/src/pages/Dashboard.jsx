import { useState, useEffect } from "react";
import { useTranslation } from "../i18n/LanguageContext";
import api from "../api/client";

export default function Dashboard() {
  const { t } = useTranslation();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    let cancelled = false;
    async function fetchData() {
      try {
        setLoading(true);
        setError(null);
        const result = await api.getDashboard();
        if (!cancelled) setData(result);
      } catch (err) {
        if (!cancelled) setError(err.message);
      } finally {
        if (!cancelled) setLoading(false);
      }
    }
    fetchData();
    return () => {
      cancelled = true;
    };
  }, []);

  if (loading) {
    return (
      <div className="loading-container">
        <div className="spinner spinner-lg" />
        <span>{t("common.loading")}</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon">⚠</div>
        <div className="empty-state-text text-danger">{t("common.error")}</div>
        <div className="text-muted text-sm mt-2">{error}</div>
        <button
          className="btn btn-outline mt-4"
          onClick={() => window.location.reload()}
        >
          {t("common.refresh")}
        </button>
      </div>
    );
  }

  const totalTransactions = data?.totalTransactions ?? 0;
  const todayAmount = data?.todayTotalAmount ?? 0;

  return (
    <div>
      <div className="grid grid-cols-2 mb-4">
        <div className="card">
          <div className="card-header">{t("dashboard.totalTransactions")}</div>
          <div className="card-body">{totalTransactions.toLocaleString()}</div>
        </div>
        <div className="card">
          <div className="card-header">{t("dashboard.todayAmount")}</div>
          <div className="card-body text-success">
            {todayAmount.toLocaleString()} UZS
          </div>
        </div>
      </div>
    </div>
  );
}
