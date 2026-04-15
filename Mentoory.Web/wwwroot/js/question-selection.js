/**
 * Question Selection - select all / deselect all toggles
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var selectAll = document.getElementById('selectAll');
        var deselectAll = document.getElementById('deselectAll');

        if (selectAll) {
            selectAll.addEventListener('click', function () {
                document.querySelectorAll('.question-checkbox').forEach(function (cb) {
                    cb.checked = true;
                });
            });
        }

        if (deselectAll) {
            deselectAll.addEventListener('click', function () {
                document.querySelectorAll('.question-checkbox').forEach(function (cb) {
                    cb.checked = false;
                });
            });
        }
    });
})();
