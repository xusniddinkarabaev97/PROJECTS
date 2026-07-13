import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from '../i18n/LanguageContext';
import { api } from '../api/client';

export default function PaymentDetail({ id }) {
  const { t } = useTranslation();
  const [payment, setPayment] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [token, setToken] = useState('');
  const [tokenSaving, setTokenSaving] = useState(false);
  const [certCommonName, setCertCommonName] = useState('');
  const [certValidDays, setCertValidDays] = useState(365);
  const [certSaving, setCertSaving] = useState(false);
  const [certResult, setCertResult] = useState('');
  const [whiteIps, setWhiteIps] = useState('');
  const [ipSaving, setIpSaving] = useState(false);

  const fetchPayment = useCallback(async () => {
    try {
      setLoading(true);
      const data = await api.getPayment(id);
      setPayment(data);
      setToken(data.token || '');
      setWhiteIps((data.whiteIps || []).join(', '));
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { fetchPayment(); }, [fetchPayment]);

  const handleGenerateToken = async () => {
    setTokenSaving(true);
    try {
      const data = await api.updatePayment(id, { ...payment, action: 'generateToken' });
      setToken(data.token || '');
      setPayment(data);
    } catch (err) { setError(err.message); }
    finally { setTokenSaving(false); }
  };

  const handleSaveToken = async () => {
    setTokenSaving(true);
    try {
      const data = await api.updatePayment(id, { ...payment, token });
      setPayment(data);
    } catch (err) { setError(err.message); }
    finally { setTokenSaving(false); }
  };

  const handleGenerateCert = async () => {
    setCertSaving(true);
    setCertResult('');
    try {
      const data = await api.updatePayment(id, { ...payment, certCommonName, certValidDays, action: 'generateCert' });
      setCertResult(data.certificate || t('certGenerated'));
    } catch (err) { setError(err.message); }
    finally { setCertSaving(false); }
  };

  const handleSaveIps = async () => {
    setIpSaving(true);
    try {
      const ips = whiteIps.split(',').map(ip => ip.trim()).filter(Boolean);
      const data = await api.updatePayment(id, { ...payment, whiteIps: ips });
      setPayment(data);
      setWhiteIps(ips.join(', '));
    } catch (err) { setError(err.message); }
    finally { setIpSaving(false); }
  };

  const handleSaveAll = async () => {
    setSaving(true);
    try {
      const ips = whiteIps.split(',').map(ip => ip.trim()).filter(Boolean);
      const data = await api.updatePayment(id, { ...payment, token, whiteIps: ips, certCommonName, certValidDays });
      setPayment(data);
    } catch (err) { setError(err.message); }
    finally { setSaving(false); }
  };

  if (loading) return <div className="flex items-center justify-center h-64"><p className="text-[#8b949e]">{t('loading')}</p></div>;
  if (error && !payment) return <div className="bg-[#490202] border border-[#f85149] text-[#f85149] p-4 rounded-lg">{error}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-[#c9d1d9]">{payment?.name || 'Payment #' + id}</h1>
          <a href="#/payments" className="text-xs text-[#58a6ff] hover:underline">&larr; {t('payments')}</a>
        </div>
        <button onClick={handleSaveAll} disabled={saving} className="px-4 py-2 bg-[#238636] hover:bg-[#2ea043] text-white rounded text-sm disabled:opacity-50 transition-colors">{saving ? t('saving') : t('save')}</button>
      </div>
      {error && <div className="bg-[#490202] border border-[#f85149] text-[#f85149] text-sm rounded p-3">{error}</div>}
      <div className="bg-[#161b22] border border-[#30363d] rounded-lg p-6">
        <h2 className="text-sm font-semibold text-[#c9d1d9] mb-4">Token</h2>
        <div className="flex gap-3">
          <input type="text" value={token} onChange={e => setToken(e.target.value)} className="flex-1 px-3 py-2 bg-[#0d1117] border border-[#30363d] rounded text-[#c9d1d9] text-sm focus:outline-none focus:border-[#58a6ff] font-mono" placeholder="Payment system token..." readOnly={!token} />
          <button onClick={handleGenerateToken} disabled={tokenSaving} className="px-4 py-2 bg-[#1f6feb] hover:bg-[#388bfd] text-white rounded text-sm disabled:opacity-50 transition-colors whitespace-nowrap">{tokenSaving ? t('saving') : t('generateToken')}</button>
          {token && <button onClick={handleSaveToken} disabled={tokenSaving} className="px-4 py-2 bg-[#238636] hover:bg-[#2ea043] text-white rounded text-sm disabled:opacity-50 transition-colors">{tokenSaving ? t('saving') : t('save')}</button>}
        </div>
      </div>
      <div className="bg-[#161b22] border border-[#30363d] rounded-lg p-6">
        <h2 className="text-sm font-semibold text-[#c9d1d9] mb-4">{t('sslCertificate')}</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          <div>
            <label className="block text-xs text-[#8b949e] mb-1">{t('commonName')}</label>
            <input type="text" value={certCommonName} onChange={e => setCertCommonName(e.target.value)} className="w-full px-3 py-2 bg-[#0d1117] border border-[#30363d] rounded text-[#c9d1d9] text-sm focus:outline-none focus:border-[#58a6ff]" placeholder="example.com" />
          </div>
          <div>
            <label className="block text-xs text-[#8b949e] mb-1">{t('validDays')}</label>
            <input type="number" value={certValidDays} onChange={e => setCertValidDays(Number(e.target.value))} className="w-full px-3 py-2 bg-[#0d1117] border border-[#30363d] rounded text-[#c9d1d9] text-sm focus:outline-none focus:border-[#58a6ff]" min={1} max={3650} />
          </div>
        </div>
        <div className="flex gap-3">
          <button onClick={handleGenerateCert} disabled={certSaving || !certCommonName.trim()} className="px-4 py-2 bg-[#1f6feb] hover:bg-[#388bfd] text-white rounded text-sm disabled:opacity-50 transition-colors">{certSaving ? t('saving') : t('generateCertificate')}</button>
        </div>
        {certResult && (
          <div className="mt-4">
            <label className="block text-xs text-[#8b949e] mb-1">{t('certificate')}</label>
            <textarea value={certResult} readOnly rows={6} className="w-full px-3 py-2 bg-[#0d1117] border border-[#30363d] rounded text-[#c9d1d9] text-sm font-mono resize-y" />
          </div>
        )}
      </div>
      <div className="bg-[#161b22] border border-[#30363d] rounded-lg p-6">
        <h2 className="text-sm font-semibold text-[#c9d1d9] mb-4">{t('whiteIps')}</h2>
        <textarea value={whiteIps} onChange={e => setWhiteIps(e.target.value)} rows={4} className="w-full px-3 py-2 bg-[#0d1117] border border-[#30363d] rounded text-[#c9d1d9] text-sm focus:outline-none focus:border-[#58a6ff] font-mono resize-y" placeholder="192.168.1.1, 10.0.0.1" />
        <div className="mt-3 flex gap-3">
          <button onClick={handleSaveIps} disabled={ipSaving} className="px-4 py-2 bg-[#238636] hover:bg-[#2ea043] text-white rounded text-sm disabled:opacity-50 transition-colors">{ipSaving ? t('saving') : t('save')}</button>
        </div>
      </div>
    </div>
  );
}
