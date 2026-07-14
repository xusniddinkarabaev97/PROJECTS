import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const STATUS_BADGE = {
  Completed: { cls: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20", icon: "check-circle", label: "Completed" },
  Paid: { cls: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20", icon: "check-circle", label: "Paid" },
  New: { cls: "bg-amber-500/10 text-amber-400 border-amber-500/20", icon: "clock", label: "New" },
  Pending: { cls: "bg-amber-500/10 text-amber-400 border-amber-500/20", icon: "clock", label: "Pending" },
  Failed: { cls: "bg-rose-500/10 text-rose-400 border-rose-500/20", icon: "x-circle", label: "Failed" },
  Cancelled: { cls: "bg-rose-500/10 text-rose-400 border-rose-500/20", icon: "x-circle", label: "Cancelled" },
  Refunded: { cls: "bg-blue-500/10 text-blue-400 border-blue-500/20", icon: "rotate-ccw", label: "Refunded" },
};

export default function Transactions() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [search, setSearch] = useState("");

  const fetchData = useCallback(async () => {
    setLoading(true); setError(null);
    try { const r = await api.getTransactions(); setData(Array.isArray(r) ? r : []); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchData(); window.lucide?.createIcons(); }, [fetchData]);

  const filtered = search ? data.filter((tx) =>
    String(tx.id).includes(search) ||
    tx.client?.fullName?.toLowerCase().includes(search.toLowerCase()) ||
    tx.status?.toLowerCase().includes(search.toLowerCase())
  ) : data;

  if (loading) return <div className="flex items-center justify-center py-20 gap-3"><div className="spinner"></div><span className="text-slate-400">{t("loading")}</span></div>;

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-white mb-1">{t("transactions")}</h1>
          <p className="text-sm text-slate-400">{t("total")}: {data.length}</p>
        </div>
        <div className="flex gap-2 items-center">
          <div className="relative">
            <i data-lucide="search" className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-500"></i>
            <input className="bg-[#11161d] border border-slate-800 rounded-xl pl-10 pr-4 py-2 text-sm text-slate-300 w-56 outline-none focus:border-teal-500/50" placeholder={t("search")} value={search} onChange={e => setSearch(e.target.value)} />
          </div>
          <button onClick={fetchData} className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800/50 cursor-pointer"><i data-lucide="refresh-cw" className="w-4 h-4"></i></button>
        </div>
      </div>

      {error && <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 rounded-xl p-4 text-sm mb-4">{error}</div>}

      {data.length === 0 ? (
        <div className="py-20 text-center text-slate-500">
          <div className="text-5xl mb-4">💳</div>
          <p>{t("noTransactions")}</p>
        </div>
      ) : (
        <div className="bg-[#161c24]/60 backdrop-blur-md border border-slate-800 rounded-2xl overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-800 bg-[#1a212c]/40 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                  <th className="py-4 px-6">ID</th>
                  <th className="py-4 px-6">{t("name") || "Mijoz"}</th>
                  <th className="py-4 px-6">{t("amount")}</th>
                  <th className="py-4 px-6">{t("paymentStatus")}</th>
                  <th className="py-4 px-6">{t("type")}</th>
                  <th className="py-4 px-6">{t("date")}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 text-sm">
                {filtered.map((tx) => {
                  const badge = STATUS_BADGE[tx.paymentStatus] || STATUS_BADGE.Pending;
                  return (
                    <tr key={tx.id} className="hover:bg-slate-800/30 transition-colors">
                      <td className="py-4 px-6 font-mono text-xs text-slate-500">#{tx.id}</td>
                      <td className="py-4 px-6 font-medium text-white">
                        {tx.client?.fullName || `#${tx.clientId || "—"}`}
                      </td>
                      <td className="py-4 px-6 text-white font-semibold">{(tx.totalSum ?? 0).toLocaleString()} UZS</td>
                      <td className="py-4 px-6">
                        <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${badge.cls}`}>
                          <i data-lucide={badge.icon} className="w-3 h-3"></i>
                          {badge.label}
                        </span>
                      </td>
                      <td className="py-4 px-6">
                        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-teal-500/10 text-teal-400 border border-teal-500/20">
                          {tx.status || "—"}
                        </span>
                      </td>
                      <td className="py-4 px-6 text-xs text-slate-400 whitespace-nowrap">
                        {tx.filledAt ? new Date(tx.filledAt).toLocaleString() : "—"}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <div className="px-6 py-3 border-t border-slate-800 text-xs text-slate-500">
            {t("total")}: {filtered.length} / {data.length} · {t("amount")}: {(filtered.reduce((s, tx) => s + (tx.totalSum || 0), 0)).toLocaleString()} UZS
          </div>
        </div>
      )}
    </div>
  );
}
