// Zone Boundary Editor - Google Maps interop
// Reuses the same API key as the trailer map.

let mapInstance = null;
let dotNetHelper = null;

let boundaryMarkers = [];
let entryMarkers = [];
let exitMarkers = [];
let dockMarkers = [];
let trailerPoolMarkers = [];

let boundaryPolygon = null;
let activeMode = 'none'; // 'boundary' | 'entry' | 'exit' | 'dock' | 'trailerpool' | 'none'

// Current user location (purely visual reference; must never interfere with drawing/clicks/drags)
let currentLocationMarker = null;
let currentLocationAccuracyCircle = null;
let geolocationWatchId = null;
let lastKnownUserLocation = null;  // {lat, lng, accuracy} - kept in sync with the visual marker

let hasAutoFocusedOnUser = false;
let usedDefaultCenterForInit = false;

const COLORS = {
    boundary: '#3b82f6',     // blue
    entry: '#22c55e',        // green
    exit: '#f97316',         // orange
    dock: '#8b5cf6',         // purple
    trailerpool: '#14b8a6'   // teal
};

export async function initBoundaryMap(elementId, centerLat, centerLng, zoomLevel, dotNetRef) {
    dotNetHelper = dotNetRef;

    // Reset per-open state so focus logic applies fresh each time the boundary map dialog is opened.
    hasAutoFocusedOnUser = false;
    usedDefaultCenterForInit = false;

    try {
        const { loadGoogleMaps, getMapId } = await import('./google-maps-loader.js');
        await loadGoogleMaps();

        const mapElement = document.getElementById(elementId);
        if (!mapElement) {
            console.error("Map element not found:", elementId);
            return false;
        }

        const defaultLat = 39.8283;
        const defaultLng = -98.5795;
        const initialLat = centerLat || defaultLat;
        const initialLng = centerLng || defaultLng;
        usedDefaultCenterForInit = (Math.abs(initialLat - defaultLat) < 0.5 && Math.abs(initialLng - defaultLng) < 0.5);

        mapInstance = new google.maps.Map(mapElement, {
            center: { lat: initialLat, lng: initialLng },
            zoom: zoomLevel || 8,
            mapTypeControl: true,
            streetViewControl: false,
            fullscreenControl: true,
            clickableIcons: false,
            mapId: getMapId()   // Required for AdvancedMarkerElement
        });

        // If we already have a last known location (e.g. user clicked "add current" button
        // before the map finished initializing), place the marker immediately.
        if (lastKnownUserLocation) {
            updateCurrentLocationMarker(
                lastKnownUserLocation.lat,
                lastKnownUserLocation.lng,
                lastKnownUserLocation.accuracy || 0
            );
            // If we were using the default "nowhere" center (typical for new zone), focus on user loc.
            if (usedDefaultCenterForInit && !hasAutoFocusedOnUser) {
                centerOnPoint(lastKnownUserLocation.lat, lastKnownUserLocation.lng, 15);
                hasAutoFocusedOnUser = true;
            }
        }
    } catch (err) {
        console.error('Failed to load Google Maps API:', err);
        return false;
    }

    // Click handler for adding points
    mapInstance.addListener('click', (e) => {
        if (!activeMode || activeMode === 'none' || !dotNetHelper) return;

        const lat = e.latLng.lat();
        const lng = e.latLng.lng();

        // Notify Blazor (it will update the list and call back to redraw)
        dotNetHelper.invokeMethodAsync('OnMapPointAdded', activeMode, lat, lng);
    });

    return true;
}

export function setDrawingMode(mode) {
    activeMode = mode || 'none';
    if (mapInstance) {
        mapInstance.setOptions({ draggableCursor: activeMode !== 'none' ? 'crosshair' : null });
    }
}

export function loadPoints(boundaryPoints, entryPoints, exitPoints, dockPoints, trailerPoolPoints) {
    if (!mapInstance) {
        console.warn("Map not initialized");
        return;
    }

    clearAllInternal();

    // Boundary polygon + markers
    const boundaryLatLngs = [];
    (boundaryPoints || []).forEach((pt, idx) => {
        if (!pt || typeof pt.x !== 'number' || typeof pt.y !== 'number') return;
        const position = { lat: pt.x, lng: pt.y };
        boundaryLatLngs.push(position);

        const marker = createPointMarker(position, 'boundary', idx);
        boundaryMarkers.push(marker);
    });

    if (boundaryLatLngs.length >= 3) {
        boundaryPolygon = new google.maps.Polygon({
            paths: boundaryLatLngs,
            strokeColor: COLORS.boundary,
            strokeOpacity: 0.9,
            strokeWeight: 2,
            fillColor: COLORS.boundary,
            fillOpacity: 0.15,
            clickable: false,
            map: mapInstance
        });
    }

    // Other point types
    (entryPoints || []).forEach((pt, idx) => addSinglePointMarker(pt, 'entry', idx));
    (exitPoints || []).forEach((pt, idx) => addSinglePointMarker(pt, 'exit', idx));
    (dockPoints || []).forEach((pt, idx) => addSinglePointMarker(pt, 'dock', idx));
    (trailerPoolPoints || []).forEach((pt, idx) => addSinglePointMarker(pt, 'trailerpool', idx));

    // IMPORTANT: Do NOT call fitToAllPoints() here.
}

function addSinglePointMarker(pt, type, index) {
    if (!pt || typeof pt.x !== 'number' || typeof pt.y !== 'number') return;
    const position = { lat: pt.x, lng: pt.y };
    const marker = createPointMarker(position, type, index);

    if (type === 'entry') entryMarkers.push(marker);
    else if (type === 'exit') exitMarkers.push(marker);
    else if (type === 'dock') dockMarkers.push(marker);
    else if (type === 'trailerpool') trailerPoolMarkers.push(marker);
}

function createPointMarker(position, type, index) {
    const color = COLORS[type] || '#666';

    // Create content with PinElement (preferred) + simple DOM fallback
    let content;
    try {
        if (google.maps.marker && google.maps.marker.PinElement) {
            const pin = new google.maps.marker.PinElement({
                background: color,
                borderColor: "#111",
                glyphColor: "#fff",
                scale: type === 'boundary' ? 0.9 : 0.75
            });
            content = pin;   // Pass PinElement directly (element property is deprecated)
        } else {
            throw new Error("PinElement unavailable");
        }
    } catch {
        // Fallback DOM element (colored circle)
        content = document.createElement('div');
        content.style.cssText = `
            background: ${color};
            width: 12px;
            height: 12px;
            border-radius: 50%;
            border: 2px solid #111;
            box-shadow: 0 1px 3px rgba(0,0,0,0.3);
        `;
    }

    const marker = new google.maps.marker.AdvancedMarkerElement({
        position,
        map: mapInstance,
        title: `${type} #${index + 1}`,
        content: content,
        gmpDraggable: true
    });

    marker.pointType = type;
    marker.pointIndex = index;

    // Drag end → notify Blazor
    marker.addListener('dragend', () => {
        const pos = marker.position; // AdvancedMarkerElement uses .position
        if (dotNetHelper && marker.pointType && typeof marker.pointIndex === 'number' && pos) {
            const lat = typeof pos.lat === 'function' ? pos.lat() : pos.lat;
            const lng = typeof pos.lng === 'function' ? pos.lng() : pos.lng;
            dotNetHelper.invokeMethodAsync(
                'OnMapPointMoved',
                marker.pointType,
                marker.pointIndex,
                lat,
                lng
            );
        }
    });

    // Click to select / future delete
    // Use gmp-click instead of click for AdvancedMarkerElement (avoids deprecation warning)
    marker.addListener('gmp-click', () => {
        if (dotNetHelper && marker.pointType && typeof marker.pointIndex === 'number') {
            const pos = marker.position;
            if (pos) {
                const lat = typeof pos.lat === 'function' ? pos.lat() : pos.lat;
                const lng = typeof pos.lng === 'function' ? pos.lng() : pos.lng;
                mapInstance.panTo({ lat, lng });
            }
        }
    });

    return marker;
}

function clearAllInternal() {
    // Clear markers (AdvancedMarkerElement uses .map = null)
    [...boundaryMarkers, ...entryMarkers, ...exitMarkers, ...dockMarkers, ...trailerPoolMarkers].forEach(m => {
        if (m) m.map = null;
    });
    boundaryMarkers = [];
    entryMarkers = [];
    exitMarkers = [];
    dockMarkers = [];
    trailerPoolMarkers = [];

    // Clear polygon
    if (boundaryPolygon) {
        boundaryPolygon.setMap(null);
        boundaryPolygon = null;
    }
}

export function clearAll() {
    clearAllInternal();
}

export function fitToAllPoints() {
    if (!mapInstance) return;

    const bounds = new google.maps.LatLngBounds();
    let hasPoints = false;

    const allMarkers = [...boundaryMarkers, ...entryMarkers, ...exitMarkers, ...dockMarkers];
    allMarkers.forEach(m => {
        const pos = m.position;
        if (pos) {
            // pos can be LatLng or LatLngLiteral
            const lat = typeof pos.lat === 'function' ? pos.lat() : pos.lat;
            const lng = typeof pos.lng === 'function' ? pos.lng() : pos.lng;
            bounds.extend({ lat, lng });
            hasPoints = true;
        }
    });

    if (hasPoints) {
        mapInstance.fitBounds(bounds);
        const listener = google.maps.event.addListenerOnce(mapInstance, 'idle', () => {
            const z = mapInstance.getZoom();
            if (z > 16) mapInstance.setZoom(16);
        });
    }
}

export function centerOnPoint(lat, lng, zoom = 15) {
    if (mapInstance) {
        mapInstance.panTo({ lat, lng });
        mapInstance.setZoom(zoom);
    }
}

// Optional: allow Blazor to force a full redraw after external edits
export function refreshMap() {
    // Currently loadPoints is the main way to refresh
    console.log('refreshMap called (no-op, use loadPoints)');
}

// ==================== CURRENT USER LOCATION (non-interactive, auto-updating) ====================
// IMPORTANT: This marker must never be clickable, draggable, or capture map clicks.
// It must not affect border creation / point placement in any drawing mode.

export function startUserLocationTracking() {
    if (!navigator.geolocation) {
        console.warn('Geolocation is not supported by this browser.');
        return;
    }
    if (geolocationWatchId !== null) {
        return; // already tracking
    }

    const options = {
        enableHighAccuracy: true,
        timeout: 15000,
        maximumAge: 5000
    };

    geolocationWatchId = navigator.geolocation.watchPosition(
        (pos) => {
            const lat = pos.coords.latitude;
            const lng = pos.coords.longitude;
            const acc = pos.coords.accuracy || 0;
            updateCurrentLocationMarker(lat, lng, acc);
        },
        (err) => {
            console.debug('Geolocation watch error (non-fatal for zone editor):', err.code, err.message);
        },
        options
    );
}

export function stopUserLocationTracking() {
    if (geolocationWatchId !== null) {
        navigator.geolocation.clearWatch(geolocationWatchId);
        geolocationWatchId = null;
    }
    removeCurrentLocationMarker();
}

function updateCurrentLocationMarker(lat, lng, accuracyMeters = 0) {
    if (!mapInstance) return;

    const position = { lat, lng };

    if (!currentLocationMarker) {
        // Distinct blue "you are here" dot. pointer-events:none + no listeners = completely inert.
        const dot = document.createElement('div');
        dot.style.cssText = `
            width: 14px;
            height: 14px;
            background-color: #1a73e8;
            border: 2px solid #ffffff;
            border-radius: 50%;
            box-shadow: 0 0 0 3px rgba(26, 115, 232, 0.35);
            pointer-events: none;
            user-select: none;
        `;

        currentLocationMarker = new google.maps.marker.AdvancedMarkerElement({
            position: position,
            map: mapInstance,
            content: dot,
            title: 'Your current location',
            zIndex: 2000
        });
        // Deliberately: no gmp-click, no drag listeners, no title click behavior.
    } else {
        currentLocationMarker.position = position;
    }

    lastKnownUserLocation = { lat, lng, accuracy: accuracyMeters };

    // Auto focus the view to user's current location the *first* time we receive a position
    // after the map opened, *but only* if the initial center was the default (i.e. no
    // existing points were provided for a new boundary). This ensures "focus to users
    // current location when it is opened" without disrupting editors for existing zones.
    if (!hasAutoFocusedOnUser && usedDefaultCenterForInit && mapInstance) {
        centerOnPoint(lat, lng, 15);
        hasAutoFocusedOnUser = true;
    }

    // Accuracy halo - visual only
    if (accuracyMeters > 5) {
        if (!currentLocationAccuracyCircle) {
            currentLocationAccuracyCircle = new google.maps.Circle({
                strokeColor: '#1a73e8',
                strokeOpacity: 0.3,
                strokeWeight: 1,
                fillColor: '#1a73e8',
                fillOpacity: 0.1,
                map: mapInstance,
                center: position,
                radius: accuracyMeters,
                clickable: false,
                zIndex: 1999
            });
        } else {
            currentLocationAccuracyCircle.setCenter(position);
            currentLocationAccuracyCircle.setRadius(accuracyMeters);
        }
    } else if (currentLocationAccuracyCircle) {
        currentLocationAccuracyCircle.setMap(null);
        currentLocationAccuracyCircle = null;
    }
}

function removeCurrentLocationMarker() {
    if (currentLocationMarker) {
        currentLocationMarker.map = null;
        currentLocationMarker = null;
    }
    if (currentLocationAccuracyCircle) {
        currentLocationAccuracyCircle.setMap(null);
        currentLocationAccuracyCircle = null;
    }
    lastKnownUserLocation = null;
    hasAutoFocusedOnUser = false;
    usedDefaultCenterForInit = false;
}

// Returns the last known user location (from the live watch or a fresh one-shot).
// This is preferred over separate geolocation calls because the map is already
// tracking it for the visual marker, and it keeps everything in sync.
export function getCurrentUserLocation() {
    if (lastKnownUserLocation) {
        return { ...lastKnownUserLocation };
    }
    return null;
}

export async function getCurrentUserLocationAsync() {
    const known = getCurrentUserLocation();
    if (known) return known;

    if (!navigator.geolocation) {
        return null;
    }

    return await new Promise((resolve) => {
        navigator.geolocation.getCurrentPosition(
            (pos) => {
                const loc = {
                    lat: pos.coords.latitude,
                    lng: pos.coords.longitude,
                    accuracy: pos.coords.accuracy || 0
                };
                lastKnownUserLocation = loc;
                // Update the visual marker immediately so it appears/refreshes
                try {
                    updateCurrentLocationMarker(loc.lat, loc.lng, loc.accuracy);
                } catch (e) { /* ignore */ }
                resolve({ ...loc });
            },
            (err) => {
                console.debug('One-shot geolocation for add-pin failed:', err);
                resolve(null);
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            }
        );
    });
}
