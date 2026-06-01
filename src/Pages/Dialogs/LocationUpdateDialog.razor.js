export function getCurrentPosition(options) {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject({ code: 0, message: "Geolocation is not supported by this browser." });
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy,
                    timestamp: position.timestamp
                });
            },
            (error) => {
                reject(error);
            },
            options
        );
    });
}