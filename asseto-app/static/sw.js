/* ASSETO Service Worker v3.0 */
const CACHE_NAME = 'asseto-v3';
const STATIC_CACHE = 'asseto-static-v3';
const API_CACHE = 'asseto-api-v3';

// Static assets to cache immediately on install
const STATIC_ASSETS = [
  '/',
  '/static/css/asseto-ios.css',
  '/static/js/app.js',
  '/static/js/csrf.js',
  '/static/js/i18n.js',
  '/static/js/app-polish.js',
  '/offline',
];

// API routes to cache (stale-while-revalidate)
const CACHE_API = [
  '/api/items',
  '/api/stats',
  '/api/departments',
];

// ── Install: pre-cache static assets ─────────────────────────────────────────
self.addEventListener('install', e => {
  e.waitUntil(
    caches.open(STATIC_CACHE).then(cache => {
      return cache.addAll(STATIC_ASSETS).catch(err => {
        console.warn('[SW] Pre-cache partial fail:', err);
      });
    }).then(() => self.skipWaiting())
  );
});

// ── Activate: clean ALL old caches ────────────────────────────────────────────────
self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys().then(keys =>
      Promise.all(
        keys
          .filter(k => k !== STATIC_CACHE && k !== API_CACHE && k !== CACHE_NAME)
          .map(k => { console.log('[SW] Deleting old cache:', k); return caches.delete(k); })
      )
    ).then(() => self.clients.claim())
  );
});

// ── Fetch strategy ────────────────────────────────────────────────────────────
self.addEventListener('fetch', e => {
  const { request } = e;
  const url = new URL(request.url);

  // Skip non-GET, cross-origin, chrome-extension
  if (request.method !== 'GET') return;
  if (url.origin !== location.origin) return;

  // API routes: Network-first, fallback to cache (stale data ok offline)
  if (url.pathname.startsWith('/api/')) {
    const shouldCache = CACHE_API.some(p => url.pathname.startsWith(p));
    if (shouldCache) {
      e.respondWith(networkFirstAPI(request));
    }
    return;
  }

  // Static assets: Cache-first (immutable after build)
  if (url.pathname.startsWith('/static/')) {
    e.respondWith(cacheFirst(request));
    return;
  }

  // Pages: Network-first, offline fallback
  e.respondWith(networkFirstPage(request));
});

// ── Strategy: Cache-first ────────────────────────────────────────────────────
async function cacheFirst(request) {
  const cached = await caches.match(request);
  if (cached) return cached;
  try {
    const resp = await fetch(request);
    if (resp.ok) {
      const cache = await caches.open(STATIC_CACHE);
      cache.put(request, resp.clone());
    }
    return resp;
  } catch {
    return new Response('', { status: 503 });
  }
}

// ── Strategy: Network-first for pages ────────────────────────────────────────
async function networkFirstPage(request) {
  try {
    const resp = await fetch(request);
    if (resp.ok) {
      const cache = await caches.open(STATIC_CACHE);
      cache.put(request, resp.clone());
    }
    return resp;
  } catch {
    const cached = await caches.match(request);
    if (cached) return cached;
    const offline = await caches.match('/offline');
    return offline || new Response('<h1>Нет соединения</h1>', {
      headers: { 'Content-Type': 'text/html; charset=utf-8' }
    });
  }
}

// ── Strategy: Network-first for API (stale-while-revalidate) ─────────────────
async function networkFirstAPI(request) {
  const cache = await caches.open(API_CACHE);
  try {
    const resp = await fetch(request);
    if (resp.ok) cache.put(request, resp.clone());
    return resp;
  } catch {
    const cached = await cache.match(request);
    return cached || new Response(
      JSON.stringify({ error: 'offline', cached: false }),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }
}

// ── Push Notifications ────────────────────────────────────────────────────────
self.addEventListener('push', e => {
  if (!e.data) return;
  let data;
  try { data = e.data.json(); }
  catch { data = { title: 'ASSETO', body: e.data.text() }; }

  const opts = {
    body: data.body || '',
    icon: '/static/icons/icon-192.png',
    badge: '/static/icons/badge-72.png',
    tag: data.tag || 'asseto-notif',
    data: { url: data.url || '/dashboard' },
    actions: data.actions || [],
    vibrate: [100, 50, 100],
    requireInteraction: data.urgent || false,
  };

  e.waitUntil(
    self.registration.showNotification(data.title || 'ASSETO', opts)
  );
});

// ── Notification click ────────────────────────────────────────────────────────
self.addEventListener('notificationclick', e => {
  e.notification.close();
  const url = (e.notification.data && e.notification.data.url) || '/dashboard';
  e.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(list => {
      for (const c of list) {
        if (c.url.includes(location.origin) && 'focus' in c) {
          c.navigate(url);
          return c.focus();
        }
      }
      return clients.openWindow(url);
    })
  );
});

// ── Background sync (queue failed mutations) ──────────────────────────────────
self.addEventListener('sync', e => {
  if (e.tag === 'asseto-sync') {
    e.waitUntil(replayQueue());
  }
});

async function replayQueue() {
  // Replay any queued POST/PUT/DELETE that failed offline
  // Stored in IndexedDB by app.js when offline
  try {
    const db = await openIDB();
    const tx = db.transaction('queue', 'readwrite');
    const store = tx.objectStore('queue');
    const items = await storeGetAll(store);
    for (const item of items) {
      try {
        await fetch(item.url, {
          method: item.method,
          headers: item.headers,
          body: item.body,
        });
        store.delete(item.id);
      } catch { /* keep in queue */ }
    }
  } catch(e) { console.warn('[SW] Queue replay failed:', e); }
}

function openIDB() {
  return new Promise((res, rej) => {
    const req = indexedDB.open('asseto-offline', 1);
    req.onupgradeneeded = e => {
      e.target.result.createObjectStore('queue', { keyPath: 'id', autoIncrement: true });
    };
    req.onsuccess = e => res(e.target.result);
    req.onerror = e => rej(e.target.error);
  });
}
function storeGetAll(store) {
  return new Promise((res, rej) => {
    const r = store.getAll();
    r.onsuccess = () => res(r.result);
    r.onerror = e => rej(e);
  });
}
