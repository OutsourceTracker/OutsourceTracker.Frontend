

window.map = {
    mapInstance: null,
    mapMarkers: [],
    infoWindowOpen: null,

    initialize: async function (containerId, args = null) {
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

                if (this.mapInstance) {
                    this.mapInstance = null;
                }

                this.mapInstance = new google.maps.Map(mapDiv, args);
                google.maps.event.trigger(this.mapInstance, 'resize');
                google.maps.event.addListener(this.mapInstance, 'click', () => {
                    if (this.infoWindowOpen) {
                        this.infoWindowOpen.close();
                        this.infoWindowOpen = null;
                    }
                });

                // Re-initialize all pending/existing markers on the new map instance
                this.reinitializeMapMarkers();

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
    },

    reinitializeMapMarkers: async function () {
        this.mapMarkers.forEach(marker => {
            marker.markerInstance = null;
            marker.accuracyInstance = null;
            marker.infoInstance = null;

            if (marker.lat !== undefined && marker.lng !== undefined) {
                this.editMapMarker(marker.id, marker.title, marker.lat, marker.lng, marker.accuracyMeters, marker.info);
            }
        });
    },

    createMapMarker: async function (markerId, title, lat, lng, accuracyMeters, info = null) {
        if (!markerId || typeof markerId !== 'string') {
            console.error('Invalid marker ID:', markerId);
            return;
        }

        if (typeof lat !== 'number' || typeof lng !== 'number' || isNaN(lat) || isNaN(lng)) {
            console.error("Invalid coordinates:", lat, lng);
            return;
        }

        let marker = this.mapMarkers.find(m => m.id === markerId);

        if (marker) {
            // Update existing marker params
            marker.title = title;
            marker.lat = lat;
            marker.lng = lng;
            marker.accuracyMeters = accuracyMeters;
            marker.info = info;

            if (this.mapInstance) {
                this.editMapMarker(markerId, title, lat, lng, accuracyMeters, info);
            } else {
                console.warn("Map not initialized; marker params stored for later.");
            }
            return marker.id;
        }

        // Create new pending marker
        this.mapMarkers.push({
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

        if (this.mapInstance) {
            this.editMapMarker(markerId, title, lat, lng, accuracyMeters, info);
        } else {
            console.warn("Map not initialized; new marker stored for later initialization.");
        }

        console.log('Created/Stored map marker:', markerId);
        return markerId;
    },

    editMapMarker: async function (markerId, title, lat, lng, accuracyMeters, info = null) {
        if (!this.mapInstance) {
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

        const targetMarker = this.mapMarkers.find(m => m.id === markerId);
        if (targetMarker) {
            const position = { lat: lat, lng: lng };
            if (!targetMarker.markerInstance) {
                const markerInstance = new google.maps.marker.AdvancedMarkerElement({
                    map: this.mapInstance,
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
                        map: this.mapInstance,
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
                        if (this.infoWindowOpen) {
                            this.infoWindowOpen.close();
                        }
                        infowindow.open({ anchor: targetMarker.markerInstance, map: this.mapInstance });
                        this.infoWindowOpen = infowindow;

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
    },

    deleteMapMarker: async function (markerId) {
        if (!this.mapInstance) {
            console.warn("Map has not been initialized yet");
            return;
        }

        if (!markerId || typeof markerId !== 'string') {
            console.error('Unable to focus map marker:', markerId, '| Invalid ID');
            return;
        }

        const targetMarker = this.mapMarkers.find(m => m.id === markerId);

        if (targetMarker) {
            if (targetMarker.markerInstance) {
                targetMarker.markerInstance.map = null;
            }

            if (targetMarker.accuracyInstance) {
                targetMarker.accuracyInstance.setMap(null);
            }

            if (targetMarker.infoInstance) {
                targetMarker.infoInstance.close();
                if (this.openInfoWindow === targetMarker.infoInstance) {
                    this.openInfoWindow = null;
                }
            }

            const index = this.mapMarkers.indexOf(targetMarker);
            if (index > -1) this.mapMarkers.splice(index, 1);
            console.log('Deleted map marker', targetMarker.id, '(', targetMarker.markerInstance ? targetMarker.markerInstance.title : '', ')');
        }
        else {
            console.warn('Could not delete marker:', markerId, '[NOT FOUND]');
        }
    },

    focusMapMarker: async function (markerId, zoom) {
        if (!this.mapInstance) {
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

        const targetMarker = this.mapMarkers.find(m => m.id === markerId);

        if (targetMarker) {
            this.mapInstance.setCenter(targetMarker.markerInstance.position);
            if (zoom) this.mapInstance.setZoom(zoom);
            console.log('Set map focus to:', markerId);
        }
        else {
            console.error('Unable to focus map marker:', markerId, '| NOT FOUND');
        }
    },

    clearMapMarkers: async function () {
        const markers = [...this.mapMarkers];

        markers.forEach(mark => {
            this.deleteMapMarker(mark.id);
        })
    }
}