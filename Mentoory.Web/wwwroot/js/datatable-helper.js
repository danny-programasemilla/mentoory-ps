// Mentoory - Reusable DataTable initialization and render helpers

/**
 * Keyword-to-icon map for automatic header icon injection.
 * Keys are lowercase substrings matched against column header text.
 */
var COLUMN_ICON_MAP = {
    'correo': 'ti-mail',
    'email': 'ti-mail',
    'nombre': 'ti-user',
    'apellido': 'ti-users',
    'estado': 'ti-circle-check',
    'fecha': 'ti-calendar',
    'descripcion': 'ti-file-text',
    'acciones': 'ti-settings',
    'etapa': 'ti-list-check',
    'proyecto': 'ti-briefcase',
    'preguntas': 'ti-help-circle',
    'version': 'ti-git-branch',
    'suscripcion': 'ti-crown',
    'nivel': 'ti-crown',
    'sincronizacion': 'ti-refresh',
    'modo': 'ti-refresh'
};

function stripAccents(str) {
    return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '');
}

/**
 * Apply header icons to a DataTable based on column header text.
 * Scans each <th> and prepends a matching Tabler icon if found.
 * @param {string} tableId - The table element ID
 */
function applyHeaderIcons(tableId) {
    var headers = document.querySelectorAll('#' + tableId + ' thead th');
    var keywords = Object.keys(COLUMN_ICON_MAP);
    headers.forEach(function (th) {
        if (th.querySelector('.ti')) return;
        // DataTables 2.x wraps header text in .dt-column-title
        var target = th.querySelector('.dt-column-title') || th;
        var text = stripAccents(target.textContent.trim().toLowerCase());
        for (var i = 0; i < keywords.length; i++) {
            if (text.indexOf(keywords[i]) !== -1) {
                var icon = document.createElement('i');
                icon.className = 'ti ' + COLUMN_ICON_MAP[keywords[i]];
                target.prepend(icon);
                break;
            }
        }
    });
}

/**
 * Initialize a DataTable with server-side processing
 * @param {string} tableId - The table element ID
 * @param {object} config - Configuration object
 * @param {string} config.apiUrl - Server-side data URL
 * @param {Array} config.columns - Column definitions
 * @param {Array} [config.defaultOrder] - Default sort order
 * @param {string} [config.filterId] - Filter form element ID
 * @param {object} [config.emptyState] - Empty state configuration
 * @param {string} [config.emptyState.icon] - Tabler icon class
 * @param {string} [config.emptyState.title] - Empty state title
 * @param {string} [config.emptyState.message] - Empty state message
 * @param {string} [config.emptyState.actionUrl] - Primary action URL
 * @param {string} [config.emptyState.actionText] - Primary action text
 * @returns {DataTable} The initialized DataTable instance
 */
function initDataTable(tableId, config) {
    var emptyTableHtml = 'No se encontraron resultados';
    if (config.emptyState) {
        var es = config.emptyState;
        emptyTableHtml = '<div class="empty">';
        if (es.icon) {
            emptyTableHtml += '<div class="empty-icon"><i class="' + es.icon + '" style="font-size:3rem;"></i></div>';
        }
        if (es.title) {
            emptyTableHtml += '<p class="empty-title">' + es.title + '</p>';
        }
        if (es.message) {
            emptyTableHtml += '<p class="empty-subtitle text-secondary">' + es.message + '</p>';
        }
        if (es.actionUrl && es.actionText) {
            emptyTableHtml += '<div class="empty-action"><a href="' + es.actionUrl + '" class="btn btn-primary">' + es.actionText + '</a></div>';
        }
        emptyTableHtml += '</div>';
    }

    var skeletonRows = '';
    var colCount = config.columns ? config.columns.length : 3;
    for (var r = 0; r < 5; r++) {
        skeletonRows += '<tr>';
        for (var c = 0; c < colCount; c++) {
            skeletonRows += '<td><span class="placeholder placeholder-glow col-' + (6 + (c % 4)) + '"></span></td>';
        }
        skeletonRows += '</tr>';
    }
    var processingHtml = '<div class="card-body"><table class="table table-vcenter placeholder-glow"><tbody>' + skeletonRows + '</tbody></table></div>';

    return new DataTable('#' + tableId, {
        processing: true,
        serverSide: true,
        ajax: {
            url: config.apiUrl,
            type: 'POST',
            data: function (d) {
                if (config.filterId) {
                    d.filters = getActiveFilters(config.filterId);
                }
            },
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        },
        columns: config.columns,
        order: config.defaultOrder || [[0, 'asc']],
        language: {
            processing: processingHtml,
            lengthMenu: 'Mostrar _MENU_ registros',
            zeroRecords: emptyTableHtml,
            emptyTable: emptyTableHtml,
            info: 'Mostrando _START_ a _END_ de _TOTAL_ registros',
            infoEmpty: 'Mostrando 0 a 0 de 0 registros',
            infoFiltered: '(filtrado de _MAX_ registros totales)',
            search: 'Buscar:',
            paginate: {
                first: 'Primero',
                last: 'Ultimo',
                next: 'Siguiente',
                previous: 'Anterior'
            }
        },
        responsive: true,
        dom: '<"row"<"col-sm-12"tr>><"row"<"col-sm-5"i><"col-sm-7"p>>',
        initComplete: function () {
            applyHeaderIcons(tableId);
        }
    });
}

/**
 * Format an ISO date string as Spanish relative time
 * @param {string} isoString - ISO 8601 date string
 * @returns {string} HTML with relative time and tooltip with exact date
 */
function formatRelativeDate(isoString) {
    if (!isoString) return '';
    var date = new Date(isoString);
    if (isNaN(date.getTime())) return isoString;

    var now = new Date();
    var diffMs = now - date;
    var diffSec = Math.floor(diffMs / 1000);
    var diffMin = Math.floor(diffSec / 60);
    var diffHrs = Math.floor(diffMin / 60);
    var diffDays = Math.floor(diffHrs / 24);
    var diffWeeks = Math.floor(diffDays / 7);

    var relative;
    if (diffSec < 60) {
        relative = 'hace un momento';
    } else if (diffMin < 60) {
        relative = 'hace ' + diffMin + (diffMin === 1 ? ' minuto' : ' minutos');
    } else if (diffHrs < 24) {
        relative = 'hace ' + diffHrs + (diffHrs === 1 ? ' hora' : ' horas');
    } else if (diffDays < 7) {
        relative = 'hace ' + diffDays + (diffDays === 1 ? ' dia' : ' dias');
    } else if (diffDays < 30) {
        relative = 'hace ' + diffWeeks + (diffWeeks === 1 ? ' semana' : ' semanas');
    } else {
        relative = date.toLocaleDateString('es-ES', { day: '2-digit', month: 'short', year: 'numeric' });
    }

    var exact = date.toLocaleString('es-ES', {
        day: '2-digit', month: 'short', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    });
    return '<span title="' + exact + '">' + relative + '</span>';
}

/**
 * Render an initials-based avatar with compound name+email cell
 * @param {string} firstName - First name
 * @param {string} lastName - Last name
 * @param {string} email - Email address
 * @returns {string} HTML for the compound avatar cell
 */
function renderAvatar(firstName, lastName, email) {
    var first = (firstName || '').trim();
    var last = (lastName || '').trim();
    var fullName = (first + ' ' + last).trim();
    var initials = '';
    if (first && last) {
        initials = (first[0] + last[0]).toUpperCase();
    } else if (first) {
        initials = first[0].toUpperCase();
    } else {
        initials = '<i class="ti ti-user"></i>';
    }

    return '<div class="d-flex align-items-center">' +
        '<span class="avatar avatar-sm me-2">' + initials + '</span>' +
        '<div class="flex-fill text-truncate">' +
        '<div class="text-truncate">' + escapeHtml(fullName || email || '') + '</div>' +
        (email ? '<div class="text-secondary text-truncate small">' + escapeHtml(email) + '</div>' : '') +
        '</div></div>';
}

/**
 * Format an ISO date string as a localized Spanish date
 * @param {string} isoString - ISO 8601 date string
 * @returns {string} Formatted date string
 */
function formatDate(isoString) {
    if (!isoString) return '';
    var date = new Date(isoString);
    if (isNaN(date.getTime())) return isoString;
    return date.toLocaleDateString('es');
}

/**
 * Render account status (Active, Locked, PendingVerification) as a status dot
 * @param {string} status - Account status value from the server
 * @returns {string} HTML for the status indicator
 */
function renderAccountStatus(status) {
    if (status === 'Active') return renderStatus('Active', 'success');
    if (status === 'Locked') return renderStatus('Locked', 'danger');
    if (status === 'PendingVerification') return renderStatus('PendingVerification', 'warning', true);
    return renderStatus(status, 'secondary');
}

/**
 * Render a Tabler status dot indicator
 * @param {string} statusText - Display text
 * @param {string} statusColor - Tabler color name (primary, success, danger, warning, info)
 * @param {boolean} [animated=false] - Whether to animate the status dot
 * @returns {string} HTML for the status indicator
 */
function renderStatus(statusText, statusColor, animated) {
    var dotClass = 'status-dot' + (animated ? ' status-dot-animated' : '');
    return '<span class="status status-' + (statusColor || 'primary') + '">' +
        '<span class="' + dotClass + '"></span> ' + escapeHtml(statusText || '') +
        '</span>';
}

/**
 * Render action buttons for a table row
 * @param {Array} actions - Array of {url, icon, title} objects
 * @returns {string} HTML for the action buttons
 */
function renderActions(actions) {
    if (!actions || actions.length === 0) return '';
    var html = '<div class="btn-list flex-nowrap">';
    for (var i = 0; i < actions.length; i++) {
        var a = actions[i];
        html += '<a href="' + a.url + '" class="btn btn-icon btn-ghost-primary btn-sm" title="' + escapeHtml(a.title || '') + '">' +
            '<i class="' + a.icon + '"></i></a>';
    }
    html += '</div>';
    return html;
}

/**
 * Collect active filters from a filter form
 * @param {string} filterId - The filter form element ID
 * @returns {object} Key-value pairs of active filters
 */
function getActiveFilters(filterId) {
    var filters = {};
    var form = document.getElementById(filterId);
    if (!form) return filters;

    var inputs = form.querySelectorAll('input, select');
    inputs.forEach(function (input) {
        if (input.name && input.value) {
            filters[input.name] = input.value;
        }
    });
    return filters;
}

/**
 * Escape HTML entities to prevent XSS
 * @param {string} str - Input string
 * @returns {string} Escaped string
 */
function escapeHtml(str) {
    if (!str) return '';
    var div = document.createElement('div');
    div.appendChild(document.createTextNode(str));
    return div.innerHTML;
}
