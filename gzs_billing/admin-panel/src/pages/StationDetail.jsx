import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const emptyForm = { name: "", fuelType: "AI-92" };

export default function StationDetail({ id }) {
  const { t } = useTranslation();
  const [station, setStation] = useState(null);
  const [dispensers, setDispensers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formErrors, setFormErrors] = useState({});
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [qrModal, setQrModal] = useState(null);

  const fetchData = useCallback(() => {
    setLoading(true);
    setError(null);
    Promise.all([api.getStation(id), api.getDispensers(id)])
      .then(([stationData, dispensersData]) => {
        setStation(stationData);
        setDispensers(dispensersData);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const validate = () => {
    const errors = {};
    if (!form.name.trim()) errors.name = t("dispenserNameRequired");
    if (!form.fuelType.trim()) errors.fuelType = t("fuelTypeRequired");
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const openAdd = () => {
    setEditingId(null);
    setForm(emptyForm);
    setFormErrors({});
    setShowModal(true);
  };

  const openEdit = (dispenser) => {
    setEditingId(dispenser.id);
    setForm({
      name: dispenser.name || "",
      fuelType: dispenser.fuelType || "AI-92",
    });
    setFormErrors({});
    setShowModal(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    setSaving(true);
    try {
      if (editingId) {
        await api.updateDispenser(id, editingId, form);
      } else {
        await api.createDispenser(id, form);
      }
      setShowModal(false);
      fetchData();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (dispenserId) => {
    try {
      await api.deleteDispenser(id, dispenserId);
      setDeleteConfirm(null);
      fetchData();
    } catch (err) {
      setError(err.message);
    }
  };

  const handleBack = () => {
    window.location.hash = "#/stations";
  };

  const setField = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    if (formErrors[field]) setFormErrors((prev) => ({ ...prev, [field]: "" }));
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[#58a6ff]"></div>
        <span className="ml-3 text-[#8b949e]">{t("loading")}</span>
      </div>
    );
  }

  return (
    <div>
      <button
        onClick={handleBack}
        className="mb-4 px-4 py-2 text-sm font-medium text-[#c9d1d9] bg-[#21262d] hover:bg-[#30363d] border border-[#30363d] rounded-lg transition-colors flex items-center gap-2"
      >
        ← {t("backToStations")}
      </button>

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

      {station && (
        <div className="bg-[#161b22] border border-[#30363d] rounded-xl p-6 mb-6">
          <h2 className="text-2xl font-bold text-[#c9d1d9] mb-2">
            {station.name}
          </h2>
          <div className="flex gap-6 text-sm text-[#8b949e]">
            <span>{station.address}</span>
            <span>{station.region}</span>
            <span
              className={
                station.isActive !== false
                  ? "inline-flex px-2.5 py-0.5 rounded-full text-xs font-medium bg-[#1a3a2a] text-[#3fb950]"
                  : "inline-flex px-2.5 py-0.5 rounded-full text-xs font-medium bg-[#490202] text-[#f85149]"
              }
            >
              {station.isActive !== false ? t("active") : t("inactive")}
            </span>
          </div>
        </div>
      )}

      <div className="flex items-center justify-between mb-4">
        <h3 className="text-xl font-bold text-[#c9d1d9]">{t("dispensers")}</h3>
        <button
          onClick={openAdd}
          className="bg-[#238636] hover:bg-[#2ea043] text-white px-5 py-2.5 rounded-lg font-medium transition-colors flex items-center gap-2"
        >
          <span className="text-lg">+</span> {t("addDispenser")}
        </button>
      </div>
      {dispensers.length === 0 ? (
        <div className="text-center py-20 bg-[#161b22] rounded-xl shadow-sm border border-[#30363d]">
          <p className="text-[#8b949e] text-lg">{t("noDispensers")}</p>
          <button
            onClick={openAdd}
            className="mt-3 text-[#58a6ff] hover:text-[#79c0ff] font-medium"
          >
            {t("addDispenser")}
          </button>
        </div>
      ) : (
        <div className="bg-[#161b22] rounded-xl shadow-sm overflow-hidden border border-[#30363d]">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-[#21262d] text-left text-sm font-semibold text-[#8b949e] uppercase tracking-wider">
                  <th className="px-6 py-4">ID</th>
                  <th className="px-6 py-4">{t("dispenserName")}</th>
                  <th className="px-6 py-4">{t("fuelType")}</th>
                  <th className="px-2 py-4 text-center">QR</th>
                  <th className="px-6 py-4 text-center">{t("status")}</th>
                  <th className="px-6 py-4 text-right">{t("actions")}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#30363d]">
                {dispensers.map((dispenser) => (
                  <tr
                    key={dispenser.id}
                    className="hover:bg-[#1c2128] transition-colors"
                  >
                    <td className="px-6 py-4 text-sm text-[#8b949e]">
                      {dispenser.id}
                    </td>
                    <td className="px-6 py-4 font-medium text-[#c9d1d9]">
                      {dispenser.name}
                    </td>
                    <td className="px-6 py-4 text-sm text-[#8b949e]">
                      {dispenser.fuelType}
                    </td>
                    <td className="px-2 py-2 text-center">
                      <button
                        onClick={() =>
                          setQrModal({
                            stationId: Number(id),
                            dispenserId: dispenser.id,
                          })
                        }
                        className="hover:opacity-80 transition-opacity"
                        title="Click to enlarge"
                      >
                        <img
                          src={`https://api.qrserver.com/v1/create-qr-code/?size=60x60&data=${encodeURIComponent(JSON.stringify({ filling_station_id: Number(id), dispenser_id: dispenser.id }))}`}
                          alt="QR"
                          className="w-10 h-10 rounded cursor-pointer"
                        />
                      </button>
                    </td>
                    <td className="px-6 py-4 text-center">
                      <span
                        className={
                          dispenser.isActive
                            ? "inline-flex px-2.5 py-1 rounded-full text-xs font-medium bg-[#1a3a2a] text-[#3fb950]"
                            : "inline-flex px-2.5 py-1 rounded-full text-xs font-medium bg-[#490202] text-[#f85149]"
                        }
                      >
                        {dispenser.isActive ? t("active") : t("inactive")}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => openEdit(dispenser)}
                          className="px-3 py-1.5 text-sm bg-[#21262d] text-[#d29922] hover:bg-[#30363d] border border-[#30363d] rounded-lg transition-colors font-medium"
                        >
                          {t("edit")}
                        </button>
                        <button
                          onClick={() => setDeleteConfirm(dispenser)}
                          className="px-3 py-1.5 text-sm bg-[#490202] text-[#f85149] hover:bg-[#da3633] hover:text-white border border-[#da3633] rounded-lg transition-colors font-medium"
                        >
                          {t("delete")}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
      {/* QR Modal */}
      {qrModal && (
        <div
          className="fixed inset-0 bg-black bg-opacity-70 flex items-center justify-center z-50"
          onClick={() => setQrModal(null)}
        >
          <div
            className="bg-[#161b22] border border-[#30363d] rounded-xl p-6"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold text-[#c9d1d9]">
                QR Code — Kolonka #{qrModal.dispenserId}
              </h3>
              <button
                onClick={() => setQrModal(null)}
                className="text-[#8b949e] hover:text-white text-xl"
              >
                ✕
              </button>
            </div>
            <div className="bg-white p-4 rounded-lg">
              <img
                src={`https://api.qrserver.com/v1/create-qr-code/?size=1000x1000&data=${encodeURIComponent(JSON.stringify({ filling_station_id: Number(id), dispenser_id: qrModal.dispenserId }))}`}
                alt="QR Large"
                className="w-[500px] h-[500px]"
              />
            </div>
            <div className="flex gap-2 justify-end mt-4">
              <a
                href={`https://api.qrserver.com/v1/create-qr-code/?size=1000x1000&data=${encodeURIComponent(JSON.stringify({ filling_station_id: Number(id), dispenser_id: qrModal.dispenserId }))}`}
                download={`qr-station-${id}-dispenser-${qrModal.dispenserId}.png`}
                className="px-4 py-2 bg-[#238636] hover:bg-[#2ea043] text-white rounded-lg text-sm transition-colors"
              >
                📥 Yuklab olish
              </a>
              <button
                onClick={() => setQrModal(null)}
                className="px-4 py-2 bg-[#21262d] hover:bg-[#30363d] text-[#c9d1d9] rounded-lg text-sm border border-[#30363d] transition-colors"
              >
                Yopish
              </button>
            </div>
          </div>
        </div>
      )}

      {showModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
          onClick={() => setShowModal(false)}
        >
          <div
            className="bg-[#161b22] border border-[#30363d] rounded-xl shadow-2xl w-full max-w-md mx-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between px-6 py-4 border-b border-[#30363d]">
              <h3 className="text-lg font-bold text-[#c9d1d9]">
                {editingId ? t("editDispenser") : t("addDispenser")}
              </h3>
              <button
                onClick={() => setShowModal(false)}
                className="text-[#8b949e] hover:text-[#c9d1d9] text-xl"
              >
                x
              </button>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-[#c9d1d9] mb-1">
                  {t("dispenserName")} *
                </label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setField("name", e.target.value)}
                  className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none bg-[#0d1117] text-[#c9d1d9] ${formErrors.name ? "border-[#f85149]" : "border-[#30363d]"}`}
                  placeholder={t("dispenserNamePlaceholder")}
                />
                {formErrors.name && (
                  <p className="text-[#f85149] text-xs mt-1">
                    {formErrors.name}
                  </p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-[#c9d1d9] mb-1">
                  {t("fuelType")} *
                </label>
                <input
                  type="text"
                  value={form.fuelType}
                  onChange={(e) => setField("fuelType", e.target.value)}
                  className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none bg-[#0d1117] text-[#c9d1d9] ${formErrors.fuelType ? "border-[#f85149]" : "border-[#30363d]"}`}
                  placeholder="AI-92"
                />
                {formErrors.fuelType && (
                  <p className="text-[#f85149] text-xs mt-1">
                    {formErrors.fuelType}
                  </p>
                )}
              </div>
              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-4 py-2 text-sm font-medium text-[#c9d1d9] bg-[#21262d] hover:bg-[#30363d] border border-[#30363d] rounded-lg transition-colors"
                >
                  {t("cancel")}
                </button>
                <button
                  type="submit"
                  disabled={saving}
                  className="px-5 py-2 text-sm font-medium text-white bg-[#238636] hover:bg-[#2ea043] rounded-lg transition-colors disabled:opacity-50"
                >
                  {saving ? t("saving") : t("save")}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {deleteConfirm && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
          onClick={() => setDeleteConfirm(null)}
        >
          <div
            className="bg-[#161b22] border border-[#30363d] rounded-xl shadow-2xl w-full max-w-sm mx-4 p-6"
            onClick={(e) => e.stopPropagation()}
          >
            <h3 className="text-lg font-bold text-[#c9d1d9] mb-2">
              {t("deleteConfirmTitle")}
            </h3>
            <p
              className="text-[#8b949e] mb-6"
              dangerouslySetInnerHTML={{
                __html: t("deleteDispenserConfirm", {
                  name: "<strong>" + deleteConfirm.name + "</strong>",
                }),
              }}
            />
            <div className="flex justify-end gap-3">
              <button
                onClick={() => setDeleteConfirm(null)}
                className="px-4 py-2 text-sm font-medium text-[#c9d1d9] bg-[#21262d] hover:bg-[#30363d] border border-[#30363d] rounded-lg transition-colors"
              >
                {t("cancel")}
              </button>
              <button
                onClick={() => handleDelete(deleteConfirm.id)}
                className="px-4 py-2 text-sm font-medium text-white bg-[#da3633] hover:bg-[#f85149] rounded-lg transition-colors"
              >
                {t("delete")}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
