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

window.application = {
    layoutComponent: null,
    registerLayoutComponent: (dotNetRef) => {
        this.layoutComponent = dotNetRef;
        console.log("Registered layout component");
    },
    unregisterLayoutComponent: () => {
        this.layoutComponent = null;
    },
    triggerUpdateCheck: async () => {
        if (this.layoutComponent) {
            await this.layoutComponent.invokeMethodAsync('ForceUpdateCheck');
        }
        else {
            window.location.reload(true);
        }
    },
    forceUpdateAndReload: async () => {
        if (!navigator.serviceWorker) {
            window.location.reload(true);
            return;
        }

        const registration = await navigator.serviceWorker.ready;

        await registration.update();

        if (registration.waiting) {
            registration.waiting.postMessage({ type: 'SKIP_WAITING' });
        }

        let refreshing = false;
        navigator.serviceWorker.addEventListener('controllerchange', () => {
            if (refreshing) return;
            refreshing = true;
            window.location.reload();
        });

        window.location.reload();
    }
}