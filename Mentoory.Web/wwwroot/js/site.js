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
    toastEl.innerHTML =
        '<div class="d-flex">' +
            '<div class="toast-body">' + message + '</div>' +
            '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Cerrar"></button>' +
        '</div>';

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
