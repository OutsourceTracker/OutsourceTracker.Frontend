window.showBootstrapModal = (id) => {
    new bootstrap.Modal(document.getElementById(id)).show();
};

window.hideBootstrapModal = (id) => {
    bootstrap.Modal.getInstance(document.getElementById(id))?.hide();
};

window.getClientTimezone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;
window.getClientLanguage = () => navigator.language || 'en-US';

window.navigator = window.navigator || {};

window.computeSha256Hex = async (input) => {
    if (!input || typeof input !== 'string') {
        console.warn('computeSha256Hex received non-string input');
        return '';
    }

    try {
        const email = input.trim().toLowerCase();
        const encoder = new TextEncoder();
        const data = encoder.encode(email);

        const hashBuffer = await crypto.subtle.digest('SHA-256', data);

        // Convert to lowercase hex string
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    } catch (err) {
        console.error('SHA-256 computation failed:', err);
        return '';
    }
};

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