import { useTranslation } from "../i18n/LanguageContext";

export default function Sverka() {
  const { t } = useTranslation();

  return (
    <div className="info-card">
      <div className="info-card-icon">{String.fromCodePoint(0x1F504)}</div>
      <h2 className="info-card-title">{t("sverka.pageTitle")}</h2>
      <p className="info-card-desc">{t("sverka.description")}</p>
    </div>
  );
}
