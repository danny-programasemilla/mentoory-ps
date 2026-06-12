// Mentoory - Global utilities

/**
 * Show a Bootstrap toast notification
 * @param {string} message - The message to display
 * @param {string} type - Bootstrap color: success, danger, warning, info
 */
function showToast(message, type) {
    type = type || 'info';
    var container = document.getElementById('toastContainer');
    if (!container) return;

    var toastEl = document.createElement('div');
    toastEl.className = 'toast align-items-center text-bg-' + type + ' border-0';
    toastEl.setAttribute('role', 'alert');
    toastEl.setAttribute('aria-atomic', 'true');

    var wrapper = document.createElement('div');
    wrapper.className = 'd-flex';

    var bodyEl = document.createElement('div');
    bodyEl.className = 'toast-body';
    // Use textContent (not innerHTML) so the message is never parsed as HTML — defense-in-depth XSS hardening.
    bodyEl.textContent = message;

    var closeEl = document.createElement('button');
    closeEl.type = 'button';
    closeEl.className = 'btn-close btn-close-white me-2 m-auto';
    closeEl.setAttribute('data-bs-dismiss', 'toast');
    closeEl.setAttribute('aria-label', 'Cerrar');

    wrapper.appendChild(bodyEl);
    wrapper.appendChild(closeEl);
    toastEl.appendChild(wrapper);

    container.appendChild(toastEl);
    var toast = new bootstrap.Toast(toastEl, { delay: 5000 });
    toast.show();
    toastEl.addEventListener('hidden.bs.toast', function () {
        toastEl.remove();
    });
}

/**
 * Get the anti-forgery token value from a form or meta tag
 * @returns {string} The token value
 */
function getAntiForgeryToken() {
    var tokenInput = document.querySelector('[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}
