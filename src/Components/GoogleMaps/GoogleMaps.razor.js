const maps = new Map();

async function ensureGoogleMapsLoaded() {
    if (window.google?.maps?.importLibrary) return;

    return new Promise((resolve, reject) => {
        const config = {
            key: "AIzaSyDRaPp2QmC2QO47AypZnI8L4tLRuDWqd70",
            v: "weekly",
            language: "en",
            region: "US",
            authReferrerPolicy: "origin"
        };

        // Split declarations: use let for vars assigned later
        let h, a, k;
        const p = "The Google Maps JavaScript API";
        const c = "google";
        const l = "importLibrary";
        const q = "__ib__";
        const m = document;
        let b = window;
        b = b[c] || (b[c] = {});
        let d = b.maps || (b.maps = {});
        const r = new Set();
        const e = new URLSearchParams();

        const u = () => h || (h = new Promise(async (f, n) => {
            a = m.createElement("script");
            e.set("libraries", [...r] + "");
            for (k in config) {
                e.set(
                    k.replace(/[A-Z]/g, t => "_" + t[0].toLowerCase()),
                    config[k]
                );
            }
            e.set("callback", c + ".maps." + q);
            a.src = `https://maps.${c}apis.com/maps/api/js?` + e;
            d[q] = f;
            a.onerror = () => { h = n(Error(p + " could not load.")); };
            a.nonce = m.querySelector("script[nonce]")?.nonce || "";
            m.head.append(a);
        }));

        if (d[l]) {
            console.warn(p + " only loads once. Ignoring:", config);
        } else {
            d[l] = (f, ...n) => r.add(f) && u().then(() => d[l](f, ...n));
        }

        google.maps.importLibrary("maps")
            .then(resolve)
            .catch(reject);

        console.log(`Initialized Map Object!`);
    });
}

async function loadMarkerClusterer() {
    if (window.markerClusterer?.MarkerClusterer) {
        return;
    }

    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://unpkg.com/@googlemaps/markerclusterer@2/dist/index.min.js';
        script.async = true;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

export async function init(containerId, lat, lng, zoom, dotNetRef, mapId = 'DEMO_MAP_ID') {
    console.log(`[GoogleMaps] init called for container: ${containerId}`);
    await ensureGoogleMapsLoaded();
    await loadMarkerClusterer();

    const { Map } = await google.maps.importLibrary("maps");
    const { AdvancedMarkerElement, PinElement } = await google.maps.importLibrary("marker");
    await google.maps.importLibrary("geometry");

    const map = new Map(document.getElementById(containerId), {
        center: { lat, lng },
        zoom,
        mapId,
        gestureHandling: 'greedy',
        mapTypeId: 'hybrid'
    });

    const clusterer = new markerClusterer.MarkerClusterer({
        map,
        markers: [],
        renderer: {
            render: ({ count, position }) => new google.maps.Marker({
                label: { text: String(count), color: 'white', fontSize: '12px' },
                position,
                icon: {
                    url: 'https://developers.google.com/maps/documentation/javascript/examples/markerclusterer/m1.png',
                    scaledSize: new google.maps.Size(53, 53)
                },
                zIndex: 1000 + count
            })
        }
    });

    maps.set(containerId, {
        map,
        clusterer,
        markers: [],
        polygons: [],
        circles: [],
        infoWindows: [],
        currentLocationMarker: null,
        waveCircle: null,
        watchId: null,
        lastUpdateTime: null,
        dotNetRef,
        AdvancedMarkerElement,
        PinElement,
        geometry: google.maps.geometry
    });

    await dotNetRef.invokeMethodAsync("OnMapInitialized", containerId);
    console.log(`[GoogleMaps] init completed for ${containerId}`);
}

export function addPin(containerId, options) {
    console.debug(`addPin: Called for ${containerId}`, options);
    const ctx = maps.get(containerId);
    if (!ctx) {
        console.warn(`[GoogleMaps] addPin: context not found for ${containerId}`);
        return;
    }

    if (!options || !options.position) {
        console.error("[addPin] Missing or invalid options.Position", options);
        return;
    }

    const pos = options.position;
    const lat = pos.lat;
    const lng = pos.lng;

    if (typeof lat !== 'number' || typeof lng !== 'number' || isNaN(lat) || isNaN(lng)) {
        console.error("[addPin] Invalid lat/lng values", { lat, lng, fullPos: pos });
        return;
    }

    const position = { lat, lng };

    let pinContent = null;

    if (options.customColor || options.glyph || options.glyphText || options.glyphSrc) {
        const pinOptions = {
            background: options.customColor || '#FF0000',
            borderColor: options.borderColor || '#000',
            glyphColor: options.glyphColor || 'white',
            scale: options.scale || 1.2,
            glyphText: options.glyphText || options.glyph || undefined,
            glyphSrc: options.glyphSrc || undefined                     
        };

        if (options.glyph && !options.glyphSrc && !options.glyphText) {
            pinOptions.glyphText = options.glyph;
        }

        const pin = new ctx.PinElement(pinOptions);
        pinContent = pin;
    } else if (options.iconUrl) {
        const img = document.createElement('img');
        img.src = options.iconUrl;
        img.style.width = '32px';
        img.style.height = '32px';
        img.style.borderRadius = '50%';
        img.style.objectFit = 'cover';
        pinContent = img;
    }

    const marker = new ctx.AdvancedMarkerElement({
        map: ctx.map,
        position,
        title: options.title,
        content: pinContent
    });

    ctx.markers.push(marker);
    ctx.clusterer?.addMarker(marker);

    if (options.content) {
        const infoWindow = new google.maps.InfoWindow({ content: options.content });
        marker.addListener('gmp-click', () => {
            ctx.infoWindows.forEach(iw => iw.close());
            infoWindow.open({ anchor: marker, map: ctx.map });
        });
        ctx.infoWindows.push(infoWindow);
    }

    if (options.radius) {
        const circle = new google.maps.Circle({
            map: ctx.map,
            center: position,
            radius: options.radius,
            strokeColor: '#FF0000',
            strokeOpacity: 0.8,
            strokeWeight: 2,
            fillColor: '#FF0000',
            fillOpacity: 0.35
        });
        ctx.circles.push(circle);
    }
}

export function addZone(containerId, options) {
    const ctx = maps.get(containerId);
    if (!ctx) return;

    const polygon = new google.maps.Polygon({
        paths: options.paths,
        strokeColor: options.strokeColor || '#008000',
        strokeOpacity: 0.8,
        strokeWeight: 2,
        fillColor: options.fillColor || '#00FF0044',
        fillOpacity: 0.35,
        map: ctx.map
    });
    ctx.polygons.push(polygon);

    options.Entries.forEach(e => {
        new google.maps.Marker({
            position: { lat: e.lat, lng: e.lng },
            map: ctx.map,
            icon: 'http://maps.google.com/mapfiles/ms/icons/green-dot.png',
            title: 'Entry Point'
        });
    });

    options.Exits.forEach(x => {
        new google.maps.Marker({
            position: { lat: x.lat, lng: x.lng },
            map: ctx.map,
            icon: 'http://maps.google.com/mapfiles/ms/icons/red-dot.png',
            title: 'Exit Point'
        });
    });
}

export function toggleCurrentLocation(containerId, show, intervalMs) {
    const ctx = maps.get(containerId);
    if (!ctx) return;

    if (!show) {
        if (ctx.watchId) navigator.geolocation.clearWatch(ctx.watchId);
        if (ctx.currentLocationMarker) ctx.currentLocationMarker.map = null;
        if (ctx.waveCircle) ctx.waveCircle.setMap(null);
        ctx.watchId = ctx.currentLocationMarker = ctx.waveCircle = null;
        ctx.lastUpdateTime = null;
        return;
    }

    ctx.watchId = navigator.geolocation.watchPosition(
        pos => {
            const now = Date.now();
            const frequency = ctx.lastUpdateTime ? now - ctx.lastUpdateTime : 0;
            ctx.lastUpdateTime = now;

            const lat = pos.coords.latitude;
            const lng = pos.coords.longitude;
            const position = { lat, lng };

            if (!ctx.currentLocationMarker) {
                ctx.currentLocationMarker = new ctx.AdvancedMarkerElement({
                    map: ctx.map,
                    position,
                    title: 'Current Location',
                    content: new ctx.PinElement({
                        background: '#1E90FF',
                        glyph: '📍',
                        glyphColor: 'white',
                        scale: 1.4
                    }).element
                });
            } else {
                ctx.currentLocationMarker.position = position;
            }

            if (ctx.waveCircle) ctx.waveCircle.setMap(null);
            ctx.waveCircle = new google.maps.Circle({
                map: ctx.map,
                center: position,
                radius: 15,
                strokeColor: '#1E90FF',
                strokeOpacity: 0.6,
                fillColor: '#1E90FF',
                fillOpacity: 0.2
            });

            let r = 15, op = 0.2;
            const anim = setInterval(() => {
                r += 8;
                op -= 0.04;
                if (op <= 0) {
                    clearInterval(anim);
                    ctx.waveCircle.setMap(null);
                } else {
                    ctx.waveCircle.setRadius(r);
                    ctx.waveCircle.setOptions({ fillOpacity: op, strokeOpacity: op });
                }
            }, 120);

            ctx.dotNetRef.invokeMethodAsync('UpdateLocation', lat, lng, frequency);
        },
        err => console.error('Geolocation error:', err),
        { enableHighAccuracy: true, timeout: intervalMs, maximumAge: 0 }
    );
}

export function fitToAllMarkers(containerId, padding = 60) {
    const ctx = maps.get(containerId);
    if (!ctx) return;

    const bounds = new google.maps.LatLngBounds();

    ctx.markers.forEach(m => {
        if (m.position) bounds.extend(m.position);
    });

    ctx.polygons.forEach(p => {
        p.getPaths().forEach(path => {
            path.forEach(ll => bounds.extend(ll));
        });
    });

    if (ctx.currentLocationMarker?.position) {
        bounds.extend(ctx.currentLocationMarker.position);
    }

    if (!bounds.isEmpty()) {
        ctx.map.fitBounds(bounds, padding);
    }
}

export function clearPins(containerId) {
    const ctx = maps.get(containerId);
    if (!ctx) return;

    ctx.clusterer?.clearMarkers();
    ctx.markers.forEach(m => { m.map = null; });
    ctx.markers = [];
    ctx.circles.forEach(c => c.setMap(null));
    ctx.circles = [];
}

export function computeDistance(containerId, lat1, lng1, lat2, lng2) {
    const ctx = maps.get(containerId);
    if (!ctx?.geometry?.spherical) return null;

    const from = new google.maps.LatLng(lat1, lng1);
    const to = new google.maps.LatLng(lat2, lng2);
    return google.maps.geometry.spherical.computeDistanceBetween(from, to);
}

export function focusOnPosition(containerId, lat, lng, zoomLevel = null) {
    const ctx = maps.get(containerId);
    if (!ctx) return;

    const position = { lat, lng };

    if (zoomLevel !== null) {
        ctx.map.setZoom(zoomLevel);
        ctx.map.setCenter(position);
    } else {
        ctx.map.panTo(position);
    }
}

export function focusOnMarker(containerId, markerIndex, zoomLevel = 15) {
    const ctx = maps.get(containerId);
    if (!ctx || markerIndex < 0 || markerIndex >= ctx.markers.length) return;

    const marker = ctx.markers[markerIndex];
    const position = marker.position;

    ctx.map.setZoom(zoomLevel);
    ctx.map.setCenter(position);

    if (marker.content) {
        marker.content.classList.add('marker-focus-pulse');
        setTimeout(() => marker.content.classList.remove('marker-focus-pulse'), 2000);
    }
}

export function focusOnZone(containerId, zoneIndex) {
    const ctx = maps.get(containerId);
    if (!ctx || zoneIndex < 0 || zoneIndex >= ctx.polygons.length) return;

    const polygon = ctx.polygons[zoneIndex];
    const bounds = new google.maps.LatLngBounds();

    polygon.getPaths().forEach(path => {
        path.forEach(ll => bounds.extend(ll));
    });

    if (!bounds.isEmpty()) {
        ctx.map.fitBounds(bounds, 60);
    }
}

console.log('[GoogleMaps module] loaded as ES module');