import { useState, useEffect } from "react";
import { useTranslation } from "../i18n/LanguageContext";
import api from "../api/client";

const STATUS_MAP = {
  completed: "statusCompleted",
  failed: "statusFailed",
  processing: "statusProcessing",
  created: "statusCreated",
  refunded: "statusRefunded",
};

const STATUS_BADGE = {
  completed: "badge-success",
  failed: "badge-danger",
  processing: "badge-warning",
  created: "badge-info",
  refunded: "badge-muted",
};

export default function Transactions() {
  const { t } = useTranslation();
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [filters, setFilters] = useState({ search: "", status: "" });

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);
      const params = {};
      if (filters.search) params.search = filters.search;
      if (filters.status) params.status = filters.status;
      const result = await api.getTransactions(params);
      setTransactions(
        Array.isArray(result) ? result : (result?.data ?? result?.items ?? []),
      );
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleFilter = (e) => {
    e.preventDefault();
    fetchData();
  };

  const handleReset = () => {
    setFilters({ search: "", status: "" });
    setTimeout(() => fetchData(), 0);
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return "-";
    try {
      return new Date(dateStr).toLocaleString();
    } catch {
      return dateStr;
    }
  };

  const formatAmount = (amount) => {
    if (amount == null) return "-";
    return Number(amount).toLocaleString();
  };

  return (
    <div>
      <div className="page-header">
        <form className="filter-bar" onSubmit={handleFilter}>
          <div className="form-group">
            <input
              type="search"
              placeholder={t("common.search") + "..."}
              value={filters.search}
              onChange={(e) =>
                setFilters((f) => ({ ...f, search: e.target.value }))
              }
            />
          </div>
          <div className="form-group">
            <select
              value={filters.status}
              onChange={(e) =>
                setFilters((f) => ({ ...f, status: e.target.value }))
              }
            >
              <option value="">{t("common.status")}</option>
              <option value="completed">
                {t("transactions.statusCompleted")}
              </option>
              <option value="failed">{t("transactions.statusFailed")}</option>
              <option value="processing">
                {t("transactions.statusProcessing")}
              </option>
              <option value="created">{t("transactions.statusCreated")}</option>
              <option value="refunded">
                {t("transactions.statusRefunded")}
              </option>
            </select>
          </div>
          <button type="submit" className="btn btn-primary">
            {t("common.filter")}
          </button>
          <button
            type="button"
            className="btn btn-outline"
            onClick={handleReset}
          >
            {t("common.reset")}
          </button>
        </form>
      </div>

      {loading ? (
        <div className="loading-container">
          <div className="spinner spinner-lg" />
          <span>{t("common.loading")}</span>
        </div>
      ) : error ? (
        <div className="empty-state">
          <div className="empty-state-icon">{String.fromCodePoint(0x26a0)}</div>
          <div className="empty-state-text text-danger">
            {t("common.error")}
          </div>
          <div className="text-muted text-sm mt-2">{error}</div>
          <button className="btn btn-outline mt-4" onClick={fetchData}>
            {t("common.refresh")}
          </button>
        </div>
      ) : transactions.length === 0 ? (
        <div className="empty-state">
          <div className="empty-state-icon">
            {String.fromCodePoint(0x1f4cb)}
          </div>
          <div className="empty-state-text">{t("common.noData")}</div>
        </div>
      ) : (
        <div className="table-container">
          <table>
            <thead>
              <tr>
                <th>{t("transactions.tableHeaders.id")}</th>
                <th>{t("transactions.tableHeaders.contragent")}</th>
                <th>{t("transactions.tableHeaders.amount")}</th>
                <th>{t("transactions.tableHeaders.currency")}</th>
                <th>{t("transactions.tableHeaders.status")}</th>
                <th>{t("transactions.tableHeaders.date")}</th>
                <th>{t("transactions.tableHeaders.paymentSystem")}</th>
                <th>{t("transactions.tableHeaders.station")}</th>
                <th>{t("transactions.tableHeaders.column")}</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((tx) => (
                <tr key={tx.id || tx.transactionId}>
                  <td className="font-mono text-sm">
                    {tx.id || tx.transactionId || "-"}
                  </td>
                  <td>{tx.contragent || tx.carNumber || "-"}</td>
                  <td className="font-mono">
                    {formatAmount(tx.amount)} {tx.currency || "UZS"}
                  </td>
                  <td>{tx.currency || "UZS"}</td>
                  <td>
                    <span
                      className={`badge ${STATUS_BADGE[tx.status] || "badge-muted"}`}
                    >
                      {t(
                        "transactions." + (STATUS_MAP[tx.status] || tx.status),
                      ) || tx.status}
                    </span>
                  </td>
                  <td className="text-muted text-sm">
                    {formatDate(tx.date || tx.createdAt)}
                  </td>
                  <td>{tx.paymentSystem || "-"}</td>
                  <td>{tx.stationName || "-"}</td>
                  <td>{tx.columnName || "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
