/**
 * Shared Google Maps JavaScript API loader.
 *
 * IMPORTANT:
 * The Maps API is now loaded using Google's recommended static pattern
 * directly in index.html:
 *
 *   <script async defer
 *     src="https://maps.googleapis.com/maps/api/js?key=...&libraries=marker&loading=async&callback=__googleMapsInit&v=weekly">
 *   </script>
 *
 * This loader is a thin promise-based coordinator on top of that static load.
 * It lets Blazor components and other modules simply `await loadGoogleMaps()`.
 *
 * Dynamic script injection has been removed — we rely exclusively on the
 * tag in index.html for earliest possible loading.
 */

const MAP_ID = '91655a72ee45e0e184bfe567'; // For AdvancedMarkerElement + PinElement

let loadPromise = null;
let isLoaded = false;

/**
 * Global callback invoked by the Maps API when using loading=async mode.
 * Defined in both index.html (primary) and here (for module timing).
 */
window.__googleMapsInit = function () {
    isLoaded = true;

    if (window.__resolveGoogleMapsLoad) {
        try {
            window.__resolveGoogleMapsLoad();
        } catch { }
        window.__resolveGoogleMapsLoad = null;
        window.__rejectGoogleMapsLoad = null;
    }
};

/**
 * Returns a promise that resolves once the Google Maps JavaScript API
 * (with the marker library) is fully ready.
 *
 * Safe to call any number of times — always returns the same promise.
 */
export function loadGoogleMaps() {
    if (isLoaded && window.google && window.google.maps) {
        return Promise.resolve();
    }

    if (loadPromise) {
        return loadPromise;
    }

    loadPromise = new Promise((resolve, reject) => {
        // If already available right now, resolve immediately
        if (window.google && window.google.maps) {
            isLoaded = true;
            resolve();
            return;
        }

        // Wire up the resolver so the callback (from index.html) can trigger us
        window.__resolveGoogleMapsLoad = resolve;
        window.__rejectGoogleMapsLoad = reject;

        // Safety net: poll until google.maps appears (in case of unusual timing)
        const pollInterval = setInterval(() => {
            if (window.google && window.google.maps) {
                clearInterval(pollInterval);
                isLoaded = true;
                resolve();
                window.__resolveGoogleMapsLoad = null;
                window.__rejectGoogleMapsLoad = null;
            }
        }, 50);

        // Hard timeout after 20 seconds
        setTimeout(() => {
            clearInterval(pollInterval);
            if (!isLoaded) {
                const err = new Error('Google Maps API failed to initialize (timeout)');
                if (window.__rejectGoogleMapsLoad) {
                    window.__rejectGoogleMapsLoad(err);
                    window.__rejectGoogleMapsLoad = null;
                }
                loadPromise = null; // allow retry on next call
                reject(err);
            }
        }, 20000);
    });

    return loadPromise;
}

/**
 * Returns the Map ID configured for this app (required for Advanced Markers).
 */
export function getMapId() {
    return MAP_ID;
}

/**
 * Returns whether the Google Maps API has finished loading.
 */
export function isGoogleMapsLoaded() {
    return isLoaded && !!(window.google && window.google.maps);
}
