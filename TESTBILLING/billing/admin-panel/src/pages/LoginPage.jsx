import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "../i18n/LanguageContext";

export default function LoginPage({ onLoginSuccess }) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(e) {
    e.preventDefault();
    setError("");

    if (!username.trim() || !password.trim()) {
      setError(t("login.invalidCredentials"));
      return;
    }

    setLoading(true);

    try {
      const res = await fetch("/api/v1/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username: username.trim(), password }),
      });

      let data;
      try {
        data = await res.json();
      } catch {
        throw new Error(t("login.invalidCredentials"));
      }

      if (!res.ok) {
        throw new Error(
          data?.message || data?.error || t("login.invalidCredentials"),
        );
      }

      if (data.token) {
        localStorage.setItem("gzs_billing_token", data.token);
      }

      const user = data.user || data;
      if (user) {
        localStorage.setItem("gzs_billing_user", JSON.stringify(user));
      }

      if (onLoginSuccess) {
        onLoginSuccess();
      }

      navigate("/", { replace: true });
    } catch (err) {
      setError(err.message || t("login.invalidCredentials"));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={styles.wrapper}>
      <div style={styles.card}>
        <div style={styles.logo}>GZS BILLING</div>
        <h2 style={styles.title}>{t("login.title")}</h2>

        <form onSubmit={handleSubmit} style={styles.form}>
          <div style={styles.inputGroup}>
            <span style={styles.inputIcon}>
              <svg
                width="16"
                height="16"
                viewBox="0 0 16 16"
                fill="currentColor"
              >
                <path d="M8 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm0 1.5c-2.675 0-5.175 1.112-6.293 3.293-.26.507-.22 1.116.098 1.563C2.24 14.94 2.91 15.5 4 15.5h8c1.09 0 1.76-.56 2.195-1.144.318-.447.358-1.056.098-1.563C13.175 10.612 10.675 9.5 8 9.5Z" />
              </svg>
            </span>
            <input
              style={styles.input}
              type="text"
              placeholder={t("login.username")}
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              autoComplete="username"
              autoFocus
            />
          </div>

          <div style={styles.inputGroup}>
            <span style={styles.inputIcon}>
              <svg
                width="16"
                height="16"
                viewBox="0 0 16 16"
                fill="currentColor"
              >
                <path d="M4 4v2H2V4h2Zm1 7V9h6v2H5Zm2-7v2h6V4H7Zm4 7v2h2v-2h-2ZM5 6.75a.75.75 0 0 0-.75.75v4c0 .414.336.75.75.75h6a.75.75 0 0 0 .75-.75v-4a.75.75 0 0 0-.75-.75H5Z" />
                <path d="M2.5 1A1.5 1.5 0 0 0 1 2.5v8.593A1.5 1.5 0 0 0 1.883 12.3l4.836 2.9a1.5 1.5 0 0 0 1.562 0l4.836-2.9A1.5 1.5 0 0 0 14 11.093V2.5A1.5 1.5 0 0 0 12.5 1h-10Z" />
              </svg>
            </span>
            <input
              style={styles.input}
              type="password"
              placeholder={t("login.password")}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            style={{
              ...styles.button,
              ...(loading ? styles.buttonDisabled : {}),
            }}
          >
            {loading ? (
              <>
                <span style={styles.spinner} />
                {t("login.loggingIn")}
              </>
            ) : (
              t("login.submit")
            )}
          </button>

          {error && <div style={styles.error}>{error}</div>}
        </form>
      </div>
    </div>
  );
}

const styles = {
  wrapper: {
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    minHeight: "100vh",
    background: "#0d1117",
    padding: "24px",
  },
  card: {
    width: "100%",
    maxWidth: "400px",
    background: "#161b22",
    border: "1px solid #30363d",
    borderRadius: "12px",
    padding: "40px 32px",
    boxShadow: "0 8px 24px rgba(0,0,0,0.4)",
  },
  logo: {
    textAlign: "center",
    fontSize: "1.75rem",
    fontWeight: "700",
    letterSpacing: "3px",
    color: "#58a6ff",
    marginBottom: "24px",
    userSelect: "none",
  },
  title: {
    textAlign: "center",
    fontSize: "1.15rem",
    fontWeight: "500",
    color: "#8b949e",
    marginBottom: "28px",
  },
  form: {
    display: "flex",
    flexDirection: "column",
    gap: "16px",
  },
  inputGroup: {
    position: "relative",
    display: "flex",
    alignItems: "center",
  },
  inputIcon: {
    position: "absolute",
    left: "12px",
    color: "#6e7681",
    display: "flex",
    alignItems: "center",
    pointerEvents: "none",
  },
  input: {
    width: "100%",
    padding: "10px 12px 10px 38px",
    background: "#0d1117",
    border: "1px solid #30363d",
    borderRadius: "6px",
    color: "#c9d1d9",
    fontSize: "0.9rem",
    fontFamily: "inherit",
    outline: "none",
    transition: "border-color 0.2s ease, box-shadow 0.2s ease",
  },
  button: {
    width: "100%",
    padding: "12px 16px",
    background: "#238636",
    border: "1px solid #2ea043",
    borderRadius: "6px",
    color: "#ffffff",
    fontSize: "0.95rem",
    fontWeight: "600",
    cursor: "pointer",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    gap: "8px",
    transition: "background 0.2s ease",
    fontFamily: "inherit",
  },
  buttonDisabled: {
    opacity: 0.6,
    cursor: "not-allowed",
  },
  spinner: {
    display: "inline-block",
    width: "18px",
    height: "18px",
    border: "2px solid rgba(255,255,255,0.25)",
    borderTopColor: "#ffffff",
    borderRadius: "50%",
    animation: "spin 0.6s linear infinite",
  },
  error: {
    background: "rgba(248,81,73,0.1)",
    border: "1px solid rgba(248,81,73,0.3)",
    borderRadius: "6px",
    padding: "10px 14px",
    color: "#f85149",
    fontSize: "0.85rem",
    textAlign: "center",
  },
};
