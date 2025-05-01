(function () {
    document.addEventListener('DOMContentLoaded', function () {
        const header = document.querySelector('header.header-area');
        let lastScrollTop = 0;

        // Add transition CSS directly to header element for longer animation
        if (header) {
            header.style.transition = "all 0.6s ease-in-out"; // Longer duration (was likely 0.3s before)
        }

        // Function to handle header visibility based on scroll direction
        function handleHeaderScroll() {
            let scrollTop = window.pageYOffset || document.documentElement.scrollTop;

            // Make sure header always exists
            if (header) {
                // Determine scroll direction
                if (scrollTop > lastScrollTop) {
                    // Scrolling DOWN
                    header.classList.remove('background-header');
                } else {
                    // Scrolling UP
                    if (scrollTop > 30) { // Only apply when not at the very top
                        header.classList.add('background-header');
                    } else {
                        header.classList.remove('background-header');
                    }
                }

                // Force header to be always visible
                header.style.display = 'block';
            }

            lastScrollTop = scrollTop <= 0 ? 0 : scrollTop; // For Mobile or negative scrolling
        }

        // Add scroll event listener
        window.addEventListener('scroll', handleHeaderScroll);

        // Run on page load
        handleHeaderScroll();
    });
})();