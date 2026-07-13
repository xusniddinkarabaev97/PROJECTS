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

  if (loading) return (
    <div className="flex items-center justify-center py-20 gap-3">
      <div className="spinner"></div>
      <span className="text-slate-400">{t("loading")}</span>
    </div>
  );

  const stats = [
    { label: "Zapravkalar", value: data?.totalFillingStations ?? 0, icon: "fuel", color: "text-emerald-400", glow: "shadow-emerald-500/5", path: "/stations" },
    { label: t("stakeholders"), value: data?.totalStakeholders ?? 0, icon: "users", color: "text-blue-400", glow: "shadow-blue-500/5", path: "/stakeholders" },
    { label: t("transactions"), value: data?.totalTransactions ?? 0, icon: "credit-card", color: "text-amber-400", glow: "shadow-amber-500/5", path: "#" },
    { label: t("todayTransactions"), value: data?.todayTransactions ?? 0, icon: "pump", color: "text-emerald-400", glow: "shadow-emerald-500/5", path: "#" },
    { label: t("todayRevenue"), value: `${(data?.todayTotalAmount ?? 0).toLocaleString()} UZS`, icon: "banknote", color: "text-rose-400", glow: "shadow-rose-500/5", path: "#" },
  ];

  return (
    <div>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold tracking-tight text-white mb-1">{t("dashboard")}</h1>
        <p className="text-sm text-slate-400">Jami zapravkalar {data?.totalFillingStations ?? 0}</p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-4 mb-8">
        {stats.map((s) => (
          <div onClick={() => { window.location.hash = s.path; }} className={`bg-[#161c24]/60 backdrop-blur-md border border-slate-800 rounded-2xl p-5 hover:border-slate-700 transition cursor-pointer ${s.glow}`}>
            <div className="flex items-start justify-between">
              <div className="flex flex-col">
                <span className="text-xs text-slate-400 mb-2">{s.label}</span>
                <span className={`text-2xl font-bold ${s.color}`}>{s.value}</span>
              </div>
              <span className={`w-12 h-12 rounded-xl bg-slate-800/50 flex items-center justify-center border border-slate-700/50`}>
                <i data-lucide={s.icon} className={`w-6 h-6 ${s.color}`}></i>
              </span>
            </div>
          </div>
        ))}
      </div>

      {/* Transactions Quick View */}
      <TransactionsMini t={t} />
    </div>
  );
}

function TransactionsMini({ t }) {
  const [txns, setTxns] = useState([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getTransactions({ pageSize: 10 }).then((r) => {
      setTxns((r?.items || r?.data || r || []).slice(0, 10));
    }).catch(() => {}).finally(() => setLoading(false));
    window.lucide?.createIcons();
  }, []);

  const filtered = search ? txns.filter((tx) =>
    (tx.idempotencyKey || "").toLowerCase().includes(search.toLowerCase()) ||
    (tx.fillingStationName || "").toLowerCase().includes(search.toLowerCase()) ||
    (tx.paymentName || "").toLowerCase().includes(search.toLowerCase())
  ) : txns;

  const statusBadge = (status) => {
    if (status === "Completed" || status === "Paid") return { cls: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20", icon: "check-circle", label: t("completed") };
    if (status === "Pending" || status === "Processing") return { cls: "bg-amber-500/10 text-amber-400 border-amber-500/20", icon: "clock", label: t("pending") };
    if (status === "Failed" || status === "Cancelled") return { cls: "bg-rose-500/10 text-rose-400 border-rose-500/20", icon: "x-circle", label: t("failed") };
    return { cls: "bg-slate-500/10 text-slate-400 border-slate-500/20", icon: "help-circle", label: status };
  };

  return (
    <div className="bg-[#161c24]/60 backdrop-blur-md border border-slate-800 rounded-2xl overflow-hidden">
      {/* Header Row */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 p-6 border-b border-slate-800">
        <div className="flex items-center gap-3">
          <span className="w-10 h-10 rounded-xl bg-teal-500/10 flex items-center justify-center">
            <i data-lucide="scroll-text" className="w-5 h-5 text-teal-400"></i>
          </span>
          <h2 className="text-xl font-bold text-white">{t("transactions")}</h2>
        </div>
        <div className="relative w-full sm:w-72">
          <i data-lucide="search" className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500"></i>
          <input
            className="w-full bg-[#11161d] border border-slate-800 rounded-xl pl-10 pr-4 py-2 text-sm text-slate-300 outline-none focus:border-teal-500/50 transition"
            placeholder={t("search")}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Table */}
      <div className="overflow-x-auto">
        {loading ? (
          <div className="flex items-center justify-center py-12 gap-3"><div className="spinner"></div><span className="text-slate-400">{t("loading")}</span></div>
        ) : txns.length === 0 ? (
          <div className="py-16 text-center text-slate-500">{t("noTransactions")}</div>
        ) : (
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-[#1a212c]/40 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                <th className="py-4 px-6">ID</th>
                <th className="py-4 px-6">Transaction ID</th>
                <th className="py-4 px-6">{t("station")}</th>
                <th className="py-4 px-6">{t("amount")}</th>
                <th className="py-4 px-6">{t("payment")}</th>
                <th className="py-4 px-6">{t("status")}</th>
                <th className="py-4 px-6">{t("date")}</th>
                <th className="py-4 px-6 w-20"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60 text-sm">
              {filtered.map((tx) => {
                const badge = statusBadge(tx.status);
                return (
                  <tr key={tx.id} className="hover:bg-slate-800/30 transition-colors">
                    <td className="py-4 px-6 font-mono text-xs text-slate-500">{String(tx.id).slice(0, 8)}</td>
                    <td className="py-4 px-6 font-mono text-xs text-slate-400 max-w-[140px] truncate">{tx.idempotencyKey || "—"}</td>
                    <td className="py-4 px-6 font-medium text-white">{tx.fillingStationName || "—"}</td>
                    <td className="py-4 px-6 text-white font-semibold">{(tx.totalSum ?? 0).toLocaleString()} UZS</td>
                    <td className="py-4 px-6 text-slate-300">{tx.paymentName || "—"}</td>
                    <td className="py-4 px-6">
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${badge.cls}`}>
                        <i data-lucide={badge.icon} className="w-3 h-3"></i>
                        {badge.label}
                      </span>
                    </td>
                    <td className="py-4 px-6 text-xs text-slate-400 whitespace-nowrap">{tx.createdAt ? new Date(tx.createdAt).toLocaleString() : "—"}</td>
                    <td className="py-4 px-6 text-right">
                      <div className="inline-flex items-center gap-1">
                        <button className="p-1.5 rounded-lg text-amber-400/80 hover:text-amber-400 hover:bg-amber-500/10 transition cursor-pointer" title={t("edit")}>
                          <i data-lucide="pencil" className="w-3.5 h-3.5"></i>
                        </button>
                        <button className="p-1.5 rounded-lg text-rose-400/80 hover:text-rose-400 hover:bg-rose-500/10 transition cursor-pointer" title={t("delete")}>
                          <i data-lucide="trash-2" className="w-3.5 h-3.5"></i>
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
