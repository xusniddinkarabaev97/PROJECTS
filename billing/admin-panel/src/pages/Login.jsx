import { useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { useTranslation } from "../i18n/LanguageContext";
import { api } from "../api/client";

export default function Login() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
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
        { username: data.username, fullName: data.fullName, role: data.role },
        data.token,
      );
      window.location.hash = "#/";
    } catch (err) {
      setError(err.message || t("loginError"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#0d1117]">
      <div className="bg-[#161b22] border border-[#30363d] rounded-lg p-8 w-full max-w-sm">
        <h1 className="text-xl font-bold text-[#c9d1d9] text-center mb-6">
          GZS Admin
        </h1>
        <h2 className="text-sm text-[#8b949e] text-center mb-6">
          {t("login")}
        </h2>
        {error && (
          <div className="bg-[#490202] border border-[#f85149] text-[#f85149] text-sm rounded p-3 mb-4">
            {error}
          </div>
        )}
        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label className="block text-xs text-[#8b949e] mb-1">
              {t("username")}
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="w-full px-3 py-2 bg-[#0d1117] border border-[#30363d] rounded text-[#c9d1d9] text-sm focus:outline-none focus:border-[#58a6ff]"
              placeholder="admin"
              autoFocus
            />
          </div>
          <div className="mb-6">
            <label className="block text-xs text-[#8b949e] mb-1">
              {t("password")}
            </label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-3 py-2 bg-[#0d1117] border border-[#30363d] rounded text-[#c9d1d9] text-sm focus:outline-none focus:border-[#58a6ff]"
              placeholder="admin123!"
            />
          </div>
          <button
            type="submit"
            disabled={loading}
            className="w-full py-2 bg-[#238636] hover:bg-[#2ea043] text-white rounded text-sm font-medium disabled:opacity-50 transition-colors"
          >
            {loading ? t("loading") : t("login")}
          </button>
        </form>
        <div className="mt-4 pt-4 border-t border-[#30363d] text-center">
          <p className="text-xs text-[#484f58]">
            Default: <span className="text-[#8b949e]">admin / admin123!</span>
          </p>
          <a
            href="http://localhost:5036/swagger"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-block mt-2 text-xs text-[#58a6ff] hover:underline"
          >
            API Swagger →
          </a>
        </div>
      </div>
    </div>
  );
}
