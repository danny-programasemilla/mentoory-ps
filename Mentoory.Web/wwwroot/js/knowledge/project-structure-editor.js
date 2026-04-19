// Mentoory - Knowledge structure clone (project) editor (US2)
// Vanilla ES5-compatible JS. Reads its configuration from data-* attributes on the
// root `[data-structure-id]` card (set by ProjectStructureDetail.cshtml), keeping
// the view free of script blocks per constitution § VIII. Uses the global
// showToast() helper from site.js.

(function () {
    'use strict';

    var root = document.querySelector('[data-structure-id]');
    if (!root) {
        return;
    }

    var ctx = {
        structureExternalId: root.dataset.structureId,
        moduleCount: parseInt(root.dataset.moduleCount || '0', 10),
        urls: {
            updateStructure: root.dataset.urlUpdateStructure,
            setSyncMode: root.dataset.urlSetSyncMode,
            syncFromTemplate: root.dataset.urlSyncFromTemplate,
            addModule: root.dataset.urlAddModule,
            updateModule: root.dataset.urlUpdateModule,
            deleteModule: root.dataset.urlDeleteModule,
            addTopic: root.dataset.urlAddTopic,
            updateTopic: root.dataset.urlUpdateTopic,
            deleteTopic: root.dataset.urlDeleteTopic,
            updateTopicRanges: root.dataset.urlUpdateTopicRanges,
            addSubject: root.dataset.urlAddSubject,
            updateSubject: root.dataset.urlUpdateSubject,
            deleteSubject: root.dataset.urlDeleteSubject,
            addResource: root.dataset.urlAddResource,
            updateResource: root.dataset.urlUpdateResource,
            deleteResource: root.dataset.urlDeleteResource
        },
        guidPlaceholder: root.dataset.guidPlaceholder || '00000000-0000-0000-0000-000000000000'
    };

    // -------------------------------------------------------------------------
    // HTTP helpers
    // -------------------------------------------------------------------------

    function getAntiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function fetchJson(url, method, body) {
        var options = {
            method: method || 'POST',
            headers: {
                'Accept': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            }
        };
        if (body !== undefined && body !== null) {
            options.headers['Content-Type'] = 'application/json';
            options.body = JSON.stringify(body);
        }
        return fetch(url, options).then(function (response) {
            return response.json().catch(function () { return { success: false, message: 'Respuesta inválida del servidor.' }; });
        });
    }

    function toast(message, type) {
        if (typeof window.showToast === 'function') {
            window.showToast(message, type || 'info');
        } else {
            alert(message);
        }
    }

    function replaceGuid(urlTemplate, guid) {
        return urlTemplate.replace(ctx.guidPlaceholder, guid);
    }

    function handleResponse(data, successMessage) {
        if (data && data.success) {
            toast(successMessage, 'success');
            window.setTimeout(function () { window.location.reload(); }, 400);
            return true;
        }
        toast((data && data.message) || 'No se pudo completar la acción.', 'danger');
        return false;
    }

    function networkError() {
        toast('Error de red. Intente nuevamente.', 'danger');
    }

    // -------------------------------------------------------------------------
    // Modal helpers
    // -------------------------------------------------------------------------

    var modalEl = document.getElementById('knowledgeModal');
    var modalTitle = document.getElementById('knowledgeModalTitle');
    var modalBody = document.getElementById('knowledgeModalBody');
    var modalSubmit = document.getElementById('knowledgeModalSubmit');
    var modalInstance = modalEl && window.bootstrap ? new window.bootstrap.Modal(modalEl) : null;
    var activeSubmit = null;

    function openModal(title, bodyHtml, onSubmit) {
        if (!modalInstance) return;
        modalTitle.textContent = title;
        modalBody.innerHTML = bodyHtml;
        activeSubmit = onSubmit;
        modalInstance.show();
    }

    function closeModal() {
        if (modalInstance) modalInstance.hide();
    }

    if (modalSubmit) {
        modalSubmit.addEventListener('click', function () {
            if (typeof activeSubmit === 'function') {
                activeSubmit();
            }
        });
    }

    function textField(id, label, value, required, maxLength) {
        var req = required ? ' required' : '';
        var max = maxLength ? ' maxlength="' + maxLength + '"' : '';
        return '<div class="mb-3">' +
            '<label for="' + id + '" class="form-label' + (required ? ' required' : '') + '">' + label + '</label>' +
            '<input type="text" class="form-control" id="' + id + '" value="' + escapeHtml(value || '') + '"' + req + max + ' />' +
            '</div>';
    }

    function textareaField(id, label, value, maxLength) {
        var max = maxLength ? ' maxlength="' + maxLength + '"' : '';
        return '<div class="mb-3">' +
            '<label for="' + id + '" class="form-label">' + label + '</label>' +
            '<textarea class="form-control" id="' + id + '" rows="3"' + max + '>' + escapeHtml(value || '') + '</textarea>' +
            '</div>';
    }

    function urlField(id, label, value) {
        return '<div class="mb-3">' +
            '<label for="' + id + '" class="form-label required">' + label + '</label>' +
            '<input type="url" class="form-control" id="' + id + '" value="' + escapeHtml(value || '') + '" required maxlength="2000" />' +
            '</div>';
    }

    function resourceTypeField(id, selectedValue) {
        var options = [
            { v: 0, l: 'Video' },
            { v: 1, l: 'Enlace' },
            { v: 2, l: 'Archivo' }
        ];
        var html = '<div class="mb-3"><label for="' + id + '" class="form-label required">Tipo de recurso</label>' +
            '<select class="form-select" id="' + id + '">';
        for (var i = 0; i < options.length; i++) {
            var sel = parseInt(selectedValue, 10) === options[i].v ? ' selected' : '';
            html += '<option value="' + options[i].v + '"' + sel + '>' + options[i].l + '</option>';
        }
        html += '</select></div>';
        return html;
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function val(id) {
        var el = document.getElementById(id);
        return el ? el.value.trim() : '';
    }

    // -------------------------------------------------------------------------
    // Structure root edit + sync mode
    // -------------------------------------------------------------------------

    function openEditStructure(btn) {
        var name = btn.getAttribute('data-name') || '';
        var description = btn.getAttribute('data-description') || '';
        var body = textField('st-name', 'Nombre', name, true, 200) +
                   textareaField('st-description', 'Descripción', description, 2000);
        openModal('Editar estructura', body, function () {
            var payload = {
                externalId: ctx.structureExternalId,
                name: val('st-name'),
                description: val('st-description') || null
            };
            if (!payload.name) { toast('El nombre es obligatorio.', 'warning'); return; }
            fetchJson(ctx.urls.updateStructure, 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Estructura actualizada.'); })
                .catch(networkError);
        });
    }

    function saveSyncMode() {
        var selected = document.querySelector('input[name="syncMode"]:checked');
        if (!selected) { toast('Seleccione un modo de sincronización.', 'warning'); return; }
        var payload = { syncMode: parseInt(selected.value, 10) };
        fetchJson(ctx.urls.setSyncMode, 'POST', payload)
            .then(function (d) { handleResponse(d, 'Modo de sincronización actualizado.'); })
            .catch(networkError);
    }

    function syncFromTemplate() {
        if (!confirm('¿Sincronizar los cambios desde la plantilla? Los items locales no se verán afectados.')) {
            return;
        }
        fetchJson(ctx.urls.syncFromTemplate, 'POST', {})
            .then(function (resp) {
                if (resp && resp.success) {
                    var data = resp.data || {};
                    toast(
                        'Sincronización completada: ' +
                        (data.modulesAdded || 0) + ' módulo(s), ' +
                        (data.topicsAdded || 0) + ' tema(s), ' +
                        (data.subjectsAdded || 0) + ' materia(s), ' +
                        (data.resourcesAdded || 0) + ' recurso(s) agregados.',
                        'success');
                    window.setTimeout(function () { window.location.reload(); }, 1200);
                } else {
                    toast((resp && resp.message) || 'Error al sincronizar.', 'danger');
                }
            })
            .catch(networkError);
    }

    // -------------------------------------------------------------------------
    // Module add/edit/delete
    // -------------------------------------------------------------------------

    function openAddModule() {
        var body = textField('mod-name', 'Nombre', '', true, 200) +
                   textareaField('mod-description', 'Descripción', '', 2000);
        openModal('Agregar módulo', body, function () {
            var payload = {
                name: val('mod-name'),
                description: val('mod-description') || null,
                sortOrder: ctx.moduleCount
            };
            if (!payload.name) { toast('El nombre es obligatorio.', 'warning'); return; }
            fetchJson(ctx.urls.addModule, 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Módulo agregado.'); })
                .catch(networkError);
        });
    }

    function openEditModule(btn) {
        var moduleId = btn.getAttribute('data-module-id');
        var body = textField('mod-name', 'Nombre', btn.getAttribute('data-name'), true, 200) +
                   textareaField('mod-description', 'Descripción', btn.getAttribute('data-description'), 2000);
        openModal('Editar módulo', body, function () {
            var payload = {
                name: val('mod-name'),
                description: val('mod-description') || null
            };
            if (!payload.name) { toast('El nombre es obligatorio.', 'warning'); return; }
            fetchJson(replaceGuid(ctx.urls.updateModule, moduleId), 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Módulo actualizado.'); })
                .catch(networkError);
        });
    }

    function deleteModule(btn) {
        var moduleId = btn.getAttribute('data-module-id');
        var name = btn.getAttribute('data-name') || '';
        if (!confirm('¿Eliminar el módulo "' + name + '" y todo su contenido?')) return;
        fetchJson(replaceGuid(ctx.urls.deleteModule, moduleId), 'POST')
            .then(function (d) { handleResponse(d, 'Módulo eliminado.'); })
            .catch(networkError);
    }

    // -------------------------------------------------------------------------
    // Topic add/edit/delete + priority ranges
    // -------------------------------------------------------------------------

    function openAddTopic(btn) {
        var moduleId = btn.getAttribute('data-module-id');
        var moduleItem = btn.closest('.module-item');
        var existingCount = moduleItem ? moduleItem.querySelectorAll('.topic-item').length : 0;
        var body = textField('tp-name', 'Nombre', '', true, 200) +
                   textareaField('tp-description', 'Descripción', '', 2000);
        openModal('Agregar tema', body, function () {
            var payload = {
                moduleExternalId: moduleId,
                name: val('tp-name'),
                description: val('tp-description') || null,
                sortOrder: existingCount
            };
            if (!payload.name) { toast('El nombre es obligatorio.', 'warning'); return; }
            fetchJson(ctx.urls.addTopic, 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Tema agregado.'); })
                .catch(networkError);
        });
    }

    function openEditTopic(btn) {
        var topicId = btn.getAttribute('data-topic-id');
        var body = textField('tp-name', 'Nombre', btn.getAttribute('data-name'), true, 200) +
                   textareaField('tp-description', 'Descripción', btn.getAttribute('data-description'), 2000);
        openModal('Editar tema', body, function () {
            var payload = {
                name: val('tp-name'),
                description: val('tp-description') || null
            };
            if (!payload.name) { toast('El nombre es obligatorio.', 'warning'); return; }
            fetchJson(replaceGuid(ctx.urls.updateTopic, topicId), 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Tema actualizado.'); })
                .catch(networkError);
        });
    }

    function deleteTopic(btn) {
        var topicId = btn.getAttribute('data-topic-id');
        var name = btn.getAttribute('data-name') || '';
        if (!confirm('¿Eliminar el tema "' + name + '" y todo su contenido?')) return;
        fetchJson(replaceGuid(ctx.urls.deleteTopic, topicId), 'POST')
            .then(function (d) { handleResponse(d, 'Tema eliminado.'); })
            .catch(networkError);
    }

    function updateTopicRanges(btn) {
        var topicId = btn.getAttribute('data-topic-id');
        var editor = btn.closest('.priority-range-editor');
        if (!editor) return;

        var bands = ['high', 'medium', 'low'];
        var ranges = {};
        for (var i = 0; i < bands.length; i++) {
            var band = bands[i];
            var row = editor.querySelector('[data-band="' + band + '"]');
            if (!row) continue;
            var minRaw = row.querySelector('[data-field="min"]').value;
            var maxRaw = row.querySelector('[data-field="max"]').value;
            if (minRaw === '' && maxRaw === '') {
                ranges[band] = null;
                continue;
            }
            if (minRaw === '' || maxRaw === '') {
                toast('Complete ambos extremos (mínimo y máximo) o deje vacío el rango "' + bandLabel(band) + '".', 'warning');
                return;
            }
            var min = parseFloat(minRaw);
            var max = parseFloat(maxRaw);
            if (isNaN(min) || isNaN(max)) {
                toast('Los valores del rango "' + bandLabel(band) + '" no son números válidos.', 'warning');
                return;
            }
            if (min > max) {
                toast('En el rango "' + bandLabel(band) + '" el mínimo no puede ser mayor al máximo.', 'warning');
                return;
            }
            ranges[band] = { min: min, max: max };
        }

        if (rangesOverlap(ranges.high, ranges.medium) ||
            rangesOverlap(ranges.high, ranges.low) ||
            rangesOverlap(ranges.medium, ranges.low)) {
            toast('Los rangos de prioridad se solapan.', 'danger');
            return;
        }

        var payload = {
            high: toRangeVm(ranges.high),
            medium: toRangeVm(ranges.medium),
            low: toRangeVm(ranges.low)
        };

        fetchJson(replaceGuid(ctx.urls.updateTopicRanges, topicId), 'POST', payload)
            .then(function (d) { handleResponse(d, 'Rangos actualizados.'); })
            .catch(networkError);
    }

    function bandLabel(band) {
        if (band === 'high') return 'Alta';
        if (band === 'medium') return 'Media';
        return 'Baja';
    }

    function toRangeVm(range) {
        if (!range) return null;
        return { min: range.min, max: range.max };
    }

    // Overlap check (touching endpoints count as overlap, per domain rules).
    function rangesOverlap(a, b) {
        if (!a || !b) return false;
        return a.min <= b.max && b.min <= a.max;
    }

    // -------------------------------------------------------------------------
    // Subject add/edit/delete
    // -------------------------------------------------------------------------

    function openAddSubject(btn) {
        var topicId = btn.getAttribute('data-topic-id');
        var topicItem = btn.closest('.topic-item');
        var existingCount = topicItem ? topicItem.querySelectorAll('.subject-item').length : 0;
        var body = textField('sj-name', 'Nombre', '', true, 200) +
                   textareaField('sj-description', 'Descripción', '', 2000);
        openModal('Agregar asignatura', body, function () {
            var payload = {
                topicExternalId: topicId,
                name: val('sj-name'),
                description: val('sj-description') || null,
                sortOrder: existingCount
            };
            if (!payload.name) { toast('El nombre es obligatorio.', 'warning'); return; }
            fetchJson(ctx.urls.addSubject, 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Asignatura agregada.'); })
                .catch(networkError);
        });
    }

    function openEditSubject(btn) {
        var subjectId = btn.getAttribute('data-subject-id');
        var body = textField('sj-name', 'Nombre', btn.getAttribute('data-name'), true, 200) +
                   textareaField('sj-description', 'Descripción', btn.getAttribute('data-description'), 2000);
        openModal('Editar asignatura', body, function () {
            var payload = {
                name: val('sj-name'),
                description: val('sj-description') || null
            };
            if (!payload.name) { toast('El nombre es obligatorio.', 'warning'); return; }
            fetchJson(replaceGuid(ctx.urls.updateSubject, subjectId), 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Asignatura actualizada.'); })
                .catch(networkError);
        });
    }

    function deleteSubject(btn) {
        var subjectId = btn.getAttribute('data-subject-id');
        var name = btn.getAttribute('data-name') || '';
        if (!confirm('¿Eliminar la asignatura "' + name + '" y todos sus recursos?')) return;
        fetchJson(replaceGuid(ctx.urls.deleteSubject, subjectId), 'POST')
            .then(function (d) { handleResponse(d, 'Asignatura eliminada.'); })
            .catch(networkError);
    }

    // -------------------------------------------------------------------------
    // Resource add/edit/delete
    // -------------------------------------------------------------------------

    function openAddResource(btn) {
        var subjectId = btn.getAttribute('data-subject-id');
        var body = textField('rs-title', 'Título', '', true, 200) +
                   textareaField('rs-description', 'Descripción', '', 2000) +
                   urlField('rs-url', 'URL', '') +
                   resourceTypeField('rs-type', 1);
        openModal('Agregar recurso', body, function () {
            var payload = {
                subjectExternalId: subjectId,
                title: val('rs-title'),
                description: val('rs-description') || null,
                url: val('rs-url'),
                resourceType: parseInt(val('rs-type'), 10),
                sortOrder: 0
            };
            if (!payload.title) { toast('El título es obligatorio.', 'warning'); return; }
            if (!payload.url) { toast('La URL es obligatoria.', 'warning'); return; }
            fetchJson(ctx.urls.addResource, 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Recurso agregado.'); })
                .catch(networkError);
        });
    }

    function openEditResource(btn) {
        var resourceId = btn.getAttribute('data-resource-id');
        var body = textField('rs-title', 'Título', btn.getAttribute('data-title'), true, 200) +
                   textareaField('rs-description', 'Descripción', btn.getAttribute('data-description'), 2000) +
                   urlField('rs-url', 'URL', btn.getAttribute('data-url')) +
                   resourceTypeField('rs-type', btn.getAttribute('data-type'));
        openModal('Editar recurso', body, function () {
            var payload = {
                title: val('rs-title'),
                description: val('rs-description') || null,
                url: val('rs-url'),
                resourceType: parseInt(val('rs-type'), 10)
            };
            if (!payload.title) { toast('El título es obligatorio.', 'warning'); return; }
            if (!payload.url) { toast('La URL es obligatoria.', 'warning'); return; }
            fetchJson(replaceGuid(ctx.urls.updateResource, resourceId), 'POST', payload)
                .then(function (d) { closeModal(); handleResponse(d, 'Recurso actualizado.'); })
                .catch(networkError);
        });
    }

    function deleteResource(btn) {
        var resourceId = btn.getAttribute('data-resource-id');
        var title = btn.getAttribute('data-title') || '';
        if (!confirm('¿Eliminar el recurso "' + title + '"?')) return;
        fetchJson(replaceGuid(ctx.urls.deleteResource, resourceId), 'POST')
            .then(function (d) { handleResponse(d, 'Recurso eliminado.'); })
            .catch(networkError);
    }

    // -------------------------------------------------------------------------
    // Event dispatcher
    // -------------------------------------------------------------------------

    var actions = {
        'edit-structure': openEditStructure,
        'save-sync-mode': saveSyncMode,
        'sync-from-template': syncFromTemplate,
        'add-module': openAddModule,
        'edit-module': openEditModule,
        'delete-module': deleteModule,
        'add-topic': openAddTopic,
        'edit-topic': openEditTopic,
        'delete-topic': deleteTopic,
        'update-topic-ranges': updateTopicRanges,
        'add-subject': openAddSubject,
        'edit-subject': openEditSubject,
        'delete-subject': deleteSubject,
        'add-resource': openAddResource,
        'edit-resource': openEditResource,
        'delete-resource': deleteResource
    };

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-action]');
        if (!btn) return;
        var action = btn.getAttribute('data-action');
        var handler = actions[action];
        if (typeof handler === 'function') {
            e.preventDefault();
            handler(btn);
        }
    });
})();
