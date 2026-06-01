// =============================================
// Site-wide early initialization + Diagnostics
// =============================================

// Expose a global helper so C# can update the loading screen with status messages.
// This is very useful for diagnosing startup hangs.
window.updateLoadingStatus = function (message) {
    console.log('[Blazor Startup] ' + message);

    const el = document.getElementById('loading-status');
    if (el) {
        el.textContent = message;
    }
};

// Pre-warm the Google Maps readiness promise.
// The actual Maps script is now loaded via a static <script async defer> tag
// in index.html using Google's recommended pattern (loading=async + callback).
// This gives the browser the earliest possible start.
//
// Calling loadGoogleMaps() here just ensures the shared promise is created
// so that trailer-map.js and zone-boundary-map.js resolve instantly later.
import('./google-maps-loader.js')
    .then(module => {
        module.loadGoogleMaps().catch(() => {
            // Non-fatal — real usage will handle loading/retry
        });
    })
    .catch(() => {
        console.debug('[OutsourceTracker] Google Maps pre-warm could not start (will load on demand).');
    });
