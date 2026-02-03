let mapInstance = null;

window.initMap = function (containerId) {
    const mapDiv = document.getElementById(containerId);
    if (!mapDiv) {
        console.error("Map container not found: #" + containerId);
        return false;
    }

    mapInstance = new google.maps.Map(mapDiv, {
        center: { lat: 45.5152, lng: -122.6784 }, // Portland default
        zoom: 12,
        mapTypeId: google.maps.MapTypeId.ROADMAP,
        mapId: '91655a72ee45e0e184bfe567'
    });

    console.log("Google Maps initialized");
    return true;
};

window.updateMapMarker = function (lat, lng, accuracyMeters) {
    if (!mapInstance) {
        console.warn("Map not initialized yet");
        return;
    }

    if (typeof lat !== 'number' || typeof lng !== 'number' || isNaN(lat) || isNaN(lng)) {
        console.error("Invalid coordinates:", lat, lng);
        return;
    }

    const position = { lat: lat, lng: lng };

    // Create Advanced Marker
    const marker = new google.maps.marker.AdvancedMarkerElement({
        position: position,
        map: mapInstance,
        title: "Trailer Location"
    });

    // Optional: Info Window
    const infoContent = `
        <div style="min-width:180px;">
            <strong>Last Spotted</strong><br>
            Accuracy: ${accuracyMeters > 0 ? accuracyMeters.toFixed(0) + ' m' : 'Unknown'}
        </div>`;

    const infowindow = new google.maps.InfoWindow({ content: infoContent });
    marker.addListener("gmp-click", () => infowindow.open({ anchor: marker, map: mapInstance }));

    // Accuracy circle (still uses classic Circle)
    if (accuracyMeters > 0) {
        new google.maps.Circle({
            strokeColor: "#007BFF",
            strokeOpacity: 0.8,
            strokeWeight: 2,
            fillColor: "#007BFF",
            fillOpacity: 0.2,
            map: mapInstance,
            center: position,
            radius: accuracyMeters
        });
    }

    // Center & zoom
    mapInstance.setCenter(position);
    mapInstance.setZoom(15);

    // Force resize after adding marker (helps in dynamic containers)
    google.maps.event.trigger(mapInstance, "resize");

    console.log("Marker added at", position);
};