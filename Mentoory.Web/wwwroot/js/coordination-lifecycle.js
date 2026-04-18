// Lifecycle page enhancements: initialize Bootstrap tooltips on locked action cards.
document.addEventListener('DOMContentLoaded', function () {
    var tooltipTriggerList = Array.prototype.slice.call(
        document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (trigger) {
        if (window.bootstrap && window.bootstrap.Tooltip) {
            new window.bootstrap.Tooltip(trigger);
        }
    });
});
