// Mentoory - Context switcher for top-bar AJAX context switching

var _areaRoleMap = {
    '/Administration/': ['IncubatorAdmin', 'GlobalAdmin'],
    '/Coordination/': ['ProjectCoordinator', 'Mentor', 'IncubatorAdmin', 'GlobalAdmin'],
    '/Participant/': ['Entrepreneur', 'Mentor', 'ProjectCoordinator', 'IncubatorAdmin', 'GlobalAdmin'],
    '/Platform/': ['GlobalAdmin']
};

/**
 * Check if forms on the page have unsaved changes
 */
function hasUnsavedChanges() {
    var forms = document.querySelectorAll('form[data-track-changes]');
    for (var i = 0; i < forms.length; i++) {
        if (forms[i].dataset.dirty === 'true') {
            return true;
        }
    }
    return false;
}

/**
 * Check if the given role can access the current URL area
 */
function canAccessCurrentArea(newRole) {
    var currentPath = window.location.pathname;
    for (var area in _areaRoleMap) {
        if (currentPath.indexOf(area) === 0) {
            return _areaRoleMap[area].indexOf(newRole) !== -1;
        }
    }
    return true;
}

/**
 * Switch the active context via AJAX
 * @param {string} roleAssignmentExternalId - The GUID of the role assignment to activate
 * @param {string} [newRole] - The role being switched to, for safe navigation
 */
function switchContext(roleAssignmentExternalId, newRole) {
    if (hasUnsavedChanges()) {
        if (!confirm('Tiene cambios sin guardar. ¿Desea cambiar el contexto de todas formas?')) {
            return;
        }
    }

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
                if (newRole && !canAccessCurrentArea(newRole)) {
                    window.location.href = '/';
                } else {
                    window.location.reload();
                }
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
 * Initialize context switcher dropdown event handlers and form change tracking
 */
function initContextSwitcher() {
    var contextItems = document.querySelectorAll('[data-context-switch]');
    contextItems.forEach(function (item) {
        item.addEventListener('click', function (e) {
            e.preventDefault();
            var externalId = this.getAttribute('data-context-switch');
            var role = this.getAttribute('data-context-role');
            switchContext(externalId, role);
        });
    });

    // Track form changes for unsaved-changes warning
    var forms = document.querySelectorAll('form[data-track-changes]');
    forms.forEach(function (form) {
        form.addEventListener('input', function () {
            form.dataset.dirty = 'true';
        });
        form.addEventListener('submit', function () {
            form.dataset.dirty = 'false';
        });
    });
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initContextSwitcher);
