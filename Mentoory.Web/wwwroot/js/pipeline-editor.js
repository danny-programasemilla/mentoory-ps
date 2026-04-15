/**
 * Pipeline Editor - Drag-and-drop stage reordering with save
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var list = document.getElementById('pipelineList');
        if (!list) return;

        var rows = list.querySelectorAll('.pipeline-row');
        if (rows.length < 3) return; // Need at least Reg + middle + Closure

        var reorderUrl = list.getAttribute('data-reorder-url');
        var btnSave = document.getElementById('btnSaveOrder');
        var originalOrder = getCurrentOrder();
        var dragItem = null;
        var highlightedEl = null;

        rows.forEach(function (row, index) {
            // Only allow dragging middle stages (not first/last)
            if (index === 0 || index === rows.length - 1) return;

            row.setAttribute('draggable', 'true');

            row.addEventListener('dragstart', function (e) {
                dragItem = this;
                this.classList.add('opacity-50');
                e.dataTransfer.effectAllowed = 'move';
            });

            row.addEventListener('dragend', function () {
                this.classList.remove('opacity-50');
                dragItem = null;
                clearHighlight();
            });
        });

        list.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            var target = e.target.closest('.pipeline-row');
            if (target && target !== dragItem && target !== highlightedEl) {
                clearHighlight();
                target.classList.add('border-primary');
                highlightedEl = target;
            }
        });

        list.addEventListener('drop', function (e) {
            e.preventDefault();
            var target = e.target.closest('.pipeline-row');
            if (!target || !dragItem || target === dragItem) return;

            var allRows = Array.from(list.querySelectorAll('.pipeline-row'));
            var targetIndex = allRows.indexOf(target);

            // Don't allow dropping at first or last position
            if (targetIndex === 0 || targetIndex === allRows.length - 1) return;

            if (allRows.indexOf(dragItem) < targetIndex) {
                target.parentNode.insertBefore(dragItem, target.nextSibling);
            } else {
                target.parentNode.insertBefore(dragItem, target);
            }

            clearHighlight();
            updateSaveButtonVisibility();
        });

        if (btnSave) {
            btnSave.addEventListener('click', function () {
                saveOrder();
            });
        }

        function getCurrentOrder() {
            return Array.from(list.querySelectorAll('.pipeline-row'))
                .map(function (row) { return row.getAttribute('data-stage-id'); });
        }

        function arraysEqual(a, b) {
            if (a.length !== b.length) return false;
            for (var i = 0; i < a.length; i++) {
                if (a[i] !== b[i]) return false;
            }
            return true;
        }

        function updateSaveButtonVisibility() {
            if (!btnSave) return;
            var current = getCurrentOrder();
            if (arraysEqual(current, originalOrder)) {
                btnSave.classList.add('d-none');
            } else {
                btnSave.classList.remove('d-none');
            }
        }

        function clearHighlight() {
            if (highlightedEl) {
                highlightedEl.classList.remove('border-primary');
                highlightedEl = null;
            }
        }

        function saveOrder() {
            var orderedIds = getCurrentOrder();
            var formData = new FormData();
            orderedIds.forEach(function (id) {
                formData.append('orderedStageIds', id);
            });

            btnSave.disabled = true;

            fetch(reorderUrl, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiForgeryToken(),
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: formData
            })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error('Error al guardar');
                }
                originalOrder = getCurrentOrder();
                btnSave.classList.add('d-none');
                showToast('Orden guardado exitosamente.', 'success');
            })
            .catch(function () {
                alert('Error al guardar el orden. Intente de nuevo.');
            })
            .finally(function () {
                btnSave.disabled = false;
            });
        }
    });
})();
