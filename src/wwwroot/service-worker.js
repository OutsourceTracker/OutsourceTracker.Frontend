let CACHE_NAME = "outsource-tracker-static-fallback";

const ASSETS_TO_CACHE = [
    '/',
    '/index.html',
    '/css/app.css',
    '/favicon.png',
    '/icon-192.png',
    '/icon-512.png',
    '/manifest.webmanifest'
];

self.addEventListener('install', event => {
    event.waitUntil(
        (async () => {
            let versionData = null;

            try {
                const response = await fetch('/version.json', { cache: 'no-store' });
                if (response.ok) {
                    const data = await response.json();
                    versionData = data;

                    if (data?.commit) {
                        CACHE_NAME = `outsource-tracker-static-${data.commit}`;
                        console.log(`[SW] Using dynamic cache: ${CACHE_NAME}`);

                        // Save version info to localStorage for the app to read
                        localStorage.setItem('appVersionInfo', JSON.stringify({
                            version: data.version || 'unknown',
                            commit: data.commit,
                            built: data.built,
                            branch: data.branch || 'unknown',
                            buildId: data.buildId || 'unknown',
                            timestamp: new Date().toISOString()
                        }));
                        console.log('[SW] Saved version info to localStorage');
                    }
                }
            } catch (err) {
                console.warn('[SW] Could not fetch version.json:', err);
            }

            // Proceed with caching using whatever CACHE_NAME we have
            const cache = await caches.open(CACHE_NAME);
            await cache.addAll(ASSETS_TO_CACHE).catch(err => {
                console.warn('[SW] Some assets failed to cache:', err);
            });

            self.skipWaiting();
        })()
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        (async () => {
            const cacheKeys = await caches.keys();
            await Promise.all(
                cacheKeys.map(key => {
                    if (key !== CACHE_NAME && key.startsWith('outsource-tracker-static')) {
                        console.log(`[SW] Deleting old cache: ${key}`);
                        return caches.delete(key);
                    }
                })
            );

            await self.clients.claim();
            console.log(`[SW] Activated – current cache: ${CACHE_NAME}`);
        })()
    );
});

self.addEventListener('fetch', event => {
    event.respondWith(
        fetch(event.request)
            .then(networkResponse => {
                return networkResponse;
            })
            .catch(() => {
                // Offline fallback: serve from cache
                return caches.match(event.request)
                    .then(cachedResponse => {
                        return cachedResponse || new Response('Offline – please check connection', {
                            status: 503,
                            statusText: 'Service Unavailable'
                        });
                    });
            })
    );
});

console.log('[SW] Outsource Tracker service worker loaded');