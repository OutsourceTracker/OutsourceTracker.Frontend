let mapInstance = null;
let markers = [];
let dotNetHelper = null;

// Zone visualization
let zonePolygons = [];
let zoneLabels = [];

// Current user location (non-interactive, auto-updating)
let currentLocationMarker = null;
let currentLocationAccuracyCircle = null;
let geolocationWatchId = null;
let hasAutoCenteredOnUser = false;  // one-time, only for the main trailer live map

export async function initMap(elementId, centerLat, centerLng, zoomLevel, dotNetRef) {
    dotNetHelper = dotNetRef;

    try {
        const { loadGoogleMaps, getMapId } = await import('./google-maps-loader.js');
        await loadGoogleMaps();

        const mapElement = document.getElementById(elementId);
        if (!mapElement) {
            console.error("Map element not found:", elementId);
            return null;
        }

        mapInstance = new google.maps.Map(mapElement, {
            center: { lat: centerLat || 39.8283, lng: centerLng || -98.5795 },
            zoom: zoomLevel || 4,
            mapTypeControl: true,
            streetViewControl: false,
            fullscreenControl: true,
            mapId: getMapId()   // Required for AdvancedMarkerElement
        });

        return true;
    } catch (err) {
        console.error('Failed to load Google Maps API:', err);
        return false;
    }
}

export async function addTrailerMarkers(trailers) {
    if (!mapInstance) {
        console.warn("Map not initialized yet — waiting briefly...");
        // Defensive wait in case of timing issue between initMap and addTrailerMarkers
        await new Promise(r => setTimeout(r, 150));
        if (!mapInstance) {
            console.error("Map still not initialized. Skipping marker render.");
            return;
        }
    }

    clearMarkers();

    const bounds = new google.maps.LatLngBounds();
    let hasLocations = false;

    trailers.forEach(trailer => {
        if (!trailer.location || !trailer.location.x || !trailer.location.y) {
            return;
        }

        const position = {
            lat: trailer.location.x,
            lng: trailer.location.y
        };

        const color = getMarkerColor(trailer.state);

        const infoContent = `
            <div style="min-width: 220px; font-family: sans-serif; line-height: 1.4;">
                <h4 style="margin: 0 0 6px 0; font-size: 15px;">${trailer.fullName}</h4>
                <div><strong>Status:</strong> ${trailer.state}</div>
                <div><strong>Type:</strong> ${trailer.type}</div>
                ${trailer.accountName ? `<div><strong>Account:</strong> ${trailer.accountShortCode} - ${trailer.accountName}</div>` : ''}
                ${trailer.zoneName ? `<div><strong>Zone:</strong> ${trailer.zoneName}</div>` : ''}
                ${trailer.locatedByName ? `<div><strong>Located by:</strong> ${trailer.locatedByName}</div>` : ''}
                ${trailer.locatedDate ? `<div style="font-size: 12px; color: #666; margin-top: 4px;">${new Date(trailer.locatedDate).toLocaleString()}</div>` : ''}
            </div>
        `;

        const infoWindow = new google.maps.InfoWindow({
            content: infoContent
        });

        // Create marker content with PinElement (preferred) + DOM fallback for robustness
        let content;
        try {
            if (google.maps.marker && google.maps.marker.PinElement) {
                const pin = new google.maps.marker.PinElement({
                    background: color,
                    borderColor: "#333",
                    glyphColor: "#fff",
                    scale: 1.0
                });
                content = pin;   // Pass PinElement directly (element property is deprecated)
            } else {
                throw new Error("PinElement not available");
            }
        } catch {
            // Fallback: simple colored circle using DOM (works even without full Advanced Markers support)
            content = document.createElement('div');
            content.style.cssText = `
                background: ${color};
                width: 14px;
                height: 14px;
                border-radius: 50%;
                border: 2px solid #333;
                box-shadow: 0 1px 4px rgba(0,0,0,0.4);
            `;
        }

        const marker = new google.maps.marker.AdvancedMarkerElement({
            position: position,
            map: mapInstance,
            title: trailer.fullName,
            content: content
        });

        marker.trailerId = trailer.id;
        marker._infoWindow = infoWindow;   // Store for focusOnTrailer

        // Use gmp-click for AdvancedMarkerElement (avoids deprecation warning)
        marker.addListener("gmp-click", () => {
            infoWindow.open({ anchor: marker, map: mapInstance });
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync("OnMarkerClicked", trailer.id);
            }
        });

        markers.push(marker);
        bounds.extend(position);
        hasLocations = true;
    });

    if (hasLocations) {
        mapInstance.fitBounds(bounds);
        const listener = google.maps.event.addListenerOnce(mapInstance, "idle", () => {
            if (mapInstance.getZoom() > 15) {
                mapInstance.setZoom(15);
            }
        });
    }
}

function getMarkerColor(state) {
    switch (state) {
        case "Available":
            return "#22c55e"; // green
        case "Dispatched":
            return "#3b82f6"; // blue
        case "MechanicallyDown":
            return "#ef4444"; // red
        default:
            return "#6b7280"; // gray
    }
}

export function focusOnTrailer(trailerId) {
    if (!mapInstance) return;

    const marker = markers.find(m => m.trailerId === trailerId);
    if (!marker) return;

    // AdvancedMarkerElement uses .position (can be LatLng or LatLngLiteral)
    const pos = marker.position;
    if (pos) {
        const lat = typeof pos.lat === 'function' ? pos.lat() : pos.lat;
        const lng = typeof pos.lng === 'function' ? pos.lng() : pos.lng;
        mapInstance.panTo({ lat, lng });
        mapInstance.setZoom(Math.max(mapInstance.getZoom() || 4, 13));
    }

    // Open the stored InfoWindow anchored to the AdvancedMarkerElement (most reliable)
    if (marker._infoWindow) {
        marker._infoWindow.open({ anchor: marker, map: mapInstance });
    } else {
        // Fallback: trigger the modern gmp-click event on AdvancedMarkerElement
        google.maps.event.trigger(marker, 'gmp-click');
    }
}

export function clearMarkers() {
    markers.forEach(marker => {
        if (marker) marker.map = null;
    });
    markers = [];

    // Also clear any zone overlays when refreshing trailer markers
    clearZones();
}

function clearZones() {
    zonePolygons.forEach(p => {
        if (p) p.setMap(null);
    });
    zoneLabels.forEach(l => {
        if (l) l.map = null;
    });
    zonePolygons = [];
    zoneLabels = [];
}

export function centerOnLocation(lat, lng, zoom = 12) {
    if (mapInstance) {
        mapInstance.setCenter({ lat, lng });
        mapInstance.setZoom(zoom);
    }
}

// ==================== CURRENT USER LOCATION (non-interactive, auto-updating) ====================

export function startUserLocationTracking() {
    if (!navigator.geolocation) {
        console.warn('Geolocation is not supported by this browser.');
        return;
    }
    if (geolocationWatchId !== null) {
        // already active
        return;
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
            // Non-fatal: user may have denied, or GPS unavailable. Just log.
            console.debug('Geolocation watch error (non-fatal):', err.code, err.message);
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

    // Create or update the "you are here" dot (AdvancedMarker for consistency)
    if (!currentLocationMarker) {
        // Custom DOM content: blue dot, completely non-interactive
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
        // Intentionally no click/drag listeners attached.
    } else {
        currentLocationMarker.position = position;
    }

    // One-time auto center for the "Live Location Map" view (does not apply to zone editor).
    // Only happens the very first time we receive a position after page load.
    if (!hasAutoCenteredOnUser && mapInstance) {
        mapInstance.setCenter(position);
        mapInstance.setZoom(13);
        hasAutoCenteredOnUser = true;
    }

    // Accuracy circle (visual only, never clickable)
    if (accuracyMeters > 5) {  // ignore tiny/meaningless accuracy
        if (!currentLocationAccuracyCircle) {
            currentLocationAccuracyCircle = new google.maps.Circle({
                strokeColor: '#1a73e8',
                strokeOpacity: 0.35,
                strokeWeight: 1,
                fillColor: '#1a73e8',
                fillOpacity: 0.12,
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
}

/**
 * Draws zone boundaries as polygons and places the zone name as a label near the center.
 * Expects an array of zone objects with: id, shortCode, fullName, boundryPoints[]
 */
export function addZonePolygons(zones) {
    if (!mapInstance || !zones || zones.length === 0) return;

    clearZones();

    zones.forEach(zone => {
        if (!zone.boundryPoints || zone.boundryPoints.length < 3) return;

        // Convert to Google LatLng format (note: our Vector2 uses x=lat, y=lng)
        const path = zone.boundryPoints.map(p => ({
            lat: p.x,
            lng: p.y
        }));

        // Draw the polygon (light fill + visible border)
        const polygon = new google.maps.Polygon({
            paths: path,
            strokeColor: "#3b82f6",
            strokeOpacity: 0.85,
            strokeWeight: 2.5,
            fillColor: "#3b82f6",
            fillOpacity: 0.06,
            clickable: false,
            map: mapInstance
        });
        zonePolygons.push(polygon);

        // Calculate simple centroid for label placement
        let sumLat = 0, sumLng = 0;
        path.forEach(pt => {
            sumLat += pt.lat;
            sumLng += pt.lng;
        });
        const centerLat = sumLat / path.length;
        const centerLng = sumLng / path.length;

        // Create a nice label using AdvancedMarkerElement with custom HTML
        const labelContent = document.createElement('div');
        labelContent.style.cssText = `
            background: rgba(255,255,255,0.92);
            color: #1e40af;
            font-size: 11px;
            font-weight: 700;
            padding: 2px 8px;
            border-radius: 4px;
            border: 1px solid #3b82f6;
            box-shadow: 0 1px 4px rgba(0, 0, 0, 0.25);
            white-space: nowrap;
            pointer-events: none;
            user-select: none;
        `;
        labelContent.textContent = zone.shortCode || zone.fullName || "Zone";

        const labelMarker = new google.maps.marker.AdvancedMarkerElement({
            position: { lat: centerLat, lng: centerLng },
            map: mapInstance,
            content: labelContent,
            zIndex: 999
        });
        zoneLabels.push(labelMarker);
    });
}
