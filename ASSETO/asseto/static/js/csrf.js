/* ASSETO — CSRF protection + sub-path proxy support. */
var __ASSETO_BASE = (function() {
  var p = window.location.pathname;
  if (p.startsWith('/asseto/') || p === '/asseto') return '/asseto';
  return '';
})();

(function () {
  var BASE = __ASSETO_BASE;

  function getCsrf() {
    const c = document.cookie.split(';').map(s => s.trim()).find(s => s.startsWith('csrf_token='));
    return c ? c.split('=').slice(1).join('=') : '';
  }

  const _orig = window.fetch;
  window.fetch = function (url, opts) {
    opts = opts || {};
    if (typeof url === 'string' && url[0] === '/' && BASE && !url.startsWith(BASE + '/')) {
      url = BASE + url;
    }
    if (['POST', 'PUT', 'DELETE', 'PATCH'].includes((opts.method || 'GET').toUpperCase())) {
      const token = getCsrf();
      if (token) {
        opts.headers = Object.assign({}, opts.headers, { 'X-CSRF-Token': token });
      }
    }
    return _orig(url, opts);
  };

  if (!getCsrf()) {
    var csrfUrl = '/api/auth/csrf-refresh';
    if (BASE) csrfUrl = BASE + csrfUrl;
    _orig.call(window, csrfUrl, { method: 'GET', credentials: 'same-origin' })
      .catch(function() { /* ignore */ });
  }
})();

// Patch window.location redirects for sub-path proxy
(function() {
  var BASE = __ASSETO_BASE;
  if (!BASE) return;
  var _loc = window.location;
  var _assign = _loc.assign.bind(_loc);
  var _replace = _loc.replace.bind(_loc);
  delete window.location;
  window.location = _loc;
  _loc.assign = function(url) {
    if (typeof url === 'string' && url[0] === '/' && !url.startsWith(BASE + '/')) {
      url = BASE + url;
    }
    _assign(url);
  };
  _loc.replace = function(url) {
    if (typeof url === 'string' && url[0] === '/' && !url.startsWith(BASE + '/')) {
      url = BASE + url;
    }
    _replace(url);
  };
})();
