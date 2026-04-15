/**
 * Pipeline Editor - Drag-and-drop stage reordering
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var list = document.getElementById('pipelineList');
        if (!list) return;

        var items = list.querySelectorAll('.list-group-item');
        if (items.length < 3) return; // Need at least Reg + middle + Closure

        var dragItem = null;

        items.forEach(function (item, index) {
            // Only allow dragging middle stages (not first/last)
            if (index === 0 || index === items.length - 1) return;

            item.setAttribute('draggable', 'true');

            item.addEventListener('dragstart', function (e) {
                dragItem = this;
                this.classList.add('opacity-50');
                e.dataTransfer.effectAllowed = 'move';
            });

            item.addEventListener('dragend', function () {
                this.classList.remove('opacity-50');
                dragItem = null;
                list.querySelectorAll('.list-group-item').forEach(function (el) {
                    el.classList.remove('border-primary');
                });
            });
        });

        list.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
            var target = e.target.closest('.list-group-item');
            if (target && target !== dragItem) {
                list.querySelectorAll('.list-group-item').forEach(function (el) {
                    el.classList.remove('border-primary');
                });
                target.classList.add('border-primary');
            }
        });

        list.addEventListener('drop', function (e) {
            e.preventDefault();
            var target = e.target.closest('.list-group-item');
            if (!target || !dragItem || target === dragItem) return;

            var allItems = Array.from(list.querySelectorAll('.list-group-item'));
            var targetIndex = allItems.indexOf(target);

            // Don't allow dropping at first or last position
            if (targetIndex === 0 || targetIndex === allItems.length - 1) return;

            // Reorder DOM
            if (allItems.indexOf(dragItem) < targetIndex) {
                target.parentNode.insertBefore(dragItem, target.nextSibling);
            } else {
                target.parentNode.insertBefore(dragItem, target);
            }

            target.classList.remove('border-primary');
        });
    });
})();
