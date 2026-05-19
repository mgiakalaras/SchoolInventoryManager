(function () {
    function updateMobileShell() {
        var root = document.documentElement;
        var topbar = document.querySelector('.topbar');
        var bottomNav = document.querySelector('.mobile-bottom-nav');

        if (topbar) {
            var topbarHeight = Math.ceil(topbar.getBoundingClientRect().height);
            root.style.setProperty('--mobile-topbar-height', topbarHeight + 'px');
        }

        if (bottomNav) {
            var bottomNavHeight = Math.ceil(bottomNav.getBoundingClientRect().height);
            root.style.setProperty('--mobile-bottom-nav-height', bottomNavHeight + 'px');
        }

        if (window.visualViewport) {
            root.style.setProperty('--visual-viewport-height', Math.round(window.visualViewport.height) + 'px');
        }
    }

    updateMobileShell();
    document.addEventListener('DOMContentLoaded', updateMobileShell);
    window.addEventListener('load', updateMobileShell, { passive: true });
    window.addEventListener('resize', updateMobileShell, { passive: true });
    window.addEventListener('orientationchange', function () {
        setTimeout(updateMobileShell, 150);
        setTimeout(updateMobileShell, 450);
    }, { passive: true });

    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', updateMobileShell, { passive: true });
        window.visualViewport.addEventListener('scroll', updateMobileShell, { passive: true });
    }
})();
