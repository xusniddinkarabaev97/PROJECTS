import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const emptyForm = {
  name: "",
};

export default function Payments() {
  const { t } = useTranslation();
  const [payments, setPayments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formErrors, setFormErrors] = useState({});
  const [deleteConfirm, setDeleteConfirm] = useState(null);

  const fetchPayments = useCallback(() => {
    setLoading(true);
    setError(null);
    api
      .getPayments()
      .then(setPayments)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    fetchPayments();
  }, [fetchPayments]);

  const validate = () => {
    const errors = {};
    if (!form.name.trim()) errors.name = t("nameRequired");
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const openAdd = () => {
    setEditingId(null);
    setForm(emptyForm);
    setFormErrors({});
    setShowModal(true);
  };

  const openEdit = (payment) => {
    setEditingId(payment.id);
    setForm({
      name: payment.name || "",
    });
    setFormErrors({});
    setShowModal(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validate()) return;
    setSaving(true);
    try {
      const data = {
        name: form.name,
      };
      if (editingId) {
        await api.updatePayment(editingId, data);
      } else {
        await api.createPayment(data);
      }
      setShowModal(false);
      fetchPayments();
    } catch (err) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id) => {
    try {
      await api.deletePayment(id);
      setDeleteConfirm(null);
      fetchPayments();
    } catch (err) {
      setError(err.message);
    }
  };

  const setField = (field, value) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    if (formErrors[field]) setFormErrors((prev) => ({ ...prev, [field]: "" }));
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-[#c9d1d9]">{t("payments")}</h2>
        <button
          onClick={openAdd}
          className="bg-[#238636] hover:bg-[#2ea043] text-white px-5 py-2.5 rounded-lg font-medium transition-colors flex items-center gap-2"
        >
          <span className="text-lg">+</span> {t("add").replace("+ ", "")}
        </button>
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
      ) : payments.length === 0 ? (
        <div className="text-center py-20 bg-[#161b22] rounded-xl shadow-sm border border-[#30363d]">
          <p className="text-[#8b949e] text-lg">{t("noPayments")}</p>
          <button
            onClick={openAdd}
            className="mt-3 text-[#58a6ff] hover:text-[#79c0ff] font-medium"
          >
            {t("addFirstPayment")}
          </button>
        </div>
      ) : (
        <div className="bg-[#161b22] rounded-xl shadow-sm overflow-hidden border border-[#30363d]">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="bg-[#21262d] text-left text-sm font-semibold text-[#8b949e] uppercase tracking-wider">
                  <th className="px-6 py-4">ID</th>
                  <th className="px-6 py-4">{t("name")}</th>
                  <th className="px-6 py-4 text-center">{t("status")}</th>
                  <th className="px-6 py-4 text-right">{t("actions")}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#30363d]">
                {payments.map((payment) => (
                  <tr
                    key={payment.id}
                    className="hover:bg-[#1c2128] transition-colors"
                  >
                    <td className="px-6 py-4 text-sm text-[#8b949e]">
                      {payment.id}
                    </td>
                    <td className="px-6 py-4 font-medium text-[#c9d1d9]">
                      {payment.name}
                    </td>
                    <td className="px-6 py-4 text-center">
                      <span
                        className={`inline-flex px-2.5 py-1 rounded-full text-xs font-medium ${payment.isActive !== false ? "bg-[#1a3a2a] text-[#3fb950]" : "bg-[#490202] text-[#f85149]"}`}
                      >
                        {payment.isActive !== false
                          ? t("active")
                          : t("inactive")}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => openEdit(payment)}
                          className="px-3 py-1.5 text-sm bg-[#21262d] text-[#d29922] hover:bg-[#30363d] border border-[#30363d] rounded-lg transition-colors font-medium"
                        >
                          {t("edit")}
                        </button>
                        <button
                          onClick={() => setDeleteConfirm(payment)}
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

      {showModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
          onClick={() => setShowModal(false)}
        >
          <div
            className="bg-[#161b22] border border-[#30363d] rounded-xl shadow-2xl w-full max-w-lg mx-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between px-6 py-4 border-b border-[#30363d]">
              <h3 className="text-lg font-bold text-[#c9d1d9]">
                {editingId ? t("editPayment") : t("newPayment")}
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
                  {t("name")} *
                </label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => setField("name", e.target.value)}
                  className={`w-full px-3 py-2 border rounded-lg focus:ring-2 focus:ring-[#58a6ff] focus:border-[#58a6ff] outline-none bg-[#0d1117] text-[#c9d1d9] ${formErrors.name ? "border-[#f85149]" : "border-[#30363d]"}`}
                  placeholder={t("paymentNamePlaceholder")}
                />
                {formErrors.name && (
                  <p className="text-[#f85149] text-xs mt-1">
                    {formErrors.name}
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
                __html: t("deletePaymentConfirm", {
                  name: `<strong>${deleteConfirm.name}</strong>`,
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
