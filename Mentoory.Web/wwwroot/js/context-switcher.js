// Mentoory - Context switcher for top-bar AJAX context switching

/**
 * Switch the active context via AJAX
 * @param {string} roleAssignmentExternalId - The GUID of the role assignment to activate
 */
function switchContext(roleAssignmentExternalId) {
    var token = getAntiForgeryToken();

    fetch('/api/context/switch', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ roleAssignmentExternalId: roleAssignmentExternalId })
    })
    .then(function (response) {
        if (response.ok) {
            showToast('Contexto actualizado exitosamente', 'success');
            setTimeout(function () {
                window.location.reload();
            }, 500);
        } else {
            return response.json().then(function (data) {
                showToast(data.message || 'Error al cambiar el contexto', 'danger');
            });
        }
    })
    .catch(function () {
        showToast('Error de conexión al cambiar el contexto', 'danger');
    });
}

/**
 * Initialize context switcher dropdown event handlers
 */
function initContextSwitcher() {
    var contextItems = document.querySelectorAll('[data-context-switch]');
    contextItems.forEach(function (item) {
        item.addEventListener('click', function (e) {
            e.preventDefault();
            var externalId = this.getAttribute('data-context-switch');
            switchContext(externalId);
        });
    });
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initContextSwitcher);
