import { useTranslation } from "../i18n/LanguageContext";

export default function Reports() {
  const { t } = useTranslation();

  return (
    <div className="info-card">
      <div className="info-card-icon">{String.fromCodePoint(0x1F4C8)}</div>
      <h2 className="info-card-title">{t("reports.pageTitle")}</h2>
      <p className="info-card-desc">{t("reports.description")}</p>
    </div>
  );
}
