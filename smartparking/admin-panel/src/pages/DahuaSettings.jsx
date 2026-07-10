import { useState, useEffect } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function DahuaSettings() {
  const { t } = useTranslation();
  const [settings, setSettings] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [message, setMessage] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    api.getDahuaSettings()
      .then(setSettings)
      .catch(() => setSettings({
        serverUrl: "", username: "", password: "", webhookSecret: "",
        hourlyRate: 5000, gracePeriodMinutes: 15, maxDailyRate: null,
        autoOpenForWhitelist: true, barrierControlEnabled: true,
      }))
      .finally(() => setLoading(false));
  }, []);

  const handleSave = async () => {
    setSaving(true);
    setMessage(null);
    setError(null);
    try {
      await api.saveDahuaSettings(settings);
      setMessage(t("saved"));
      setTimeout(() => setMessage(null), 3000);
    } catch (e) {
      setError(e.message);
    } finally {
      setSaving(false);
    }
  };

  const handleTest = async () => {
    setTesting(true);
    setMessage(null);
    setError(null);
    try {
      await api.testDahuaConnection(settings);
      setMessage("✅ " + t("connectionOk"));
    } catch (e) {
      setError(t("connectionFailed") + ": " + e.message);
    } finally {
      setTesting(false);
    }
  };

  if (loading) {
    return (
      <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}>
        <div className="spinner" />
        <span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span>
      </div>
    );
  }

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>
          🔌 {t("dahuaIntegration")}
        </h2>
      </div>

      {message && (
        <div style={{ background: "var(--success-bg)", border: "1px solid var(--success)", color: "var(--success)", padding: "12px 16px", borderRadius: 8, marginBottom: 16 }}>
          {message}
        </div>
      )}
      {error && (
        <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: "12px 16px", borderRadius: 8, marginBottom: 16, display: "flex", justifyContent: "space-between" }}>
          <span>{error}</span>
          <button onClick={() => setError(null)} style={{ color: "var(--danger)", fontWeight: 700, cursor: "pointer", background: "none", border: "none" }}>×</button>
        </div>
      )}

      <div className="card" style={{ maxWidth: 700, marginBottom: 20 }}>
        <h3 style={{ marginBottom: 16, fontWeight: 600 }}>🌐 {t("dssConnection")}</h3>
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          <div>
            <label style={{ display: "block", marginBottom: 4, fontSize: 13, color: "var(--text-secondary)" }}>{t("dssServerUrl")}</label>
            <input className="input" placeholder="https://192.168.1.100:443" value={settings?.serverUrl || ""} onChange={(e) => setSettings({ ...settings, serverUrl: e.target.value })} />
          </div>
          <div style={{ display: "flex", gap: 12 }}>
            <div style={{ flex: 1 }}>
              <label style={{ display: "block", marginBottom: 4, fontSize: 13, color: "var(--text-secondary)" }}>{t("username")}</label>
              <input className="input" value={settings?.username || ""} onChange={(e) => setSettings({ ...settings, username: e.target.value })} />
            </div>
            <div style={{ flex: 1 }}>
              <label style={{ display: "block", marginBottom: 4, fontSize: 13, color: "var(--text-secondary)" }}>{t("password")}</label>
              <input className="input" type="password" value={settings?.password || ""} onChange={(e) => setSettings({ ...settings, password: e.target.value })} />
            </div>
          </div>
          <div>
            <label style={{ display: "block", marginBottom: 4, fontSize: 13, color: "var(--text-secondary)" }}>{t("webhookSecret")}</label>
            <input className="input" value={settings?.webhookSecret || ""} onChange={(e) => setSettings({ ...settings, webhookSecret: e.target.value })} />
          </div>
          <div style={{ display: "flex", gap: 8 }}>
            <button className="btn btn-primary" onClick={handleSave} disabled={saving} style={{ justifyContent: "center" }}>
              {saving ? t("saving") : t("save")}
            </button>
            <button className="btn btn-ghost" onClick={handleTest} disabled={testing} style={{ justifyContent: "center" }}>
              {testing ? "..." : "🔍 " + t("testConnection")}
            </button>
          </div>
        </div>
      </div>

      <div className="card" style={{ maxWidth: 700, marginBottom: 20 }}>
        <h3 style={{ marginBottom: 16, fontWeight: 600 }}>💰 {t("tariffSettings")}</h3>
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          <div style={{ display: "flex", gap: 12 }}>
            <div style={{ flex: 1 }}>
              <label style={{ display: "block", marginBottom: 4, fontSize: 13, color: "var(--text-secondary)" }}>{t("hourlyRate")} (UZS)</label>
              <input className="input" type="number" value={settings?.hourlyRate || 0} onChange={(e) => setSettings({ ...settings, hourlyRate: parseFloat(e.target.value) || 0 })} />
            </div>
            <div style={{ flex: 1 }}>
              <label style={{ display: "block", marginBottom: 4, fontSize: 13, color: "var(--text-secondary)" }}>{t("gracePeriodMinutes")}</label>
              <input className="input" type="number" value={settings?.gracePeriodMinutes || 0} onChange={(e) => setSettings({ ...settings, gracePeriodMinutes: parseInt(e.target.value) || 0 })} />
            </div>
            <div style={{ flex: 1 }}>
              <label style={{ display: "block", marginBottom: 4, fontSize: 13, color: "var(--text-secondary)" }}>{t("maxDailyRate")}</label>
              <input className="input" type="number" placeholder="0 = unlimited" value={settings?.maxDailyRate || ""} onChange={(e) => setSettings({ ...settings, maxDailyRate: e.target.value ? parseFloat(e.target.value) : null })} />
            </div>
          </div>
          <div style={{ display: "flex", gap: 24 }}>
            <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, cursor: "pointer" }}>
              <input type="checkbox" checked={settings?.autoOpenForWhitelist ?? true} onChange={(e) => setSettings({ ...settings, autoOpenForWhitelist: e.target.checked })} />
              {t("autoOpenForWhitelist")}
            </label>
            <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, cursor: "pointer" }}>
              <input type="checkbox" checked={settings?.barrierControlEnabled ?? true} onChange={(e) => setSettings({ ...settings, barrierControlEnabled: e.target.checked })} />
              {t("barrierControlEnabled")}
            </label>
          </div>
        </div>
      </div>

      <div className="card" style={{ maxWidth: 700 }}>
        <h3 style={{ marginBottom: 12, fontWeight: 600 }}>📡 {t("webhookUrl")}</h3>
        <div style={{ background: "var(--bg-primary)", padding: 12, borderRadius: 8, fontFamily: "monospace", fontSize: 12, color: "var(--info)", wordBreak: "break-all" }}>
          POST {window.location.origin}/api/DahuaIntegration/events
        </div>
        <p style={{ fontSize: 12, color: "var(--text-muted)", marginTop: 8 }}>
          {t("webhookHint")}
        </p>
      </div>
    </div>
  );
}
