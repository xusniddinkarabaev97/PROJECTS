import { useState, useEffect } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

export default function Settings() {
  const { t } = useTranslation();
  const [settings, setSettings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [editingKey, setEditingKey] = useState(null);
  const [editValue, setEditValue] = useState("");
  const [saving, setSaving] = useState(false);
  const [successMsg, setSuccessMsg] = useState("");

  const fetchSettings = () => {
    setLoading(true);
    setError(null);
    api
      .getSettings()
      .then(setSettings)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchSettings();
  }, []);

  const startEdit = (setting) => {
    setEditingKey(setting.key);
    setEditValue(setting.value ?? "");
    setSuccessMsg("");
  };

  const cancelEdit = () => {
    setEditingKey(null);
    setEditValue("");
  };

  const handleSave = async (key) => {
    setSaving(true);
    try {
      await api.updateSetting(key, { value: editValue });
      setEditingKey(null);
      setEditValue("");
      setSuccessMsg(t("settingSaved", { key: `"${key}"` }));
      fetchSettings();
      setTimeout(() => setSuccessMsg(""), 3000);
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-[#c9d1d9]">{t("settings")}</h2>
      </div>

      {successMsg && (
        <div className="bg-[#1a3a2a] border border-[#3fb950] text-[#3fb950] px-4 py-3 rounded-lg mb-4">
          {successMsg}
        </div>
      )}

      {error && (
        <div className="bg-[#490202] border border-[#f85149] text-[#f85149] px-4 py-3 rounded-lg mb-4 flex items-center justify-between">
          <span>{error}</span>
          <button
            onClick={() => setError(null)}
            className="text-[#f85149] hover:text-[#ff7b72] font-bold"
          >
            x
          </button>
        </div>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[#58a6ff]"></div>
          <span className="ml-3 text-[#8b949e]">{t("loading")}</span>
        </div>
      ) : settings.length === 0 ? (
        <div className="text-center py-20 bg-[#161b22] rounded-xl shadow-sm border border-[#30363d]">
          <p className="text-[#8b949e] text-lg">{t("noSettings")}</p>
        </div>
      ) : (
        <div className="bg-[#161b22] rounded-xl shadow-sm overflow-hidden border border-[#30363d]">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-[#21262d] text-left text-sm font-semibold text-[#8b949e] uppercase tracking-wider">
                  <th className="px-6 py-4 w-48">{t("key")} (Key)</th>
                  <th className="px-6 py-4">{t("value")} (Value)</th>
                  <th className="px-6 py-4">{t("category")}</th>
                  <th className="px-6 py-4">{t("description")}</th>
                  <th className="px-6 py-4">{t("updatedAt")}</th>
                  <th className="px-6 py-4 text-right">{t("actions")}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#30363d]">
                {settings.map((setting) => (
                  <tr
                    key={setting.key}
                    className="hover:bg-[#1c2128] transition-colors"
                  >
                    <td className="px-6 py-4 font-mono text-sm font-medium text-[#c9d1d9]">
                      {setting.key}
                    </td>
                    <td className="px-6 py-4">
                      {editingKey === setting.key ? (
                        <textarea
                          value={editValue}
                          onChange={(e) => setEditValue(e.target.value)}
                          className="w-full px-3 py-2 border border-[#58a6ff] bg-[#0d1117] text-[#c9d1d9] rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none text-sm"
                          rows={editValue && editValue.length > 50 ? 3 : 1}
                        />
                      ) : (
                        <span className="text-sm text-[#8b949e] break-all">
                          {setting.value ?? "-"}
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <span className="inline-flex px-2 py-1 rounded-full text-xs font-medium bg-[#1a1a3a] text-[#bc8cff]">
                        {setting.category || t("general")}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-[#8b949e]">
                      {setting.description || "-"}
                    </td>
                    <td className="px-6 py-4 text-sm text-[#8b949e] whitespace-nowrap">
                      {setting.updatedAt
                        ? new Date(setting.updatedAt).toLocaleString("uz-UZ")
                        : "-"}
                    </td>
                    <td className="px-6 py-4 text-right">
                      {editingKey === setting.key ? (
                        <div className="flex items-center justify-end gap-2">
                          <button
                            onClick={cancelEdit}
                            className="px-3 py-1.5 text-sm bg-[#21262d] text-[#c9d1d9] hover:bg-[#30363d] border border-[#30363d] rounded-lg transition-colors font-medium"
                          >
                            {t("cancel")}
                          </button>
                          <button
                            onClick={() => handleSave(setting.key)}
                            disabled={saving}
                            className="px-3 py-1.5 text-sm bg-[#238636] text-white hover:bg-[#2ea043] rounded-lg transition-colors font-medium disabled:opacity-50"
                          >
                            {saving ? t("saving") : t("save")}
                          </button>
                        </div>
                      ) : (
                        <button
                          onClick={() => startEdit(setting)}
                          className="px-3 py-1.5 text-sm bg-[#21262d] text-[#d29922] hover:bg-[#30363d] border border-[#30363d] rounded-lg transition-colors font-medium"
                        >
                          {t("edit")}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
