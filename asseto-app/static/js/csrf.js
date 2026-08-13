/* ASSETO — CSRF protection. Include on every page with mutating requests. */
(function () {
  function getCsrf() {
    const c = document.cookie.split(';').map(s => s.trim()).find(s => s.startsWith('csrf_token='));
    return c ? c.split('=').slice(1).join('=') : '';
  }

  // Patch window.fetch to auto-attach X-CSRF-Token
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

  // Auto-refresh CSRF cookie if missing (covers sessions older than CSRF implementation)
  if (!getCsrf()) {
    _orig.call(window, '/api/auth/csrf-refresh', { method: 'GET', credentials: 'same-origin' })
      .catch(() => { /* ignore if not logged in */ });
  }
})();
