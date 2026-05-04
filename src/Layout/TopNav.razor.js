export function setupNavbarClose() {
    console.log('setupNavbarClose called from Blazor');

    const navbarCollapse = document.querySelector('#navbarNav');
    if (!navbarCollapse) return;

    document.addEventListener('click', function (event) {
        const isClickInside = navbarCollapse.contains(event.target) ||
            document.querySelector('.navbar-toggler')?.contains(event.target);
        if (!isClickInside && navbarCollapse.classList.contains('show')) {
            const toggler = document.querySelector('.navbar-toggler');
            toggler?.click();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && navbarCollapse.classList.contains('show')) {
            document.querySelector('.navbar-toggler')?.click();
        }
    });
}

export function collapseNavbar() {
    const collapseEl = document.getElementById('navbarNav');
    if (!collapseEl) return;

    const bsCollapse = bootstrap.Collapse.getOrCreateInstance(collapseEl);
    if (bsCollapse) {
        bsCollapse.hide();
    }
}