import { createContext, useContext, useState, useCallback } from 'react';
import { translations } from './translations';

const LanguageContext = createContext();

export function LanguageProvider({ children }) {
  const [lang, setLang] = useState(() => localStorage.getItem('gzs-lang') || 'uz');

  const t = useCallback((key, params) => {
    let text = translations[lang]?.[key] || translations.uz[key] || key;
    if (params) {
      Object.entries(params).forEach(([k, v]) => {
        text = text.replace('{' + k + '}', v);
      });
    }
    return text;
  }, [lang]);

  const changeLanguage = useCallback((newLang) => {
    setLang(newLang);
    localStorage.setItem('gzs-lang', newLang);
  }, []);

  return (
    <LanguageContext.Provider value={{ lang, t, changeLanguage }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useTranslation() {
  const ctx = useContext(LanguageContext);
  if (!ctx) throw new Error('useTranslation must be used within LanguageProvider');
  return ctx;
}
