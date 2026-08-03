import { useTranslation } from "../i18n/LanguageContext";

export default function Integrations() {
  const { t } = useTranslation();
  const services = [
    { icon: "🏥", name: "EMIS / МИС", status: "online", desc: "Медицинская информационная система — проверка пациента и синхронизация платежей", endpoint: "emis.med.mod.uz" },
    { icon: "💳", name: "Click", status: "online", desc: "Платёжный шлюз Click — приём платежей через Click Pay", endpoint: "click.uz" },
    { icon: "🏦", name: "Uzcard", status: "warning", desc: "Эквайринг Uzcard — обработка карточных платежей", endpoint: "uzcard.uz" },
    { icon: "🏦", name: "Humo", status: "warning", desc: "Эквайринг Humo — обработка карточных платежей", endpoint: "humo.uz" },
    { icon: "📡", name: "ОФД РУз", status: "warning", desc: "Оператор фискальных данных — отправка чеков в ГНК", endpoint: "ofd.uz" },
  ];

  const statusInfo = {
    online: { dot: "🟢", color: "#3a8c5c", bg: "rgba(58,140,92,0.08)", border: "rgba(58,140,92,0.2)", label: "Онлайн" },
    warning: { dot: "🟡", color: "#5b8c3e", bg: "rgba(91,140,62,0.08)", border: "rgba(91,140,62,0.2)", label: "Настройка" },
    offline: { dot: "🔴", color: "#f87171", bg: "rgba(248,113,113,0.08)", border: "rgba(248,113,113,0.2)", label: "Офлайн" },
  };

  return (
    <div>
      <h2 style={{ fontSize: 24, fontWeight: 700, fontFamily: "'Montserrat', sans-serif", color: "#fff", marginBottom: 24 }}>🔌 {t("integrations")}</h2>
      <div style={{ display: "grid", gap: 12 }}>
        {services.map((s, i) => {
          const st = statusInfo[s.status];
          return (
            <div key={i} style={{ display: "flex", alignItems: "center", gap: 16, background: "var(--bg-card)", backdropFilter: "blur(8px)", border: "1px solid var(--border)", borderRadius: 16, padding: "16px 20px", boxShadow: "var(--green-glow)" }}>
              <div style={{ fontSize: 32 }}>{s.icon}</div>
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 14, fontWeight: 600, color: "#fff", marginBottom: 2 }}>{s.name}</div>
                <div style={{ fontSize: 12, color: "var(--text-secondary)", marginBottom: 4 }}>{s.desc}</div>
                <div style={{ fontFamily: "monospace", fontSize: 11, color: "var(--text-muted)" }}>{s.endpoint}</div>
              </div>
              <div style={{ textAlign: "right" }}>
                <span className="badge" style={{ background: st.bg, color: st.color, border: `1px solid ${st.border}`, marginBottom: 4 }}>{st.dot} {st.label}</span>
                <div style={{ fontSize: 11, color: "var(--text-muted)", marginTop: 4 }}>Ping: 12ms</div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
