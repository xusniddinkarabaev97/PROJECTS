/* ASSETO — CSRF protection. */
(function () {
  function getCsrf() {
    const c = document.cookie.split(';').map(s => s.trim()).find(s => s.startsWith('csrf_token='));
    return c ? c.split('=').slice(1).join('=') : '';
  }

  const _orig = window.fetch;
  window.fetch = function (url, opts) {
    opts = opts || {};
    if (['POST', 'PUT', 'DELETE', 'PATCH'].includes((opts.method || 'GET').toUpperCase())) {
      const token = getCsrf();
      if (token) {
        opts.headers = Object.assign({}, opts.headers, { 'X-CSRF-Token': token });
      }
    }
    return _orig(url, opts);
  };

  if (!getCsrf()) {
    _orig.call(window, '/api/auth/csrf-refresh', { method: 'GET', credentials: 'same-origin' })
      .catch(function() { /* ignore */ });
  }
})();
