let mapInstance = null;
const markers = [];
let openInfoWindow = null;

window.initMap = async function (containerId, args = null) {
    let attempts = 0;
    const maxAttempts = 5;

    while (attempts < maxAttempts) {
        const mapDiv = document.getElementById(containerId);
        if (mapDiv) {
            if (!args) {
                args = {
                    center: { lat: 47.23299, lng: -122.22583 },
                    zoom: 15,
                    mapTypeId: 'satellite'
                };
            }

            // Clear existing map instance if it exists (for page navigation)
            if (mapInstance) {
                mapInstance = null;
            }

            mapInstance = new google.maps.Map(mapDiv, args);

            // Trigger resize for Bootstrap responsive containers
            google.maps.event.trigger(mapInstance, 'resize');

            // Add map click listener to close open InfoWindow
            google.maps.event.addListener(mapInstance, 'click', () => {
                if (openInfoWindow) {
                    openInfoWindow.close();
                    openInfoWindow = null;
                }
            });

            // Re-initialize all pending/existing markers on the new map instance
            initializePendingMarkers();

            console.log("Google Maps initialized");
            return true;
        }

        attempts++;
        if (attempts < maxAttempts) {
            console.log(`Map container not found, retrying in 1 second... (Attempt ${attempts})`);
            await new Promise(resolve => setTimeout(resolve, 1000));
        }
    }

    console.error(`Map container not found after ${maxAttempts} attempts: #${containerId}`);
    return false;
};

function initializePendingMarkers() {
    markers.forEach(marker => {
        // Reset instances to null to force recreation
        marker.markerInstance = null;
        marker.accuracyInstance = null;
        marker.infoInstance = null;

        if (marker.lat !== undefined && marker.lng !== undefined) {
            editMapMarker(marker.id, marker.title, marker.lat, marker.lng, marker.accuracyMeters, marker.info);
        }
    });
}

window.createMapMarker = function (markerId, title, lat, lng, accuracyMeters, info = null) {
    if (!markerId || typeof markerId !== 'string') {
        console.error('Invalid marker ID:', markerId);
        return;
    }

    if (typeof lat !== 'number' || typeof lng !== 'number' || isNaN(lat) || isNaN(lng)) {
        console.error("Invalid coordinates:", lat, lng);
        return;
    }

    let marker = markers.find(m => m.id === markerId);

    if (marker) {
        // Update existing marker params
        marker.title = title;
        marker.lat = lat;
        marker.lng = lng;
        marker.accuracyMeters = accuracyMeters;
        marker.info = info;

        if (mapInstance) {
            editMapMarker(markerId, title, lat, lng, accuracyMeters, info);
        } else {
            console.warn("Map not initialized; marker params stored for later.");
        }
        return marker.id;
    }

    // Create new pending marker
    markers.push({
        id: markerId,
        title,
        lat,
        lng,
        accuracyMeters,
        info,
        markerInstance: null,
        accuracyInstance: null,
        infoInstance: null
    });

    if (mapInstance) {
        editMapMarker(markerId, title, lat, lng, accuracyMeters, info);
    } else {
        console.warn("Map not initialized; new marker stored for later initialization.");
    }

    console.log('Created/Stored map marker:', markerId);
    return markerId;
}

window.editMapMarker = function (markerId, title, lat, lng, accuracyMeters, info = null) {
    if (!mapInstance) {
        // If map not ready, update params only (via createMapMarker logic)
        console.warn("Map has not been initialized yet; updating params only.");
        return;
    }

    if (!markerId || typeof markerId !== 'string') {
        console.error('Invalid marker ID:', markerId);
        return;
    }

    if (typeof lat !== 'number' || typeof lng !== 'number' || isNaN(lat) || isNaN(lng)) {
        console.error("Invalid coordinates:", lat, lng);
        return;
    }

    const targetMarker = markers.find(m => m.id === markerId);
    if (targetMarker) {
        const position = { lat: lat, lng: lng };
        if (!targetMarker.markerInstance) {
            const markerInstance = new google.maps.marker.AdvancedMarkerElement({
                map: mapInstance,
                position: position,
                title: title
            });

            targetMarker.markerInstance = markerInstance;
        }
        else {
            targetMarker.markerInstance.position = position;
        }

        if (!targetMarker.accuracyInstance) {
            if (accuracyMeters > 15) {
                targetMarker.accuracyInstance = new google.maps.Circle({
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
        }
        else {
            if (accuracyMeters > 15) {
                targetMarker.accuracyInstance.center = position;
                targetMarker.accuracyInstance.radius = accuracyMeters;
            }
            else {
                targetMarker.accuracyInstance.setMap(null);
                targetMarker.accuracyInstance = null;
            }
        }

        if (targetMarker.infoInstance) {
            if (info && typeof info === 'string') {
                targetMarker.infoInstance.setContent(info);
            }
        }
        else {
            if (info && typeof info === 'string') {
                const infowindow = new google.maps.InfoWindow({ content: info });
                targetMarker.markerInstance.addListener("gmp-click", () => {
                    if (openInfoWindow) {
                        openInfoWindow.close();
                    }
                    infowindow.open({ anchor: targetMarker.markerInstance, map: mapInstance });
                    openInfoWindow = infowindow;

                    const element = document.getElementById(markerId);
                    if (element) {
                        element.click();
                    } else {
                        console.debug(`Element with ID ${markerId} not found`);
                    }
                });
                targetMarker.infoInstance = infowindow;
            }
        }

        // Update stored params for future reference
        targetMarker.title = title;
        targetMarker.lat = lat;
        targetMarker.lng = lng;
        targetMarker.accuracyMeters = accuracyMeters;
        targetMarker.info = info;

        console.log('Updated position of marker: ' + markerId);
    }
    else {
        console.warn('Failed to update map marker: ' + markerId);
    }
}

window.deleteMapMarker = function (markerId) {
    if (!mapInstance) {
        console.warn("Map has not been initialized yet");
        return;
    }

    if (!markerId || typeof markerId !== 'string') {
        console.error('Unable to focus map marker:', markerId, '| Invalid ID');
        return;
    }

    const targetMarker = markers.find(m => m.id === markerId);

    if (targetMarker) {
        if (targetMarker.markerInstance) {
            targetMarker.markerInstance.map = null;
        }

        if (targetMarker.accuracyInstance) {
            targetMarker.accuracyInstance.setMap(null);
        }

        if (targetMarker.infoInstance) {
            targetMarker.infoInstance.close();
            if (openInfoWindow === targetMarker.infoInstance) {
                openInfoWindow = null;
            }
        }

        const index = markers.indexOf(targetMarker);
        if (index > -1) markers.splice(index, 1);
        console.log('Deleted map marker', targetMarker.id, '(', targetMarker.markerInstance ? targetMarker.markerInstance.title : '', ')');
    }
    else {
        console.warn('Could not delete marker:', markerId, '[NOT FOUND]');
    }
}

window.focusMapMarker = function (markerId, zoom) {
    if (!mapInstance) {
        console.warn("Map has not been initialized yet");
        return;
    }

    if (!markerId || typeof markerId !== 'string') {
        console.error('Unable to focus map marker:', markerId, '| Invalid ID');
        return;
    }

    if (typeof zoom !== 'number' || isNaN(zoom)) {
        zoom = null;
    }

    const targetMarker = markers.find(m => m.id === markerId);

    if (targetMarker) {
        mapInstance.setCenter(targetMarker.markerInstance.position);
        if (zoom) mapInstance.setZoom(zoom);
        console.log('Set map focus to:', markerId);
    }
    else {
        console.error('Unable to focus map marker:', markerId, '| NOT FOUND');
    }
}