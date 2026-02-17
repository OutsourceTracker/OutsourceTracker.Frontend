window.showBootstrapModal = (id) => {
    new bootstrap.Modal(document.getElementById(id)).show();
};

window.hideBootstrapModal = (id) => {
    bootstrap.Modal.getInstance(document.getElementById(id))?.hide();
};

window.getClientTimezone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;
window.getClientLanguage = () => navigator.language || 'en-US';

window.navigator = window.navigator || {};
navigator.geolocation = navigator.geolocation || {};
navigator.geolocation.getCurrentPositionWrapper = function () {
    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            position => resolve(position),
            error => reject(error),
            {
                enableHighAccuracy: true,
                timeout: 15000,
                maximumAge: 0
            }
        );
    });
};