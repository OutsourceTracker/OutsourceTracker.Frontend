export function setupNavbarClose() {
    console.log('setupNavbarClose called from Blazor');

    const navbarCollapse = document.querySelector('#navbarNav');
    if (!navbarCollapse) return;

    document.addEventListener('click', function (event) {
        const isClickInside = navbarCollapse.contains(event.target) ||
            document.querySelector('.navbar-toggler').contains(event.target);
        if (!isClickInside && navbarCollapse.classList.contains('show')) {
            const toggler = document.querySelector('.navbar-toggler');
            toggler.click();  // Simulate toggle to close
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && navbarCollapse.classList.contains('show')) {
            document.querySelector('.navbar-toggler').click();
        }
    });
}