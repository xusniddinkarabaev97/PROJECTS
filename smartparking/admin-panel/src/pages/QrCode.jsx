import { useState, useEffect, useRef, useCallback } from "react";
import "qrcodejs/qrcode.js"; // sets window.QRCode as global
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function QrCode() {
  const { t } = useTranslation();
  const [txData, setTxData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [txError, setTxError] = useState(null);
  const [qrType, setQrType] = useState("payment");
  const [qrSize, setQrSize] = useState(250);
  const [generatedUrl, setGeneratedUrl] = useState("");
  const [qrReady, setQrReady] = useState(false);
  const qrContainerRef = useRef(null);

  const [txnId, setTxnId] = useState("");
  const [amount, setAmount] = useState("");
  const [stationId, setStationId] = useState("");
  const [stationName, setStationName] = useState("");
  const [customUrl, setCustomUrl] = useState("");

  const fetchTx = useCallback(async () => {
    setLoading(true);
    setTxError(null);
    try {
      const res = await api.getTransactions();
      setTxData(Array.isArray(res) ? res.slice(0, 10) : []);
    } catch (err) {
      setTxError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchTx(); }, [fetchTx]);

  const generateQr = () => {
    let url = "";
    switch (qrType) {
      case "payment":
        url = `${window.location.origin}/Billing/PaymentPage/${txnId || "0"}`;
        break;
      case "station":
        url = `${window.location.origin}/Billing/PaymentPage/${stationId || "0"}`;
        break;
      case "custom":
        url = customUrl || window.location.origin;
        break;
      default:
        url = window.location.origin;
    }
    setGeneratedUrl(url);
    setQrReady(false);

    if (qrContainerRef.current && window.QRCode) {
      qrContainerRef.current.innerHTML = "";
      new window.QRCode(qrContainerRef.current, {
        text: url,
        width: qrSize,
        height: qrSize,
        colorDark: "#000000",
        colorLight: "#ffffff",
        correctLevel: window.QRCode.CorrectLevel.M,
      });
      setQrReady(true);
    }
  };

  const downloadQr = () => {
    const container = qrContainerRef.current;
    if (!container) return;
    // try canvas first, fallback to img
    const canvas = container.querySelector("canvas");
    const img = container.querySelector("img");
    if (canvas) {
      const link = document.createElement("a");
      link.download = "billinggaz-qr.png";
      link.href = canvas.toDataURL("image/png");
      link.click();
    } else if (img) {
      const link = document.createElement("a");
      link.download = "billinggaz-qr.png";
      link.href = img.src;
      link.click();
    }
  };

  const quickGenerate = (tx) => {
    setQrType("payment");
    setTxnId(String(tx.id));
    setAmount(String(tx.totalSum || 0));
    setTimeout(generateQr, 150);
  };

  const formatAmount = (v) => {
    if (v == null) return "—";
    return Number(v).toLocaleString() + " UZS";
  };

  return (
    <div>
      <h2 style={{ fontSize: 24, fontWeight: 700, marginBottom: 24, color: "var(--text-primary)" }}>
        📱 {t("qrcode")}
      </h2>

      {/* Generator + Preview */}
      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginBottom: 24 }}>
        {/* Generator */}
        <div className="card">
          <h3 style={{ fontSize: 16, fontWeight: 600, marginBottom: 16, color: "var(--text-primary)" }}>
            {t("generateQr")}
          </h3>
          <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
            <div>
              <label style={{ display: "block", fontSize: 12, color: "var(--text-secondary)", marginBottom: 6 }}>{t("qrType")}</label>
              <select className="input" value={qrType} onChange={(e) => setQrType(e.target.value)}>
                <option value="payment">🅿️ {t("qrPayment")}</option>
                <option value="station">⛽ {t("qrStation")}</option>
                <option value="custom">🔗 {t("qrCustom")}</option>
              </select>
            </div>

            {qrType === "payment" && (
              <>
                <input className="input" placeholder="ID" type="number" value={txnId} onChange={(e) => setTxnId(e.target.value)} />
                <input className="input" placeholder={t("amount")} type="number" value={amount} onChange={(e) => setAmount(e.target.value)} />
              </>
            )}
            {qrType === "station" && (
              <>
                <input className="input" placeholder="ID" type="number" value={stationId} onChange={(e) => setStationId(e.target.value)} />
                <input className="input" placeholder={t("name")} value={stationName} onChange={(e) => setStationName(e.target.value)} />
              </>
            )}
            {qrType === "custom" && (
              <input className="input" placeholder="https://..." value={customUrl} onChange={(e) => setCustomUrl(e.target.value)} />
            )}

            <div>
              <label style={{ display: "block", fontSize: 12, color: "var(--text-secondary)", marginBottom: 6 }}>
                {t("qrSize")}: {qrSize}px
              </label>
              <input
                type="range" min="150" max="400" step="10" value={qrSize}
                onChange={(e) => setQrSize(Number(e.target.value))}
                style={{ width: "100%", accentColor: "var(--accent)" }}
              />
            </div>

            <button className="btn btn-primary" style={{ justifyContent: "center", padding: "12px 0" }} onClick={generateQr}>
              {t("generateQr")}
            </button>
          </div>
        </div>

        {/* Preview */}
        <div className="card" style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", minHeight: 300 }}>
          <h3 style={{ fontSize: 16, fontWeight: 600, marginBottom: 16, color: "var(--text-primary)" }}>{t("preview")}</h3>
          {generatedUrl ? (
            <>
              <div ref={qrContainerRef} className="qr-preview" />
              <div style={{ marginTop: 12, textAlign: "center" }}>
                <p style={{ fontSize: 11, color: "var(--text-muted)", marginBottom: 4 }}>{t("encodedUrl")}:</p>
                <code style={{ fontSize: 11, color: "var(--text-secondary)", wordBreak: "break-all" }}>{generatedUrl}</code>
              </div>
              {qrReady && (
                <button className="btn btn-success btn-sm" style={{ marginTop: 12 }} onClick={downloadQr}>
                  💾 {t("download")}
                </button>
              )}
            </>
          ) : (
            <div style={{ textAlign: "center", color: "var(--text-muted)" }}>
              <span style={{ fontSize: 64 }}>📱</span>
              <p style={{ marginTop: 12 }}>{t("generateQr")}</p>
            </div>
          )}
        </div>
      </div>

      {/* Tx list */}
      <div className="card" style={{ padding: 0, overflow: "hidden" }}>
        <div style={{ padding: "16px 24px", borderBottom: "1px solid var(--border)" }}>
          <h3 style={{ fontSize: 16, fontWeight: 600, color: "var(--text-primary)" }}>
            {t("transactions")}
          </h3>
        </div>
        {txError && (
          <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: "10px 16px", display: "flex", justifyContent: "space-between" }}>
            <span>{txError}</span>
            <button onClick={fetchTx} style={{ color: "var(--danger)", fontWeight: 700, cursor: "pointer", background: "none", border: "none" }}>🔄</button>
          </div>
        )}
        {loading ? (
          <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 40, gap: 12 }}>
            <div className="spinner" /><span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
          </div>
        ) : txData.length === 0 ? (
          <div className="empty-state"><p>{t("noTransactions")}</p></div>
        ) : (
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead>
                <tr><th>ID</th><th>{t("name") || "Client"}</th><th>{t("amount")}</th><th>{t("paymentStatus")}</th><th>{t("actions")}</th></tr>
              </thead>
              <tbody>
                {txData.map((tx) => {
                  const st = {
                    Completed: { label: "Completed", className: "badge-success" },
                    Paid: { label: "Paid", className: "badge-success" },
                    New: { label: "New", className: "badge-warning" },
                    Failed: { label: "Failed", className: "badge-danger" },
                    Cancelled: { label: "Cancelled", className: "badge-danger" },
                  }[tx.paymentStatus] || { label: tx.paymentStatus || "—", className: "badge-info" };
                  return (
                    <tr key={tx.id}>
                      <td style={{ color: "var(--text-muted)", fontSize: 12 }}>#{tx.id}</td>
                      <td style={{ fontWeight: 600 }}>{tx.client?.fullName || `#${tx.clientId}` || "—"}</td>
                      <td style={{ fontWeight: 600 }}>{formatAmount(tx.totalSum)}</td>
                      <td><span className={`badge ${st.className}`}>{st.label}</span></td>
                      <td>
                        <button className="btn btn-ghost btn-sm" onClick={() => quickGenerate(tx)}>📱 QR</button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
