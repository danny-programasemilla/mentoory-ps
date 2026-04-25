// Event-delegated actions for the Knowledge template list page.
// Reads URL templates from data-* attributes on the root container so the view
// can stay pure markup per constitution § VIII.
(function () {
    'use strict';

    var root = document.getElementById('knowledge-templates-list');
    if (!root) {
        return;
    }

    var urls = {
        archive: root.dataset.urlArchiveTemplate,
        unarchive: root.dataset.urlUnarchiveTemplate,
        delete: root.dataset.urlDeleteTemplate
    };
    var placeholder = '00000000-0000-0000-0000-000000000000';

    function getToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function postAction(url) {
        return fetch(url, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getToken(),
                'Accept': 'application/json'
            }
        }).then(function (r) { return r.json(); });
    }

    function toast(msg, type) {
        if (typeof window.showToast === 'function') {
            window.showToast(msg, type);
        } else {
            alert(msg);
        }
    }

    root.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-action]');
        if (!btn) {
            return;
        }

        var id = btn.getAttribute('data-template-id');
        if (!id) {
            return;
        }

        var action = btn.getAttribute('data-action');
        var url = null;

        if (action === 'archive-template') {
            if (!confirm('¿Archivar esta plantilla?')) {
                return;
            }
            url = urls.archive.replace(placeholder, id);
        } else if (action === 'unarchive-template') {
            url = urls.unarchive.replace(placeholder, id);
        } else if (action === 'delete-template') {
            var name = btn.getAttribute('data-template-name') || '';
            if (!confirm('¿Eliminar permanentemente la plantilla "' + name + '"? Esta acción no se puede deshacer.')) {
                return;
            }
            url = urls.delete.replace(placeholder, id);
        }

        if (!url) {
            return;
        }

        postAction(url).then(function (data) {
            if (data && data.success) {
                if (data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                } else {
                    window.location.reload();
                }
            } else {
                toast((data && data.message) || 'No se pudo completar la acción.', 'danger');
            }
        }).catch(function () {
            toast('Error de red. Intente nuevamente.', 'danger');
        });
    });
})();
