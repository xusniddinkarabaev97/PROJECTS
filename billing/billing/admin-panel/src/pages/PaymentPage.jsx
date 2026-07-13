import { useState, useEffect, useCallback } from "react";
import { useParams } from "react-router-dom";
import { useTranslation } from "../i18n/LanguageContext";
import { request } from "../api/client";

const PAYMENT_PROVIDERS = [
  { key: "uzcard", label: "Uzcard", color: "#1a73e8" },
  { key: "humo", label: "Humo", color: "#e91e63" },
  { key: "click", label: "Click", color: "#00c853" },
  { key: "payme", label: "Payme", color: "#7c4dff" },
  { key: "apelsin", label: "Apelsin", color: "#ff6d00" },
];

export default function PaymentPage() {
  const { columnId } = useParams();
  const { t, lang, toggleLanguage } = useTranslation();

  const [column, setColumn] = useState(null);
  const [station, setStation] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [amount, setAmount] = useState("");
  const [paymentSystem, setPaymentSystem] = useState("");
  const [carNumber, setCarNumber] = useState("");

  const [paying, setPaying] = useState(false);
  const [result, setResult] = useState(null);
  const [payError, setPayError] = useState(null);

  // Fetch column and station info
  useEffect(() => {
    if (!columnId) {
      setError(
        t("payment.noColumnId") ||
          "No column ID provided. Please use a valid payment link, e.g. /pay/123",
      );
      setLoading(false);
      return;
    }

    let cancelled = false;

    async function load() {
      try {
        // Fetch all stations, then find the column
        const stations = await request("/v1/stations");
        if (cancelled) return;

        for (const s of stations) {
          try {
            const columns = await request("/v1/stations/" + s.id + "/columns");
            const col = columns.find((c) => String(c.id) === String(columnId));
            if (col) {
              setColumn(col);
              setStation(s);
              return;
            }
          } catch {
            // Continue to next station
          }
        }

        if (!cancelled) {
          setError("Column not found");
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message || "Failed to load column data");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    load();
    return () => {
      cancelled = true;
    };
  }, [columnId]);

  const pricePerLiter = column ? Number(column.pricePerLiter) : 0;
  const liters =
    pricePerLiter > 0 && amount
      ? (Number(amount) / pricePerLiter).toFixed(2)
      : "0.00";

  const handleAmountChange = (e) => {
    const val = e.target.value;
    // Allow only digits and optional decimal
    if (val === "" || /^\d*\.?\d{0,2}$/.test(val)) {
      setAmount(val);
    }
  };

  const handlePay = useCallback(
    async (e) => {
      e.preventDefault();
      if (!amount || Number(amount) <= 0) return;
      if (!paymentSystem) {
        setPayError(t("payment.selectProvider"));
        return;
      }

      setPaying(true);
      setPayError(null);
      setResult(null);

      try {
        const data = await request("/pay/" + columnId, {
          method: "POST",
          body: {
            amount: Number(amount),
            paymentSystem,
            carNumber: carNumber || undefined,
          },
        });
        setResult(data);
      } catch (err) {
        setPayError(err.message || t("common.error"));
      } finally {
        setPaying(false);
      }
    },
    [amount, paymentSystem, carNumber, columnId, t],
  );

  // Loading state
  if (loading) {
    return (
      <div style={s.wrapper}>
        <div style={s.card}>
          <div className="spinner" />
          <p style={{ color: "#8b949e", marginTop: 16 }}>
            {t("common.loading")}
          </p>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div style={s.wrapper}>
        <div style={s.card}>
          <div style={{ fontSize: 48, marginBottom: 16 }}>{"\u26A0\uFE0F"}</div>
          <h2 style={{ color: "#f85149", margin: "0 0 8px" }}>
            {t("common.error")}
          </h2>
          <p style={{ color: "#8b949e", margin: 0 }}>{error}</p>
        </div>
      </div>
    );
  }

  // Success result card
  if (result) {
    return (
      <div style={s.wrapper}>
        <div style={s.card}>
          <div style={{ fontSize: 48, marginBottom: 12 }}>{"\u2705"}</div>
          <h2 style={{ color: "#3fb950", margin: "0 0 20px", fontSize: 24 }}>
            {t("payment.success")}
          </h2>
          <div style={s.resultGrid}>
            <div style={s.resultRow}>
              <span style={s.resultLabel}>{t("payment.transactionId")}</span>
              <span style={s.resultValue}>
                {result.transactionId || result.id || "-"}
              </span>
            </div>
            <div style={s.resultRow}>
              <span style={s.resultLabel}>{t("payment.station")}</span>
              <span style={s.resultValue}>{station?.name || "-"}</span>
            </div>
            <div style={s.resultRow}>
              <span style={s.resultLabel}>{t("payment.fuelType")}</span>
              <span style={s.resultValue}>{column?.fuelType || "-"}</span>
            </div>
            <div style={s.resultRow}>
              <span style={s.resultLabel}>{t("payment.amount")}</span>
              <span style={s.resultValue}>
                {Number(amount).toLocaleString()} UZS
              </span>
            </div>
            <div style={s.resultRow}>
              <span style={s.resultLabel}>{t("payment.liters")}</span>
              <span style={s.resultValue}>{liters} L</span>
            </div>
            {result.processedAt && (
              <div style={s.resultRow}>
                <span style={s.resultLabel}>{t("payment.processedAt")}</span>
                <span style={s.resultValue}>
                  {new Date(result.processedAt).toLocaleString(
                    lang === "uz" ? "uz-UZ" : "ru-RU",
                  )}
                </span>
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  // Payment form
  return (
    <div style={s.wrapper}>
      {/* Lang toggle */}
      <button style={s.langToggle} onClick={toggleLanguage}>
        {lang === "uz" ? "RU" : "UZ"}
      </button>

      <div style={s.card}>
        <h1 style={s.title}>{t("payment.pageTitle")}</h1>

        {/* Station & Column info */}
        <div style={s.infoBlock}>
          <div style={s.infoRow}>
            <span style={s.infoLabel}>{t("payment.station")}</span>
            <span style={s.infoValue}>{station?.name || "-"}</span>
          </div>
          <div style={s.infoRow}>
            <span style={s.infoLabel}>{t("payment.column")}</span>
            <span style={s.infoValue}>
              {column?.name || "-"}
              {column?.columnNumber != null &&
                " (#" + column.columnNumber + ")"}
            </span>
          </div>
          <div style={s.infoRow}>
            <span style={s.infoLabel}>{t("payment.fuelType")}</span>
            <span style={s.infoValue}>{column?.fuelType || "-"}</span>
          </div>
          <div style={s.infoRow}>
            <span style={s.infoLabel}>{t("payment.pricePerLiter")}</span>
            <span style={s.infoValue}>
              {pricePerLiter ? pricePerLiter.toLocaleString() + " UZS" : "-"}
            </span>
          </div>
        </div>

        <form onSubmit={handlePay} style={s.form}>
          {/* Amount input */}
          <div style={s.fieldGroup}>
            <label style={s.label}>{t("payment.enterAmount")}</label>
            <div style={s.amountInputWrap}>
              <input
                type="text"
                inputMode="decimal"
                value={amount}
                onChange={handleAmountChange}
                placeholder="0.00"
                style={s.input}
                autoFocus
                disabled={paying}
              />
              <span style={s.currencySuffix}>UZS</span>
            </div>
            <div style={s.litersHint}>
              {t("payment.liters")}: <strong>{liters} L</strong>
            </div>
          </div>

          {/* Payment system selector */}
          <div style={s.fieldGroup}>
            <label style={s.label}>{t("payment.selectProvider")}</label>
            <div style={s.providerGrid}>
              {PAYMENT_PROVIDERS.map((p) => (
                <button
                  key={p.key}
                  type="button"
                  onClick={() => setPaymentSystem(p.key)}
                  disabled={paying}
                  style={{
                    ...s.providerBtn,
                    borderColor:
                      paymentSystem === p.key ? p.color : "transparent",
                    background:
                      paymentSystem === p.key
                        ? p.color + "22"
                        : "rgba(255,255,255,0.06)",
                    opacity: paying ? 0.5 : 1,
                  }}
                >
                  <span
                    style={{
                      ...s.providerBadge,
                      background: p.color,
                    }}
                  />
                  <span style={{ color: "#e6edf3" }}>{p.label}</span>
                </button>
              ))}
            </div>
          </div>

          {/* Car number */}
          <div style={s.fieldGroup}>
            <label style={s.label}>{t("payment.carNumber")}</label>
            <input
              type="text"
              value={carNumber}
              onChange={(e) => setCarNumber(e.target.value)}
              placeholder="01A999AA"
              style={s.input}
              disabled={paying}
            />
          </div>

          {/* Error */}
          {payError && (
            <div style={s.errorBox}>{"\u26A0\uFE0F " + payError}</div>
          )}

          {/* Submit */}
          <button
            type="submit"
            disabled={paying || !amount || Number(amount) <= 0}
            style={{
              ...s.payBtn,
              opacity: paying || !amount || Number(amount) <= 0 ? 0.6 : 1,
              cursor: paying ? "wait" : "pointer",
            }}
          >
            {paying ? (
              <>
                <span
                  className="spinner"
                  style={{ width: 18, height: 18, borderWidth: 2 }}
                />
                <span>{t("payment.paying")}</span>
              </>
            ) : (
              t("payment.pay")
            )}
          </button>
        </form>
      </div>
    </div>
  );
}

const s = {
  wrapper: {
    minHeight: "100vh",
    background: "#0d1117",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    padding: 20,
    position: "relative",
  },
  langToggle: {
    position: "absolute",
    top: 16,
    right: 16,
    background: "rgba(255,255,255,0.08)",
    border: "1px solid rgba(255,255,255,0.12)",
    borderRadius: 6,
    color: "#e6edf3",
    padding: "6px 14px",
    fontSize: 13,
    fontWeight: 600,
    cursor: "pointer",
    fontFamily: "inherit",
  },
  card: {
    background: "#161b22",
    border: "1px solid #30363d",
    borderRadius: 12,
    padding: "32px 28px",
    width: "100%",
    maxWidth: 420,
    boxShadow: "0 8px 24px rgba(0,0,0,0.4)",
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
  },
  title: {
    color: "#e6edf3",
    fontSize: 22,
    fontWeight: 700,
    margin: "0 0 24px",
    textAlign: "center",
  },
  infoBlock: {
    width: "100%",
    background: "rgba(255,255,255,0.03)",
    borderRadius: 8,
    padding: "16px 18px",
    marginBottom: 24,
  },
  infoRow: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "6px 0",
  },
  infoLabel: {
    color: "#8b949e",
    fontSize: 13,
  },
  infoValue: {
    color: "#e6edf3",
    fontSize: 13,
    fontWeight: 600,
  },
  form: {
    width: "100%",
    display: "flex",
    flexDirection: "column",
    gap: 18,
  },
  fieldGroup: {
    display: "flex",
    flexDirection: "column",
    gap: 6,
  },
  label: {
    color: "#8b949e",
    fontSize: 12,
    fontWeight: 500,
    textTransform: "uppercase",
    letterSpacing: "0.5px",
  },
  amountInputWrap: {
    position: "relative",
  },
  input: {
    width: "100%",
    boxSizing: "border-box",
    padding: "10px 12px",
    background: "#0d1117",
    border: "1px solid #30363d",
    borderRadius: 6,
    color: "#e6edf3",
    fontSize: 16,
    fontFamily: "inherit",
    outline: "none",
    transition: "border-color 0.15s",
  },
  currencySuffix: {
    position: "absolute",
    right: 12,
    top: "50%",
    transform: "translateY(-50%)",
    color: "#8b949e",
    fontSize: 13,
    fontWeight: 600,
    pointerEvents: "none",
  },
  litersHint: {
    color: "#8b949e",
    fontSize: 13,
  },
  providerGrid: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fill, minmax(90px, 1fr))",
    gap: 8,
  },
  providerBtn: {
    display: "flex",
    alignItems: "center",
    gap: 8,
    padding: "10px 12px",
    borderRadius: 8,
    border: "2px solid transparent",
    cursor: "pointer",
    fontSize: 13,
    fontWeight: 500,
    fontFamily: "inherit",
    transition: "border-color 0.15s, background 0.15s",
  },
  providerBadge: {
    width: 12,
    height: 12,
    borderRadius: "50%",
    flexShrink: 0,
  },
  payBtn: {
    width: "100%",
    padding: "12px 16px",
    background: "#238636",
    border: "1px solid rgba(240,246,252,0.1)",
    borderRadius: 6,
    color: "#fff",
    fontSize: 16,
    fontWeight: 600,
    fontFamily: "inherit",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    gap: 8,
    transition: "background 0.15s",
  },
  errorBox: {
    background: "rgba(248,81,73,0.12)",
    border: "1px solid rgba(248,81,73,0.3)",
    borderRadius: 6,
    padding: "10px 14px",
    color: "#f85149",
    fontSize: 13,
    textAlign: "center",
  },
  resultGrid: {
    width: "100%",
    display: "flex",
    flexDirection: "column",
    gap: 8,
  },
  resultRow: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "8px 12px",
    background: "rgba(255,255,255,0.03)",
    borderRadius: 6,
  },
  resultLabel: {
    color: "#8b949e",
    fontSize: 13,
  },
  resultValue: {
    color: "#e6edf3",
    fontSize: 14,
    fontWeight: 600,
  },
};
