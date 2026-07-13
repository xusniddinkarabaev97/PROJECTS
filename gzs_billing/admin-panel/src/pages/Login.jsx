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
    if (!username.trim() || !password.trim()) { setError(t("loginError")); return; }
    setLoading(true);
    try {
      const data = await api.login(username, password);
      login({ username: data.username, fullName: data.fullName, role: data.role }, data.token);
      window.location.hash = "#/";
    } catch (err) {
      setError(err.message || t("loginError"));
    } finally { setLoading(false); }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#11161d] p-4">
      <div className="w-full max-w-sm">
        <div className="text-center mb-8">
          <div className="inline-flex w-14 h-14 rounded-2xl bg-gradient-to-tr from-teal-500 to-emerald-400 items-center justify-center text-slate-900 font-bold text-2xl mb-4">GZS</div>
          <h1 className="text-2xl font-bold text-white">GZS Billing</h1>
          <p className="text-sm text-slate-400 mt-2">{t("login")}</p>
        </div>
        <form onSubmit={handleSubmit} className="bg-[#161c24]/60 backdrop-blur-md border border-slate-800 rounded-2xl p-6 space-y-4">
          {error && <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 text-sm px-4 py-3 rounded-xl">{error}</div>}
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">{t("username")}</label>
            <input type="text" className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-xl text-white text-sm outline-none focus:border-teal-500/50" value={username} onChange={(e) => setUsername(e.target.value)} autoFocus />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-400 mb-1.5">{t("password")}</label>
            <input type="password" className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-xl text-white text-sm outline-none focus:border-teal-500/50" value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>
          <button type="submit" disabled={loading} className="w-full py-2.5 rounded-xl bg-gradient-to-r from-teal-500 to-emerald-500 text-slate-900 font-semibold text-sm hover:opacity-90 transition cursor-pointer disabled:opacity-50">
            {loading ? t("loading") : t("login")}
          </button>
        </form>
      </div>
    </div>
  );
}
