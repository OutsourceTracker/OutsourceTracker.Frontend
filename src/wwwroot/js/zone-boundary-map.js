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

const COLORS = {
    boundary: '#3b82f6',     // blue
    entry: '#22c55e',        // green
    exit: '#f97316',         // orange
    dock: '#8b5cf6',         // purple
    trailerpool: '#14b8a6'   // teal
};

export async function initBoundaryMap(elementId, centerLat, centerLng, zoomLevel, dotNetRef) {
    dotNetHelper = dotNetRef;

    try {
        const { loadGoogleMaps, getMapId } = await import('./google-maps-loader.js');
        await loadGoogleMaps();

        const mapElement = document.getElementById(elementId);
        if (!mapElement) {
            console.error("Map element not found:", elementId);
            return false;
        }

        mapInstance = new google.maps.Map(mapElement, {
            center: { lat: centerLat || 39.8283, lng: centerLng || -98.5795 },
            zoom: zoomLevel || 8,
            mapTypeControl: true,
            streetViewControl: false,
            fullscreenControl: true,
            clickableIcons: false,
            mapId: getMapId()   // Required for AdvancedMarkerElement
        });
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
