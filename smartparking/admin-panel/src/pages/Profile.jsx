import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { useTranslation } from "../i18n/LanguageContext";

export default function Profile() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState(null);
  const [error, setError] = useState(null);

  const companyId = user?.companyId;

  const fetchProfile = useCallback(async () => {
    if (!companyId) return;
    setLoading(true);
    try {
      const res = await api.getCompany(companyId);
      setProfile(res);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [companyId]);

  useEffect(() => {
    fetchProfile();
  }, [fetchProfile]);

  const handleSave = async () => {
    if (!profile) return;
    setSaving(true);
    setMessage(null);
    setError(null);
    try {
      await api.updateCompany(companyId, profile);
      setMessage(t("saved"));
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
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

  if (!profile) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon">🏢</div>
        <p>{t("noProfile")}</p>
      </div>
    );
  }

  return (
    <div>
      <div className="card-header">
        <h2 style={{ fontSize: 24, fontWeight: 700, color: "var(--text-primary)" }}>
          👤 {t("profile")}
        </h2>
      </div>

      {message && (
        <div
          style={{
            background: "var(--success-bg)",
            border: "1px solid var(--success)",
            color: "var(--success)",
            padding: "12px 16px",
            borderRadius: 8,
            marginBottom: 16,
          }}
        >
          ✅ {message}
        </div>
      )}

      {error && (
        <div
          style={{
            background: "var(--danger-bg)",
            border: "1px solid var(--danger)",
            color: "var(--danger)",
            padding: "12px 16px",
            borderRadius: 8,
            marginBottom: 16,
            display: "flex",
            justifyContent: "space-between",
          }}
        >
          <span>{error}</span>
          <button
            onClick={() => setError(null)}
            style={{
              color: "var(--danger)",
              fontWeight: 700,
              cursor: "pointer",
              background: "none",
              border: "none",
            }}
          >
            ×
          </button>
        </div>
      )}

      <div className="card" style={{ maxWidth: 600 }}>
        <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
          <div>
            <label
              style={{
                display: "block",
                marginBottom: 6,
                fontSize: 13,
                fontWeight: 500,
                color: "var(--text-secondary)",
              }}
            >
              🏢 {t("companyName")}
            </label>
            <input
              className="input"
              value={profile.name || ""}
              onChange={(e) => setProfile({ ...profile, name: e.target.value })}
            />
          </div>

          <div>
            <label
              style={{
                display: "block",
                marginBottom: 6,
                fontSize: 13,
                fontWeight: 500,
                color: "var(--text-secondary)",
              }}
            >
              📧 {t("email")}
            </label>
            <input
              className="input"
              type="email"
              value={profile.email || ""}
              onChange={(e) => setProfile({ ...profile, email: e.target.value })}
            />
          </div>

          <div>
            <label
              style={{
                display: "block",
                marginBottom: 6,
                fontSize: 13,
                fontWeight: 500,
                color: "var(--text-secondary)",
              }}
            >
              📋 {t("inn")}
            </label>
            <input
              className="input"
              value={profile.inn || ""}
              onChange={(e) => setProfile({ ...profile, inn: e.target.value })}
            />
          </div>

          <div>
            <label
              style={{
                display: "block",
                marginBottom: 6,
                fontSize: 13,
                fontWeight: 500,
                color: "var(--text-secondary)",
              }}
            >
              📍 {t("address")}
            </label>
            <input
              className="input"
              value={profile.address || ""}
              onChange={(e) => setProfile({ ...profile, address: e.target.value })}
            />
          </div>

          <div style={{ display: "flex", gap: 12 }}>
            <div style={{ flex: 1 }}>
              <label
                style={{
                  display: "block",
                  marginBottom: 6,
                  fontSize: 13,
                  fontWeight: 500,
                  color: "var(--text-secondary)",
                }}
              >
                📞 {t("phone")}
              </label>
              <input
                className="input"
                value={profile.phone || ""}
                onChange={(e) => setProfile({ ...profile, phone: e.target.value })}
              />
            </div>
          </div>

          <div>
            <label
              style={{
                display: "block",
                marginBottom: 6,
                fontSize: 13,
                fontWeight: 500,
                color: "var(--text-secondary)",
              }}
            >
              🔑 {t("password")}
            </label>
            <input
              className="input"
              type="password"
              value={profile.jwtAuthToken || ""}
              onChange={(e) =>
                setProfile({ ...profile, jwtAuthToken: e.target.value })
              }
            />
          </div>

          <div style={{ marginTop: 8 }}>
            <button
              className="btn btn-primary"
              onClick={handleSave}
              disabled={saving}
              style={{ justifyContent: "center", minWidth: 140 }}
            >
              {saving ? t("saving") : t("save")}
            </button>
          </div>
        </div>
      </div>

      <div className="card" style={{ maxWidth: 600, marginTop: 16 }}>
        <h4 style={{ marginBottom: 12, fontSize: 14, color: "var(--text-secondary)" }}>
          ℹ️ {t("companyInfo")}
        </h4>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, fontSize: 13 }}>
          <div>
            <span style={{ color: "var(--text-muted)" }}>ID:</span>{" "}
            <span style={{ fontFamily: "monospace" }}>#{profile.id}</span>
          </div>
          <div>
            <span style={{ color: "var(--text-muted)" }}>{t("createdAt")}:</span>{" "}
            {profile.createdAt ? new Date(profile.createdAt).toLocaleDateString() : "—"}
          </div>
        </div>
      </div>
    </div>
  );
}
