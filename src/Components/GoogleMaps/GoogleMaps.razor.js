window.GoogleMaps = window.GoogleMaps || {};
GoogleMaps.maps = GoogleMaps.maps || {};

GoogleMaps.ensureLoaded = function (apiKey) {
    return new Promise((resolve, reject) => {
        if (window.google?.maps?.importLibrary) {
            resolve();
            return;
        }

        const script = document.createElement('script');
        script.async = true;
        script.defer = true;

        const params = new URLSearchParams({
            key: apiKey,
            v: 'weekly',
            callback: 'google.maps.__ib__'
        });

        script.src = `https://maps.googleapis.com/maps/api/js?${params}`;

        window.google = window.google || {};
        window.google.maps = window.google.maps || {};
        const maps = window.google.maps;

        maps.__ib__ = () => {
            maps.importLibrary = (name) => Promise.resolve(maps[name] || Promise.reject(`Library ${name} unavailable`));
            resolve();
        };

        script.onerror = () => reject(new Error('Google Maps load failed'));
        document.head.appendChild(script);
    });
};

GoogleMaps.loadMarkerClusterer = function () {
    return new Promise((resolve, reject) => {
        if (window.markerClusterer?.MarkerClusterer) {
            resolve();
            return;
        }
        const script = document.createElement('script');
        script.src = 'https://unpkg.com/@googlemaps/markerclusterer@2/dist/index.min.js';
        script.async = true;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
};

GoogleMaps.init = async function (containerId, apiKey, lat, lng, zoom, dotNetRef, mapId = 'DEMO_MAP_ID') {
    await GoogleMaps.ensureLoaded(apiKey);
    await GoogleMaps.loadMarkerClusterer();

    const { Map } = await google.maps.importLibrary('maps');
    const { AdvancedMarkerElement, PinElement } = await google.maps.importLibrary('marker');
    await google.maps.importLibrary('geometry');

    const map = new Map(document.getElementById(containerId), {
        center: { lat, lng },
        zoom,
        mapId,
        gestureHandling: 'greedy'
    });

    const clusterer = new markerClusterer.MarkerClusterer({
        map,
        markers: [],
        renderer: {
            render: ({ count, position }) => new google.maps.Marker({
                label: { text: String(count), color: 'white', fontSize: '12px' },
                position,
                icon: { url: 'https://developers.google.com/maps/documentation/javascript/examples/markerclusterer/m1.png', scaledSize: new google.maps.Size(53, 53) },
                zIndex: 1000 + count
            })
        }
    });

    GoogleMaps.maps[containerId] = {
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
    };
};

GoogleMaps.addPin = function (containerId, options) {
    const ctx = GoogleMaps.maps[containerId];
    if (!ctx) return;

    const position = { lat: options.Position.Lat, lng: options.Position.Lng };

    let pinContent;
    if (options.CustomColor || options.Glyph) {
        pinContent = new ctx.PinElement({
            glyph: options.Glyph || undefined,
            glyphColor: options.GlyphColor || 'white',
            background: options.CustomColor || '#FF0000',
            borderColor: options.BorderColor || '#000',
            scale: options.Scale || 1.2
        }).element;
    } else if (options.IconUrl) {
        pinContent = document.createElement('img');
        pinContent.src = options.IconUrl;
        pinContent.style.width = '32px';
        pinContent.style.height = '32px';
        pinContent.style.borderRadius = '50%';
    }

    const marker = new ctx.AdvancedMarkerElement({
        map: ctx.map,
        position,
        title: options.Title,
        content: pinContent
    });

    ctx.markers.push(marker);
    if (ctx.clusterer) ctx.clusterer.addMarker(marker);

    if (options.Content) {
        const infoWindow = new google.maps.InfoWindow({ content: options.Content });
        marker.addListener('gmp-click', () => {
            ctx.infoWindows.forEach(iw => iw.close());
            infoWindow.open({ anchor: marker, map: ctx.map });
        });
        ctx.infoWindows.push(infoWindow);
    }

    if (options.Radius) {
        const circle = new google.maps.Circle({
            map: ctx.map,
            center: position,
            radius: options.Radius,
            strokeColor: '#FF0000',
            strokeOpacity: 0.8,
            strokeWeight: 2,
            fillColor: '#FF0000',
            fillOpacity: 0.35
        });
        ctx.circles.push(circle);
    }
};

GoogleMaps.addZone = function (containerId, options) {
    const ctx = GoogleMaps.maps[containerId];
    if (!ctx) return;

    const polygon = new google.maps.Polygon({
        paths: options.Paths,
        strokeColor: options.StrokeColor || '#008000',
        strokeOpacity: 0.8,
        strokeWeight: 2,
        fillColor: options.FillColor || '#00FF0044',
        fillOpacity: 0.35,
        map: ctx.map
    });
    ctx.polygons.push(polygon);

    options.Entries.forEach(e => {
        new google.maps.Marker({
            position: { lat: e.Lat, lng: e.Lng },
            map: ctx.map,
            icon: 'http://maps.google.com/mapfiles/ms/icons/green-dot.png',
            title: 'Entry'
        });
    });

    options.Exits.forEach(x => {
        new google.maps.Marker({
            position: { lat: x.Lat, lng: x.Lng },
            map: ctx.map,
            icon: 'http://maps.google.com/mapfiles/ms/icons/red-dot.png',
            title: 'Exit'
        });
    });
};

GoogleMaps.toggleCurrentLocation = function (containerId, show, intervalMs) {
    const ctx = GoogleMaps.maps[containerId];
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

            // Breathing wave
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
        err => console.error(err),
        { enableHighAccuracy: true, timeout: intervalMs, maximumAge: 0 }
    );
};

GoogleMaps.fitToAllMarkers = function (containerId, padding = 60) {
    const ctx = GoogleMaps.maps[containerId];
    if (!ctx || (ctx.markers.length === 0 && ctx.polygons.length === 0 && !ctx.currentLocationMarker)) return;

    const bounds = new google.maps.LatLngBounds();

    ctx.markers.forEach(m => {
        const pos = m.position;
        if (pos) bounds.extend(pos);
    });

    ctx.polygons.forEach(p => {
        p.getPaths().forEach(path => {
            path.forEach(ll => bounds.extend(ll));
        });
    });

    if (ctx.currentLocationMarker) {
        const pos = ctx.currentLocationMarker.position;
        if (pos) bounds.extend(pos);
    }

    if (!bounds.isEmpty()) {
        ctx.map.fitBounds(bounds, padding);
    }
};

GoogleMaps.clearPins = function (containerId) {
    const ctx = GoogleMaps.maps[containerId];
    if (!ctx) return;
    ctx.clusterer.clearMarkers();
    ctx.markers.forEach(m => { m.map = null; });
    ctx.markers = [];
    ctx.circles.forEach(c => c.setMap(null));
    ctx.circles = [];
};

GoogleMaps.computeDistance = function (containerId, lat1, lng1, lat2, lng2) {
    const ctx = GoogleMaps.maps[containerId];
    if (!ctx?.geometry?.spherical) return null;
    const from = new google.maps.LatLng(lat1, lng1);
    const to = new google.maps.LatLng(lat2, lng2);
    return google.maps.geometry.spherical.computeDistanceBetween(from, to);
};

GoogleMaps.focusOnPosition = function (containerId, lat, lng, zoomLevel = null) {
    const ctx = GoogleMaps.maps[containerId];
    if (!ctx) return;

    const position = { lat, lng };

    if (zoomLevel !== null) {
        ctx.map.setZoom(zoomLevel);
        ctx.map.setCenter(position);
    } else {
        ctx.map.panTo(position);
    }
};

GoogleMaps.focusOnMarker = function (containerId, markerIndex, zoomLevel = 15) {
    const ctx = GoogleMaps.maps[containerId];
    if (!ctx || markerIndex < 0 || markerIndex >= ctx.markers.length) return;

    const marker = ctx.markers[markerIndex];
    const position = marker.position;

    ctx.map.setZoom(zoomLevel);
    ctx.map.setCenter(position);

    if (marker.content) {
        marker.content.classList.add('marker-focus-pulse');
        setTimeout(() => marker.content.classList.remove('marker-focus-pulse'), 2000);
    }
};

GoogleMaps.focusOnZone = function (containerId, zoneIndex) {
    const ctx = GoogleMaps.maps[containerId];
    if (!ctx || zoneIndex < 0 || zoneIndex >= ctx.polygons.length) return;

    const polygon = ctx.polygons[zoneIndex];
    const bounds = new google.maps.LatLngBounds();

    polygon.getPaths().forEach(path => {
        path.forEach(ll => bounds.extend(ll));
    });

    if (!bounds.isEmpty()) {
        ctx.map.fitBounds(bounds, 60);
    }
};

console.log('GoogleMaps fully loaded with Advanced Markers, clustering & geometry');