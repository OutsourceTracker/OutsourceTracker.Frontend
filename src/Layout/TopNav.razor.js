export function setupNavbarClose() {
    // Find all nav links inside the navbar (adjust selector if your structure differs)
    document.querySelectorAll('.navbar-nav .nav-link, .navbar-nav .dropdown-item').forEach(link => {
        link.addEventListener('click', () => {
            const collapseEl = document.querySelector('.navbar-collapse.show');
            if (collapseEl) {
                // Use Bootstrap 5 Collapse API to hide it
                const bsCollapse = bootstrap.Collapse.getInstance(collapseEl) || new bootstrap.Collapse(collapseEl, { toggle: false });
                bsCollapse.hide();
            }

            // Optional: reset toggler button aria/state
            const toggler = document.querySelector('.navbar-toggler');
            if (toggler) {
                toggler.classList.add('collapsed');
                toggler.setAttribute('aria-expanded', 'false');
            }
        });
    });
}