import { useState } from "react";
import { useTranslation } from "../i18n/LanguageContext";
import api from "../api/client";

const PROVIDERS = [
  { value: "uzcard", key: "uzcard" },
  { value: "humo", key: "humo" },
  { value: "click", key: "click" },
  { value: "payme", key: "payme" },
  { value: "apelsin", key: "apelsin" },
];

export default function TestImitation() {
  const { t } = useTranslation();
  const [provider, setProvider] = useState("");
  const [amount, setAmount] = useState("");
  const [carNumber, setCarNumber] = useState("");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!provider) {
      setError(t("testImitation.selectProvider"));
      return;
    }
    if (!amount || isNaN(Number(amount)) || Number(amount) <= 0) {
      setError(t("testImitation.enterAmount"));
      return;
    }

    setError(null);
    setResult(null);
    setLoading(true);

    try {
      const data = await api.createPayment({
        amount: Number(amount),
        currency: "UZS",
        paymentSystem: provider,
        carNumber: carNumber || undefined,
        metadata: { source: "admin-test-imitation" },
      });
      setResult(data);
    } catch (err) {
      setError(err.message || t("common.failed"));
    } finally {
      setLoading(false);
    }
  };

  const handleReset = () => {
    setProvider("");
    setAmount("");
    setCarNumber("");
    setResult(null);
    setError(null);
  };

  return (
    <div>
      <div className="warning-banner">
        <span>{String.fromCodePoint(0x26A0)}</span>
        <span>{t("testImitation.testWarning")}</span>
      </div>

      <div className="card" style={{ maxWidth: 560 }}>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>{t("testImitation.selectProvider")}</label>
            <select
              value={provider}
              onChange={(e) => setProvider(e.target.value)}
            >
              <option value="">-- {t("testImitation.selectProvider")} --</option>
              {PROVIDERS.map((p) => (
                <option key={p.value} value={p.value}>
                  {t("testImitation.providers." + p.key)}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>{t("testImitation.enterAmount")} (UZS)</label>
            <input
              type="number"
              placeholder="10000"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              min="1"
            />
          </div>

          <div className="form-group">
            <label>{t("testImitation.enterCarNumber")}</label>
            <input
              type="text"
              placeholder="01A123AA"
              value={carNumber}
              onChange={(e) => setCarNumber(e.target.value)}
            />
          </div>

          <div className="flex gap-2">
            <button
              type="submit"
              className="btn btn-primary"
              disabled={loading}
            >
              {loading ? (
                <>
                  <span className="spinner" />
                  {t("testImitation.submitting")}
                </>
              ) : (
                t("testImitation.submitButton")
              )}
            </button>
            <button
              type="button"
              className="btn btn-outline"
              onClick={handleReset}
              disabled={loading}
            >
              {t("common.reset")}
            </button>
          </div>
        </form>
      </div>

      {error && (
        <div className="result-card error mt-4">
          <div className="result-row">
            <span className="result-label text-danger">{t("common.error")}</span>
            <span className="result-value text-danger">{error}</span>
          </div>
        </div>
      )}

      {result && (
        <div className="result-card mt-4">
          <h3 className="mb-3" style={{ color: "var(--accent-green)", fontSize: "0.95rem", fontWeight: 600 }}>
            {t("testImitation.transactionCreated")}
          </h3>
          <div className="result-row">
            <span className="result-label">{t("testImitation.transactionId")}</span>
            <span className="result-value font-mono">
              {result.id || result.transactionId || "-"}
            </span>
          </div>
          <div className="result-row">
            <span className="result-label">{t("testImitation.provider")}</span>
            <span className="result-value">
              {t("testImitation.providers." + provider) || provider}
            </span>
          </div>
          <div className="result-row">
            <span className="result-label">{t("common.amount")}</span>
            <span className="result-value">
              {Number(amount).toLocaleString()} UZS
            </span>
          </div>
          {carNumber && (
            <div className="result-row">
              <span className="result-label">{t("testImitation.carNumberLabel")}</span>
              <span className="result-value font-mono">{carNumber}</span>
            </div>
          )}
          <div className="result-row">
            <span className="result-label">{t("testImitation.createdAt")}</span>
            <span className="result-value text-sm">
              {result.createdAt
                ? new Date(result.createdAt).toLocaleString()
                : new Date().toLocaleString()}
            </span>
          </div>
        </div>
      )}
    </div>
  );
}
