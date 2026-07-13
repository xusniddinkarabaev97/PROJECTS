import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function Stations() {
  const { t } = useTranslation();
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editId, setEditId] = useState(null);
  const [form, setForm] = useState({ name: "", region: "", address: "" });
  const [saving, setSaving] = useState(false);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try { const res = await api.getStations(); setData(Array.isArray(res) ? res : []); } catch { setData([]); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetchData(); window.lucide?.createIcons(); }, [fetchData]);

  const openAdd = () => { setEditId(null); setForm({ name: "", region: "", address: "" }); setShowModal(true); };
  const openEdit = (s) => { setEditId(s.id); setForm({ name: s.name, region: s.region, address: s.address }); setShowModal(true); };

  const save = async () => {
    setSaving(true);
    try {
      editId ? await api.updateStation(editId, form) : await api.createStation(form);
      setShowModal(false);
      fetchData();
    } finally { setSaving(false); }
  };

  const del = async (id) => { if (confirm(t("deleteStationConfirm"))) { await api.deleteStation(id); fetchData(); } };

  if (loading) return <div className="flex items-center justify-center py-20 gap-3"><div className="spinner"></div><span className="text-slate-400">{t("loading")}</span></div>;

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-white mb-1">{t("stations")}</h1>
          <p className="text-sm text-slate-400">{t("totalStations")}: {data.length}</p>
        </div>
        <button onClick={openAdd} className="inline-flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-teal-500 to-emerald-500 text-slate-900 font-semibold text-sm shadow-lg shadow-teal-500/20 hover:opacity-90 transition cursor-pointer">
          <i data-lucide="plus" className="w-5 h-5"></i>
          {t("add")}
        </button>
      </div>

      <div className="bg-[#161c24]/60 backdrop-blur-md border border-slate-800 rounded-2xl shadow-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-[#1a212c]/40 text-xs font-semibold text-slate-400 uppercase tracking-wider">
                <th className="py-4 px-6 w-20">ID</th>
                <th className="py-4 px-6">{t("name")}</th>
                <th className="py-4 px-6">{t("address")}</th>
                <th className="py-4 px-6">{t("region")}</th>
                <th className="py-4 px-6 w-32">{t("status")}</th>
                <th className="py-4 px-6 w-24 text-right">{t("actions")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60 text-sm text-slate-300">
              {data.length === 0 ? (
                <tr><td colSpan="6" className="py-16 text-center text-slate-500">{t("noStations")}</td></tr>
              ) : data.map((s) => (
                <tr key={s.id} className="hover:bg-slate-800/30 transition-colors">
                  <td className="py-4 px-6 font-mono text-slate-500 font-medium">#{s.id}</td>
                  <td className="py-4 px-6 font-semibold text-white">{s.name}</td>
                  <td className="py-4 px-6 text-slate-400 max-w-[300px] truncate">{s.address || "—"}</td>
                  <td className="py-4 px-6">{s.region || "—"}</td>
                  <td className="py-4 px-6">
                    <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                      <span className="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
                      {s.isActive ? t("active") : t("inactive")}
                    </span>
                  </td>
                  <td className="py-4 px-6 text-right">
                    <div className="inline-flex items-center gap-1">
                      <button onClick={() => openEdit(s)} className="p-2 rounded-lg text-amber-400 hover:bg-amber-500/10 transition cursor-pointer" title={t("edit")}>
                        <i data-lucide="pencil" className="w-4 h-4"></i>
                      </button>
                      <button onClick={() => del(s.id)} className="p-2 rounded-lg text-rose-400 hover:bg-rose-500/10 transition cursor-pointer" title={t("delete")}>
                        <i data-lucide="trash-2" className="w-4 h-4"></i>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm" onClick={() => setShowModal(false)}>
          <div className="bg-[#161c24] border border-slate-700 rounded-2xl p-6 w-full max-w-md shadow-2xl" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-lg font-bold text-white mb-4">{editId ? t("editStation") : t("newStation")}</h3>
            <div className="space-y-3">
              <input className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-xl text-white text-sm outline-none focus:border-teal-500/50" placeholder={t("name")} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              <input className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-xl text-white text-sm outline-none focus:border-teal-500/50" placeholder={t("region")} value={form.region} onChange={(e) => setForm({ ...form, region: e.target.value })} />
              <input className="w-full px-4 py-2.5 bg-slate-800 border border-slate-700 rounded-xl text-white text-sm outline-none focus:border-teal-500/50" placeholder={t("address")} value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
              <div className="flex gap-3 pt-2">
                <button onClick={save} disabled={saving} className="flex-1 py-2.5 rounded-xl bg-gradient-to-r from-teal-500 to-emerald-500 text-slate-900 font-semibold text-sm hover:opacity-90 transition cursor-pointer disabled:opacity-50">
                  {saving ? t("saving") : t("save")}
                </button>
                <button onClick={() => setShowModal(false)} className="flex-1 py-2.5 rounded-xl bg-slate-800 text-slate-400 text-sm font-medium hover:text-white transition cursor-pointer">
                  {t("cancel")}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
