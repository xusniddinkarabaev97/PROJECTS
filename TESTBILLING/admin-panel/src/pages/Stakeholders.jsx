import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const emptyForm = {
  fullName: "",
  fillingStationId: "",
  paymentId: "",
  bankAccount: "",
  sharePercent: "",
};

export default function Stakeholders() {
  const { t } = useTranslation();
  const [stakeholders, setStakeholders] = useState([]);
  const [stations, setStations] = useState([]);
  const [payments, setPayments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formErrors, setFormErrors] = useState({});
  const [deleteConfirm, setDeleteConfirm] = useState(null);
  const [filterStationId, setFilterStationId] = useState("");

  const fetchData = useCallback(() => {
    setLoading(true);
    setError(null);
    Promise.all([api.getStakeholders(), api.getStations(), api.getPayments()])
      .then(([stakeholdersData, stationsData, paymentsData]) => {
        console.log("Payments loaded:", paymentsData);
        setStakeholders(stakeholdersData);
        setStations(stationsData);
        setPayments(paymentsData);
      })
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const getStationName = (id) => {
    const station = stations.find((s) => s.id === id);
    return station ? station.name : "—";
  };

  const getPaymentName = (id) => {
    const payment = payments.find((p) => p.id === id);
    return payment ? payment.name : "—";
  };

  const validate = () => {
    const errors = {};
    if (!form.fullName.trim()) errors.fullName = t("fullNameRequired");
    if (!form.fillingStationId)
      errors.fillingStationId = t("fillingStationRequired");
    if (!form.paymentId) errors.paymentId = t("paymentRequired");
    if (!form.sharePercent) errors.sharePercent = t("sharePercentRequired");
    else if (
      isNaN(form.sharePercent) ||
      Number(form.sharePercent) < 0 ||
      Number(form.sharePercent) > 100
    )
      errors.sharePercent = t("sharePercentRange");
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const openAdd = () => {
    setEditingId(null);
    setForm(emptyForm);
    setFormErrors({});
    setShowModal(true);
  };

  const openEdit = (stakeholder) => {
    setEditingId(stakeholder.id);
    setForm({
      fullName: stakeholder.fullName || "",
      fillingStationId: stakeholder.fillingStationId || "",
      paymentId: stakeholder.paymentId || "",
      bankAccount: stakeholder.bankAccount || "",
      sharePercent: stakeholder.sharePercent ?? "",
    });
    setFormErrors({});
    setShowModal(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    setSaving(true);
    try {
      const payload = { ...form, sharePercent: Number(form.sharePercent) };
      if (editingId) {
        await api.updateStakeholder(editingId, payload);
      } else {
        await api.createStakeholder(payload);
      }
      setShowModal(false);
      fetchData();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id) => {
    try {
      await api.deleteStakeholder(id);
      setDeleteConfirm(null);
      fetchData();
    } catch (err) {
      setError(err.message);
    }
  };

  const setField = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    if (formErrors[field]) setFormErrors((prev) => ({ ...prev, [field]: "" }));
  };

  const filteredStakeholders = filterStationId
    ? stakeholders.filter((s) => s.fillingStationId == filterStationId)
    : stakeholders;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-[#c9d1d9]">
          {t("stakeholders")}
        </h2>
        <button
          onClick={openAdd}
          className="bg-[#238636] hover:bg-[#2ea043] text-white px-5 py-2.5 rounded-lg font-medium transition-colors flex items-center gap-2"
        >
          <span className="text-lg">+</span> {t("add").replace("+ ", "")}
        </button>
      </div>

      {/* Filter */}
      <div className="mb-4">
        <select
          value={filterStationId}
          onChange={(e) => setFilterStationId(e.target.value)}
          className="px-4 py-2 border border-[#30363d] bg-[#0d1117] text-[#c9d1d9] rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none"
        >
          <option value="">{t("all")}</option>
          {stations.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>
      </div>

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
      ) : filteredStakeholders.length === 0 ? (
        <div className="text-center py-20 bg-[#161b22] rounded-xl shadow-sm border border-[#30363d]">
          <p className="text-[#8b949e] text-lg">
            {filterStationId ? t("noStakeholderFilter") : t("noStakeholders")}
          </p>
          <button
            onClick={openAdd}
            className="mt-3 text-[#58a6ff] hover:text-[#79c0ff] font-medium"
          >
            {t("addFirstStakeholder")}
          </button>
        </div>
      ) : (
        <div className="bg-[#161b22] rounded-xl shadow-sm overflow-hidden border border-[#30363d]">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-[#21262d] text-left text-sm font-semibold text-[#8b949e] uppercase tracking-wider">
                  <th className="px-6 py-4">ID</th>
                  <th className="px-6 py-4">{t("fullName")}</th>
                  <th className="px-6 py-4">{t("fillingStation")}</th>
                  <th className="px-6 py-4">{t("paymentSystem")}</th>
                  <th className="px-6 py-4">{t("bankAccount")}</th>
                  <th className="px-6 py-4 text-center">{t("sharePercent")}</th>
                  <th className="px-6 py-4 text-right">{t("actions")}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#30363d]">
                {filteredStakeholders.map((sh) => (
                  <tr
                    key={sh.id}
                    className="hover:bg-[#1c2128] transition-colors"
                  >
                    <td className="px-6 py-4 text-sm text-[#8b949e]">
                      {sh.id}
                    </td>
                    <td className="px-6 py-4 font-medium text-[#c9d1d9]">
                      {sh.fullName}
                    </td>
                    <td className="px-6 py-4 text-sm text-[#8b949e]">
                      {getStationName(sh.fillingStationId)}
                    </td>
                    <td className="px-6 py-4 text-sm text-[#8b949e]">
                      {getPaymentName(sh.paymentId)}
                    </td>
                    <td className="px-6 py-4 text-sm text-[#8b949e]">
                      {sh.bankAccount || "-"}
                    </td>
                    <td className="px-6 py-4 text-center">
                      <span className="inline-flex px-2.5 py-1 rounded-full text-xs font-medium bg-[#1a2332] text-[#58a6ff]">
                        {sh.sharePercent}%
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => openEdit(sh)}
                          className="px-3 py-1.5 text-sm bg-[#21262d] text-[#d29922] hover:bg-[#30363d] border border-[#30363d] rounded-lg transition-colors font-medium"
                        >
                          {t("edit")}
                        </button>
                        <button
                          onClick={() => setDeleteConfirm(sh)}
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

      {/* Modal */}
      {showModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 overflow-y-auto"
          onClick={() => setShowModal(false)}
        >
          <div
            className="bg-[#161b22] border border-[#30363d] rounded-xl shadow-2xl w-full max-w-lg mx-4 my-8"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between px-6 py-4 border-b border-[#30363d]">
              <h3 className="text-lg font-bold text-[#c9d1d9]">
                {editingId ? t("editStakeholder") : t("newStakeholder")}
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
                  {t("fullName")} *
                </label>
                <input
                  type="text"
                  value={form.fullName}
                  onChange={(e) => setField("fullName", e.target.value)}
                  className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none bg-[#0d1117] text-[#c9d1d9] ${formErrors.fullName ? "border-[#f85149]" : "border-[#30363d]"}`}
                  placeholder={t("stakeholderNamePlaceholder")}
                />
                {formErrors.fullName && (
                  <p className="text-[#f85149] text-xs mt-1">
                    {formErrors.fullName}
                  </p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-[#c9d1d9] mb-1">
                  {t("fillingStation")} *
                </label>
                <select
                  value={form.fillingStationId}
                  onChange={(e) => setField("fillingStationId", e.target.value)}
                  className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none bg-[#0d1117] text-[#c9d1d9] ${formErrors.fillingStationId ? "border-[#f85149]" : "border-[#30363d]"}`}
                >
                  <option value="">{t("selectStation")}</option>
                  {stations.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name}
                    </option>
                  ))}
                </select>
                {formErrors.fillingStationId && (
                  <p className="text-[#f85149] text-xs mt-1">
                    {formErrors.fillingStationId}
                  </p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-[#c9d1d9] mb-1">
                  {t("paymentSystem")} *
                </label>
                <select
                  value={form.paymentId}
                  onChange={(e) => setField("paymentId", e.target.value)}
                  className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none bg-[#0d1117] text-[#c9d1d9] ${formErrors.paymentId ? "border-[#f85149]" : "border-[#30363d]"}`}
                  required
                >
                  <option value="">{t("select")}</option>
                  {payments
                    .filter((p) => p.isActive !== false)
                    .map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                </select>
                {formErrors.paymentId && (
                  <p className="text-[#f85149] text-xs mt-1">
                    {formErrors.paymentId}
                  </p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-[#c9d1d9] mb-1">
                  {t("bankAccount")}
                </label>
                <input
                  type="text"
                  value={form.bankAccount}
                  onChange={(e) => setField("bankAccount", e.target.value)}
                  className="w-full px-3 py-2 border border-[#30363d] bg-[#0d1117] text-[#c9d1d9] rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none"
                  placeholder={t("bankAccountOptional")}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-[#c9d1d9] mb-1">
                  {t("sharePercent")} (%) *
                </label>
                <input
                  type="number"
                  min="0"
                  max="100"
                  step="0.01"
                  value={form.sharePercent}
                  onChange={(e) => setField("sharePercent", e.target.value)}
                  className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none bg-[#0d1117] text-[#c9d1d9] ${formErrors.sharePercent ? "border-[#f85149]" : "border-[#30363d]"}`}
                  placeholder={t("sharePercentPlaceholder")}
                />
                {formErrors.sharePercent && (
                  <p className="text-[#f85149] text-xs mt-1">
                    {formErrors.sharePercent}
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

      {/* Delete Confirmation */}
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
                __html: t("deleteStakeholderConfirm", {
                  name: `<strong>${deleteConfirm.fullName}</strong>`,
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
