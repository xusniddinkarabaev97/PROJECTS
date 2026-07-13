import { useState, useEffect } from 'react';
import { useTranslation } from '../i18n/LanguageContext';

export default function Sverka() {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(true);
  useEffect(() => { setLoading(false); }, []);

  if (loading) return <div className="text-center py-12 text-[#8b949e]">{t('loading')}</div>;
  return (
    <div>
      <h2 className="text-2xl font-bold mb-6 text-[#c9d1d9]">{t('sverka')}</h2>
      <div className="bg-[#161b22] border border-[#30363d] rounded-lg p-8 text-center text-[#8b949e]">
        <p className="text-lg mb-2">Sverka</p>
        <p>Kunlik 3-way reconciliation natijalari shu yerda</p>
      </div>
    </div>
  );
}
