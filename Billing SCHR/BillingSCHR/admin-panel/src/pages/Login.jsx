import { useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { useTranslation } from "../i18n/LanguageContext";
import { api } from "../api/client";

const styles = {
  wrapper: {
    minHeight: "100vh",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    background: "linear-gradient(135deg, #0d1410 0%, #162218 50%, #0f1712 100%)",
    position: "relative",
    overflow: "hidden",
  },
  wrapperBefore: {
    content: '""',
    position: "absolute",
    inset: 0,
    background: "radial-gradient(circle at 50% 50%, rgba(91,140,62,0.06) 0%, transparent 60%)",
    pointerEvents: "none",
  },
  card: {
    background: "rgba(18, 28, 20, 0.80)",
    backdropFilter: "blur(16px)",
    WebkitBackdropFilter: "blur(16px)",
    border: "1px solid rgba(91, 140, 62, 0.25)",
    borderRadius: 20,
    boxShadow: "0 20px 50px rgba(0,0,0,0.5), 0 0 25px rgba(91,140,62,0.08)",
    width: "100%",
    maxWidth: 440,
    padding: "2.5rem 2rem",
  },
  headerTitle: {
    fontFamily: "'Montserrat', sans-serif",
    fontWeight: 700,
    fontSize: "1.1rem",
    color: "#ffffff",
    letterSpacing: "0.5px",
    textTransform: "uppercase",
    marginBottom: 4,
  },
  headerSubtitle: {
    fontSize: "0.78rem",
    color: "#5b8c3e",
    fontWeight: 600,
    letterSpacing: "1px",
  },
  badge: {
    display: "inline-block",
    background: "rgba(91, 140, 62, 0.18)",
    color: "#6da34a",
    border: "1px solid rgba(91, 140, 62, 0.35)",
    fontSize: "0.75rem",
    fontWeight: 600,
    padding: "4px 12px",
    borderRadius: 50,
    marginTop: 10,
  },
  label: {
    fontSize: "0.85rem",
    fontWeight: 500,
    color: "#a3b89b",
    marginBottom: 6,
    display: "block",
  },
  inputGroup: {
    display: "flex",
    marginBottom: 16,
  },
  inputIcon: {
    background: "rgba(255,255,255,0.04)",
    border: "1px solid rgba(255,255,255,0.12)",
    borderRight: "none",
    color: "#8a9e84",
    display: "flex",
    alignItems: "center",
    padding: "0 14px",
    borderTopLeftRadius: 10,
    borderBottomLeftRadius: 10,
    fontSize: "1.1rem",
  },
  input: {
    background: "rgba(255,255,255,0.04)",
    border: "1px solid rgba(255,255,255,0.12)",
    borderLeft: "none",
    color: "#ffffff",
    padding: "0.75rem 1rem",
    fontSize: "0.95rem",
    flex: 1,
    outline: "none",
    transition: "border-color 0.2s",
    width: "100%",
    boxSizing: "border-box",
  },
  inputRight: {
    borderTopRightRadius: 10,
    borderBottomRightRadius: 10,
  },
  inputNoRightRadius: {
    borderTopRightRadius: 0,
    borderBottomRightRadius: 0,
    borderRight: "none",
  },
  toggleBtn: {
    cursor: "pointer",
    background: "rgba(255,255,255,0.04)",
    border: "1px solid rgba(255,255,255,0.12)",
    borderLeft: "none",
    color: "#8a9e84",
    display: "flex",
    alignItems: "center",
    padding: "0 14px",
    borderTopRightRadius: 10,
    borderBottomRightRadius: 10,
    fontSize: "1rem",
    transition: "color 0.2s",
  },
  btn: {
    background: "linear-gradient(135deg, #5b8c3e 0%, #3d6b25 100%)",
    border: "none",
    color: "#ffffff",
    fontWeight: 700,
    fontSize: "0.95rem",
    padding: "0.85rem",
    borderRadius: 10,
    letterSpacing: "0.5px",
    cursor: "pointer",
    width: "100%",
    boxShadow: "0 4px 15px rgba(91,140,62,0.35)",
    transition: "all 0.3s ease",
  },
  forgotLink: {
    textDecoration: "none",
    fontSize: "0.78rem",
    color: "#8a9e84",
  },
  checkbox: {
    accentColor: "#5b8c3e",
    marginRight: 8,
  },
  checkboxLabel: {
    fontSize: "0.82rem",
    color: "#a3b89b",
  },
  footer: {
    fontSize: "0.75rem",
    color: "#556652",
    textAlign: "center",
    marginTop: "1.5rem",
  },
  errorBox: {
    background: "rgba(239, 68, 68, 0.12)",
    border: "1px solid rgba(239, 68, 68, 0.35)",
    color: "#f87171",
    fontSize: 13,
    padding: "10px 14px",
    borderRadius: 8,
    marginBottom: 16,
  },
  flexBetween: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 4,
  },
};

export default function Login() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    if (!username.trim() || !password.trim()) {
      setError(t("loginError"));
      return;
    }
    setLoading(true);
    try {
      const data = await api.login(username, password);
      login(
        { login: data.login || username, role: data.role || "admin" },
        data.accessToken || data.token
      );
      window.location.hash = "#/";
    } catch (err) {
      setError(err.message || t("loginError"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.wrapper}>
      <div style={styles.wrapperBefore} />

      <div style={{ padding: "0 1rem", width: "100%", maxWidth: 440 }}>
        <div style={styles.card}>
          {/* Header */}
          <div style={{ textAlign: "center", marginBottom: 24 }}>
            <div style={{ marginBottom: 12, fontSize: 48, lineHeight: 1 }}>🎖️</div>
            <div style={styles.headerTitle}>BillingSCHR</div>
            <div style={styles.headerSubtitle}>
              МОБИЛИЗАЦИОННЫЙ ПРИЗЫВНОЙ РЕЗЕРВ
            </div>
            <span style={styles.badge}>💰 БИЛЛИНГ МПР</span>
          </div>

          {/* Error */}
          {error && <div style={styles.errorBox}>{error}</div>}

          {/* Form */}
          <form onSubmit={handleSubmit}>
            {/* Login */}
            <div>
              <label style={styles.label} htmlFor="username">
                {t("username")}
              </label>
              <div style={styles.inputGroup}>
                <span style={styles.inputIcon}>👤</span>
                <input
                  id="username"
                  type="text"
                  style={{ ...styles.input, ...styles.inputRight }}
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  placeholder="admin"
                  autoFocus
                />
              </div>
            </div>

            {/* Password */}
            <div>
              <div style={styles.flexBetween}>
                <label style={{ ...styles.label, marginBottom: 0 }} htmlFor="password">
                  {t("password")}
                </label>
                <a href="#" style={styles.forgotLink}>
                  {t("forgotPassword")}
                </a>
              </div>
              <div style={styles.inputGroup}>
                <span style={styles.inputIcon}>🔒</span>
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  style={styles.inputNoRightRadius}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                />
                <span
                  style={styles.toggleBtn}
                  onClick={() => setShowPassword(!showPassword)}
                  onMouseEnter={(e) => (e.currentTarget.style.color = "#fff")}
                  onMouseLeave={(e) => (e.currentTarget.style.color = "#8a9e84")}
                  title={showPassword ? "Скрыть пароль" : "Показать пароль"}
                >
                  {showPassword ? "🙈" : "👁️"}
                </span>
              </div>
            </div>

            {/* Remember me */}
            <div style={{ marginBottom: 24 }}>
              <label style={{ display: "flex", alignItems: "center", cursor: "pointer" }}>
                <input
                  type="checkbox"
                  style={styles.checkbox}
                  defaultChecked
                />
                <span style={styles.checkboxLabel}>{t("rememberMe")}</span>
              </label>
            </div>

            {/* Submit */}
            <button
              type="submit"
              disabled={loading}
              style={{
                ...styles.btn,
                opacity: loading ? 0.7 : 1,
                cursor: loading ? "wait" : "pointer",
              }}
            >
              {loading ? t("loading") : `🔑 ${t("login")}`}
            </button>
          </form>

          {/* Footer */}
          <div style={styles.footer}>
            🔐 {t("secureConnection")} • МПР МО РУз
          </div>
        </div>
      </div>
    </div>
  );
}
