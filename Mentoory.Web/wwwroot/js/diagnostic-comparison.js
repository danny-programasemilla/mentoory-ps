/**
 * Diagnostic Comparison - selection management for timeline comparison
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var checkboxes = document.querySelectorAll('.compare-checkbox');
        var compareBtn = document.getElementById('compareBtn');
        var compareForm = document.getElementById('compareForm');

        if (!compareBtn || !compareForm || checkboxes.length < 2) return;

        checkboxes.forEach(function (cb) {
            cb.addEventListener('change', function () {
                var selected = document.querySelectorAll('.compare-checkbox:checked');

                if (selected.length > 2) {
                    this.checked = false;
                    return;
                }

                compareBtn.disabled = selected.length !== 2;

                if (selected.length === 2) {
                    compareForm.action = compareForm.action.split('?')[0]
                        + '?responseId1=' + selected[0].value
                        + '&responseId2=' + selected[1].value;
                }
            });
        });
    });
})();
