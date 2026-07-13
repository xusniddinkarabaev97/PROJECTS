import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function Dashboard() {
  const { t } = useTranslation();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try { setData(await api.getDashboard()); } catch { setData(null); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchData(); window.lucide?.createIcons(); }, [fetchData]);

  if (loading) return <div className="flex items-center justify-center py-20 gap-3"><div className="spinner"></div><span className="text-slate-400">{t("loading")}</span></div>;

  const stats = [
    { label: t("totalStations"), value: data?.totalStations ?? 0, icon: "fuel", color: "text-teal-400", bg: "bg-teal-500/10" },
    { label: t("totalStakeholders"), value: data?.totalStakeholders ?? 0, icon: "users", color: "text-blue-400", bg: "bg-blue-500/10" },
    { label: t("totalTransactions"), value: data?.totalTransactions ?? 0, icon: "arrow-left-right", color: "text-amber-400", bg: "bg-amber-500/10" },
    { label: t("todayTransactions"), value: data?.todayTransactions ?? 0, icon: "calendar", color: "text-emerald-400", bg: "bg-emerald-500/10" },
    { label: t("todayRevenue"), value: `${(data?.todayRevenue ?? 0).toLocaleString()} UZS`, icon: "banknote", color: "text-rose-400", bg: "bg-rose-500/10" },
  ];

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-3xl font-bold tracking-tight text-white mb-1">{t("dashboard")}</h1>
        <p className="text-sm text-slate-400">{t("totalStations")} {data?.totalStations ?? 0}</p>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4 mb-8">
        {stats.map((s) => (
          <div key={s.label} className="bg-[#161c24]/60 backdrop-blur-md border border-slate-800 rounded-2xl p-5 hover:border-slate-700 transition">
            <div className="flex items-center justify-between mb-3">
              <span className={`w-10 h-10 rounded-xl ${s.bg} flex items-center justify-center`}>
                <i data-lucide={s.icon} className={`w-5 h-5 ${s.color}`}></i>
              </span>
            </div>
            <div className={`text-2xl font-bold ${s.color} mb-1`}>{s.value}</div>
            <div className="text-xs text-slate-400">{s.label}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
