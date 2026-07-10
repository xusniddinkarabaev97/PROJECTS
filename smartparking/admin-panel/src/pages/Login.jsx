import { useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { useTranslation } from "../i18n/LanguageContext";
import { api } from "../api/client";

export default function Login() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    if (!email.trim() || !password.trim()) {
      setError(t("loginError"));
      return;
    }
    setLoading(true);
    try {
      const data = await api.login(email, password);
      login(
        {
          email: data.email || email,
          role: data.role || "admin",
          companyId: data.companyId,
        },
        data.accessToken || data.token,
      );
      window.location.hash = "#/";
    } catch (err) {
      setError(err.message || t("loginError"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      className="min-h-screen flex items-center justify-center"
      style={{ background: "var(--bg-primary)" }}
    >
      <div className="card" style={{ width: "100%", maxWidth: 400 }}>
        <div style={{ textAlign: "center", marginBottom: 24 }}>
          <span style={{ fontSize: 48 }}>🅿️</span>
          <h1
            style={{
              fontSize: 22,
              fontWeight: 700,
              color: "var(--text-primary)",
              marginTop: 8,
            }}
          >
            SmartParking
          </h1>
          <p
            style={{
              fontSize: 13,
              color: "var(--text-secondary)",
              marginTop: 4,
            }}
          >
            {t("login")}
          </p>
        </div>
        {error && (
          <div
            style={{
              background: "var(--danger-bg)",
              border: "1px solid var(--danger)",
              color: "var(--danger)",
              fontSize: 13,
              padding: "10px 14px",
              borderRadius: 8,
              marginBottom: 16,
            }}
          >
            {error}
          </div>
        )}
        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: 16 }}>
            <label
              style={{
                display: "block",
                fontSize: 12,
                color: "var(--text-secondary)",
                marginBottom: 6,
              }}
            >
              {t("username")}
            </label>
            <input
              type="email"
              className="input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="company@example.com"
              autoFocus
            />
          </div>
          <div style={{ marginBottom: 24 }}>
            <label
              style={{
                display: "block",
                fontSize: 12,
                color: "var(--text-secondary)",
                marginBottom: 6,
              }}
            >
              {t("password")}
            </label>
            <input
              type="password"
              className="input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
            />
          </div>
          <button
            type="submit"
            disabled={loading}
            className="btn btn-primary"
            style={{
              width: "100%",
              justifyContent: "center",
              padding: "12px 0",
              fontSize: 15,
            }}
          >
            {loading ? t("loading") : t("login")}
          </button>
        </form>
        <div
          style={{
            marginTop: 16,
            paddingTop: 16,
            borderTop: "1px solid var(--border)",
            textAlign: "center",
          }}
        >
          <a
            href="http://localhost:5121/swagger"
            target="_blank"
            rel="noopener noreferrer"
            style={{
              fontSize: 12,
              color: "var(--accent)",
              textDecoration: "none",
            }}
          >
            {t("swagger")} →
          </a>
        </div>
      </div>
    </div>
  );
}
