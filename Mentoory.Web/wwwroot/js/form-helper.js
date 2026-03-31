// Mentoory - AJAX form submission helper

/**
 * Submit a form via AJAX
 * @param {HTMLFormElement} formElement - The form element
 * @param {object} [options] - Configuration options
 * @param {string} [options.successMessage] - Success toast message
 * @param {function} [options.onSuccess] - Callback on success
 * @param {function} [options.onError] - Callback on error
 * @returns {Promise<void>}
 */
async function submitForm(formElement, options) {
    options = options || {};
    var formData = new FormData(formElement);
    var tokenInput = formElement.querySelector('[name="__RequestVerificationToken"]');
    var headers = {};
    if (tokenInput) {
        headers['RequestVerificationToken'] = tokenInput.value;
    }

    try {
        var response = await fetch(formElement.action, {
            method: 'POST',
            headers: headers,
            body: formData
        });

        if (response.ok) {
            showToast(options.successMessage || 'Operación exitosa', 'success');
            if (options.onSuccess) {
                var data = null;
                var contentType = response.headers.get('content-type');
                if (contentType && contentType.indexOf('application/json') !== -1) {
                    data = await response.json();
                }
                options.onSuccess(data);
            }
        } else {
            var errorData = null;
            try {
                errorData = await response.json();
            } catch (e) {
                // Response is not JSON
            }
            showToast((errorData && errorData.message) || 'Error al guardar', 'danger');
            if (options.onError) options.onError(errorData);
        }
    } catch (err) {
        showToast('Error de conexión', 'danger');
    }
}
