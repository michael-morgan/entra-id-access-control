// ============================================================================
// Access Control Admin - Site JavaScript
// ============================================================================

// ============================================================================
// Navigation Active State Management
// ============================================================================

/**
 * Sets the active state on navigation items based on the current controller.
 * This function runs on DOM ready and highlights the appropriate nav item.
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        // Get the current controller from the nav container's data attribute
        const navContainer = document.querySelector('.navbar-nav[data-controller]');
        if (!navContainer) return;

        const currentController = navContainer.getAttribute('data-controller');
        if (!currentController) return;

        // Find all nav links with data-nav-target attribute
        const navLinks = document.querySelectorAll('.nav-link[data-nav-target]');

        navLinks.forEach(function (link) {
            const targets = link.getAttribute('data-nav-target');
            if (!targets) return;

            // Split comma-separated targets
            const targetControllers = targets.split(',').map(t => t.trim());

            // Check if current controller matches any of the targets
            if (targetControllers.includes(currentController)) {
                // Add active class to the link
                link.classList.add('active');

                // If it's a dropdown, also mark the parent nav-item as active
                const parentNavItem = link.closest('.nav-item');
                if (parentNavItem && parentNavItem.classList.contains('dropdown')) {
                    parentNavItem.classList.add('active');
                }
            }
        });
    });
})();

// ============================================================================
// Alert Auto-Dismiss
// ============================================================================

/**
 * Auto-dismiss alert messages after 5 seconds.
 * Applies to alerts with the 'alert-dismissible' class.
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const alerts = document.querySelectorAll('.alert.alert-dismissible');

        alerts.forEach(function (alert) {
            // Auto-dismiss after 5 seconds
            setTimeout(function () {
                const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
                bsAlert.close();
            }, 5000);
        });
    });
})();

// ============================================================================
// Form Validation Enhancement
// ============================================================================

/**
 * Enhances form validation by adding visual feedback.
 * Works with Bootstrap 5's validation classes.
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        // Fetch all the forms we want to apply custom Bootstrap validation styles to
        const forms = document.querySelectorAll('.needs-validation');

        // Loop over them and prevent submission if invalid
        Array.from(forms).forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (!form.checkValidity()) {
                    event.preventDefault();
                    event.stopPropagation();
                }

                form.classList.add('was-validated');
            }, false);
        });
    });
})();

// ============================================================================
// Smooth Scroll to Anchor Links
// ============================================================================

/**
 * Adds smooth scrolling behavior to anchor links within the page.
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const anchorLinks = document.querySelectorAll('a[href^="#"]:not([href="#"])');

        anchorLinks.forEach(function (link) {
            link.addEventListener('click', function (e) {
                const targetId = this.getAttribute('href').substring(1);
                const targetElement = document.getElementById(targetId);

                if (targetElement) {
                    e.preventDefault();
                    targetElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });

                    // Update URL without jumping
                    if (history.pushState) {
                        history.pushState(null, null, '#' + targetId);
                    }
                }
            });
        });
    });
})();

// ============================================================================
// Tooltip Initialization
// ============================================================================

/**
 * Initializes Bootstrap tooltips for elements with data-bs-toggle="tooltip".
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        const tooltipTriggerList = [].slice.call(
            document.querySelectorAll('[data-bs-toggle="tooltip"]')
        );

        tooltipTriggerList.forEach(function (tooltipTriggerEl) {
            new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });
})();

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Debounce function to limit the rate at which a function can fire.
 * Useful for search inputs, window resize events, etc.
 *
 * @param {Function} func - The function to debounce
 * @param {number} wait - The number of milliseconds to delay
 * @param {boolean} immediate - Trigger the function on the leading edge instead of trailing
 * @returns {Function} - The debounced function
 */
function debounce(func, wait, immediate) {
    let timeout;
    return function executedFunction() {
        const context = this;
        const args = arguments;

        const later = function () {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };

        const callNow = immediate && !timeout;

        clearTimeout(timeout);
        timeout = setTimeout(later, wait);

        if (callNow) func.apply(context, args);
    };
}

/**
 * Gets the anti-forgery token value from the hidden form field.
 * Used for AJAX requests that require CSRF protection.
 *
 * @returns {string|null} - The anti-forgery token value or null if not found
 */
function getAntiForgeryToken() {
    const tokenInput = document.querySelector(
        '#antiforgeryTokenContainer input[name="__RequestVerificationToken"]'
    );
    return tokenInput ? tokenInput.value : null;
}

/**
 * Makes an AJAX request with anti-forgery token included.
 *
 * @param {string} url - The URL to send the request to
 * @param {string} method - The HTTP method (GET, POST, PUT, DELETE, etc.)
 * @param {object} data - The data to send (will be JSON stringified for POST/PUT)
 * @returns {Promise} - A promise that resolves with the response
 */
function makeAuthenticatedRequest(url, method = 'GET', data = null) {
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
        },
    };

    // Add anti-forgery token for state-changing requests
    if (['POST', 'PUT', 'DELETE', 'PATCH'].includes(method.toUpperCase())) {
        const token = getAntiForgeryToken();
        if (token) {
            options.headers['RequestVerificationToken'] = token;
        }
    }

    // Add body for requests that support it
    if (data && ['POST', 'PUT', 'PATCH'].includes(method.toUpperCase())) {
        options.body = JSON.stringify(data);
    }

    return fetch(url, options)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        });
}

// Export utility functions to global scope if needed
window.AppUtils = {
    debounce: debounce,
    getAntiForgeryToken: getAntiForgeryToken,
    makeAuthenticatedRequest: makeAuthenticatedRequest
};
