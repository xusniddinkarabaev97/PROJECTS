import { useState, useEffect, useCallback } from "react";
import { api } from "../api/client";
import { useTranslation } from "../i18n/LanguageContext";

const ROLES = ["Cashier", "SeniorCashier", "Accountant", "Admin", "ChiefDoctor"];
const ROLE_LABELS = { Cashier: "Кассир", SeniorCashier: "Ст. кассир", Accountant: "Бухгалтер", Admin: "Администратор", ChiefDoctor: "Главврач" };

export default function Users() {
  const { t } = useTranslation();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({ login: "", name: "", role: "Cashier", password: "" });

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try { setUsers(await api.getUsers()); }
    catch (e) { setError(e.message); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { fetchUsers(); }, [fetchUsers]);

  const openCreate = () => { setEditing("new"); setForm({ login: "", name: "", role: "Cashier", password: "" }); };
  const openEdit = (u) => { setEditing(u.id); setForm({ login: u.login, name: u.name, role: u.role, password: "" }); };

  const handleSave = async () => {
    try {
      if (editing === "new") await api.createUser(form);
      else await api.updateUser(editing, form);
      setEditing(null); fetchUsers();
    } catch (e) { setError(e.message); }
  };

  const handleDelete = async (id) => {
    if (!confirm("Удалить пользователя?")) return;
    try { await api.deleteUser(id); fetchUsers(); }
    catch (e) { setError(e.message); }
  };

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 24 }}>
        <h2 style={{ fontSize: 24, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#fff" }}>👥 {t("users")}</h2>
        <button onClick={openCreate} className="btn btn-primary">➕ {t("add")}</button>
      </div>

      {error && <div style={{ background: "var(--danger-bg)", border: "1px solid var(--danger)", color: "var(--danger)", padding: "10px 14px", borderRadius: 10, marginBottom: 16, display: "flex", justifyContent: "space-between" }}><span>{error}</span><button onClick={() => setError(null)} style={{ color: "var(--danger)", background: "none", border: "none", cursor: "pointer", fontWeight: 700 }}>×</button></div>}

      {loading ? (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 60, gap: 12 }}><div className="spinner" /><span style={{ color: "var(--text-secondary)" }}>{t("loading")}</span></div>
      ) : (
        <div className="card" style={{ padding: 0, overflow: "hidden" }}>
          <div style={{ overflowX: "auto" }}>
            <table className="data-table">
              <thead><tr><th>ID</th><th>Логин</th><th>ФИО</th><th>Роль</th><th>Дата создания</th><th>Действия</th></tr></thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id}>
                    <td style={{ color: "var(--text-muted)", fontSize: 12 }}>#{u.id}</td>
                    <td style={{ fontWeight: 600 }}>{u.login}</td>
                    <td>{u.name}</td>
                    <td><span className="badge badge-accent">{ROLE_LABELS[u.role] || u.role}</span></td>
                    <td style={{ color: "var(--text-secondary)", fontSize: 12 }}>{u.createdAt ? new Date(u.createdAt).toLocaleDateString("ru-RU") : "—"}</td>
                    <td>
                      <div style={{ display: "flex", gap: 4 }}>
                        <button onClick={() => openEdit(u)} className="btn btn-ghost btn-sm">✏️</button>
                        <button onClick={() => handleDelete(u.id)} className="btn btn-danger btn-sm">🗑️</button>
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
      {editing && (
        <div className="modal-overlay" onClick={() => setEditing(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 440 }}>
            <h3 className="modal-title">{editing === "new" ? "👤 Новый пользователь" : "✏️ Редактирование"}</h3>
            <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
              <div>
                <label style={{ display: "block", fontSize: 12, color: "var(--text-secondary)", marginBottom: 4 }}>Логин</label>
                <input className="input" value={form.login} onChange={(e) => setForm({ ...form, login: e.target.value })} disabled={editing !== "new"} />
              </div>
              <div>
                <label style={{ display: "block", fontSize: 12, color: "var(--text-secondary)", marginBottom: 4 }}>ФИО</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              </div>
              <div>
                <label style={{ display: "block", fontSize: 12, color: "var(--text-secondary)", marginBottom: 4 }}>Роль</label>
                <select className="input" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}>
                  {ROLES.map((r) => <option key={r} value={r}>{ROLE_LABELS[r]}</option>)}
                </select>
              </div>
              <div>
                <label style={{ display: "block", fontSize: 12, color: "var(--text-secondary)", marginBottom: 4 }}>Пароль {editing !== "new" && "(оставьте пустым, чтобы не менять)"}</label>
                <input className="input" type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
              </div>
            </div>
            <div style={{ display: "flex", gap: 8, marginTop: 20 }}>
              <button onClick={() => setEditing(null)} className="btn btn-ghost" style={{ flex: 1, justifyContent: "center" }}>Отмена</button>
              <button onClick={handleSave} className="btn btn-primary" style={{ flex: 1, justifyContent: "center" }}>💾 Сохранить</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
