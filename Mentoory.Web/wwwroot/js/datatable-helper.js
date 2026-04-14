// Mentoory - Reusable DataTable initialization and render helpers

/**
 * Filter type registry: maps render function signature substrings to filter descriptors.
 * When building the filter panel, each column's render.toString() is checked against these keys.
 */
var FILTER_TYPE_REGISTRY = {
    'renderAccountStatus': {
        type: 'select',
        options: [
            { value: '', label: 'Todos' },
            { value: 'Active', label: 'Activo' },
            { value: 'Locked', label: 'Bloqueado' },
            { value: 'PendingVerification', label: 'Verificación Pendiente' }
        ]
    }
};

/**
 * Column header keywords to exclude from filter panel generation.
 * Matched as lowercase substrings against accent-stripped header text.
 */
var FILTER_EXCLUDE_LIST = ['acciones'];

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
 * Build a filter panel above the table card with auto-detected filter fields.
 * @param {string} tableId - The table element ID
 * @param {Array} columns - DataTable column definitions
 * @param {Array} [overrides] - Per-view filter overrides
 * @returns {string|null} Generated filter form ID, or null if no fields
 */
function buildFilterPanel(tableId, columns, overrides) {
    var formId = tableId + '-filter-form';
    var panelId = tableId + '-filter-panel';
    var toggleId = tableId + '-filter-toggle';

    var headers = document.querySelectorAll('#' + tableId + ' thead th');
    var fields = [];

    for (var i = 0; i < columns.length; i++) {
        var col = columns[i];

        if (col.orderable === false) continue;
        if (!col.data) continue;

        // Check header text against exclude list
        if (headers[i]) {
            var target = headers[i].querySelector('.dt-column-title') || headers[i];
            var headerText = stripAccents(target.textContent.trim().toLowerCase());
            var excluded = false;
            for (var e = 0; e < FILTER_EXCLUDE_LIST.length; e++) {
                if (headerText.indexOf(FILTER_EXCLUDE_LIST[e]) !== -1) {
                    excluded = true;
                    break;
                }
            }
            if (excluded) continue;
        }

        // Check for per-view override
        var override = null;
        if (overrides) {
            for (var o = 0; o < overrides.length; o++) {
                if (overrides[o].column === col.data) {
                    override = overrides[o];
                    break;
                }
            }
        }
        if (override && override.filterable === false) continue;

        // Detect filter type from render function
        var filterType = 'text';
        var filterOptions = null;
        var renderStr = col.render ? col.render.toString() : '';

        // Skip date-formatted columns (not useful for text/select filtering)
        if (renderStr.indexOf('formatDate') !== -1 || renderStr.indexOf('formatRelativeDate') !== -1) continue;

        // Match against filter type registry
        var registryKeys = Object.keys(FILTER_TYPE_REGISTRY);
        for (var r = 0; r < registryKeys.length; r++) {
            if (renderStr.indexOf(registryKeys[r]) !== -1) {
                var entry = FILTER_TYPE_REGISTRY[registryKeys[r]];
                filterType = entry.type;
                filterOptions = entry.options;
                break;
            }
        }

        // Apply override type/options
        if (override) {
            if (override.type) filterType = override.type;
            if (override.options) filterOptions = override.options;
        }

        // Get label from header
        var label = col.data;
        if (headers[i]) {
            var labelTarget = headers[i].querySelector('.dt-column-title') || headers[i];
            label = labelTarget.textContent.trim();
            // Strip any icon text that may have been prepended
            label = label.replace(/^\s+/, '');
        }

        fields.push({
            name: col.data,
            type: filterType,
            options: filterOptions,
            label: label,
            placeholder: (override && override.placeholder) ? override.placeholder : ''
        });
    }

    if (fields.length === 0) return null;

    // Generate toggle link
    var html = '<div class="d-flex justify-content-end mb-2">' +
        '<a href="#" id="' + toggleId + '" class="filter-toggle-link">' +
        '<i class="ti ti-filter"></i> Filtros' +
        '<span class="badge bg-primary ms-1 d-none" id="' + toggleId + '-badge">0</span>' +
        '</a></div>';

    // Generate filter panel
    html += '<div id="' + panelId + '" class="card card-body filter-panel mb-3" style="display:none;">' +
        '<form id="' + formId + '">' +
        '<div class="row g-3">';

    for (var f = 0; f < fields.length; f++) {
        var field = fields[f];
        html += '<div class="col-md-3">';
        html += '<label class="form-label">' + escapeHtml(field.label) + '</label>';

        if (field.type === 'select' && field.options) {
            html += '<select name="' + field.name + '" class="form-select form-select-sm">';
            for (var opt = 0; opt < field.options.length; opt++) {
                html += '<option value="' + escapeHtml(field.options[opt].value) + '">' +
                    escapeHtml(field.options[opt].label) + '</option>';
            }
            html += '</select>';
        } else {
            html += '<input type="text" name="' + field.name + '" class="form-control form-control-sm"' +
                ' placeholder="' + escapeHtml(field.placeholder || field.label) + '">';
        }

        html += '</div>';
    }

    html += '</div>' +
        '<div class="mt-3">' +
        '<button type="submit" class="btn btn-primary btn-sm">' +
        '<i class="ti ti-filter-check me-1"></i>Filtrar</button>' +
        '<button type="button" class="btn btn-ghost-secondary btn-sm ms-2 filter-clear-btn">' +
        '<i class="ti ti-filter-x me-1"></i>Limpiar</button>' +
        '</div></form></div>';

    // Insert before the table's card parent
    var table = document.getElementById(tableId);
    var card = table ? table.closest('.card') : null;
    if (card) {
        card.insertAdjacentHTML('beforebegin', html);
    }

    // Wire toggle link
    var toggleLink = document.getElementById(toggleId);
    var panel = document.getElementById(panelId);
    if (toggleLink && panel) {
        toggleLink.addEventListener('click', function (e) {
            e.preventDefault();
            panel.style.display = panel.style.display === 'none' ? '' : 'none';
        });
    }

    return formId;
}

/**
 * Update the filter badge count on the toggle link.
 * @param {string} tableId - The table element ID
 */
function updateFilterBadge(tableId) {
    var formId = tableId + '-filter-form';
    var badgeId = tableId + '-filter-toggle-badge';
    var form = document.getElementById(formId);
    var badge = document.getElementById(badgeId);
    if (!form || !badge) return;

    var count = 0;
    var inputs = form.querySelectorAll('input, select');
    inputs.forEach(function (input) {
        if (input.name && input.value) count++;
    });

    if (count > 0) {
        badge.textContent = count;
        badge.classList.remove('d-none');
    } else {
        badge.classList.add('d-none');
    }
}

/**
 * Sync active filter values to URL query params with f_ prefix.
 * @param {string} tableId - The table element ID
 */
function syncFiltersToUrl(tableId) {
    var formId = tableId + '-filter-form';
    var form = document.getElementById(formId);
    if (!form) return;

    var params = new URLSearchParams(window.location.search);

    // Remove existing f_ params
    var toRemove = [];
    params.forEach(function (_v, key) {
        if (key.indexOf('f_') === 0) toRemove.push(key);
    });
    for (var i = 0; i < toRemove.length; i++) params.delete(toRemove[i]);

    // Add active filter values
    var inputs = form.querySelectorAll('input, select');
    inputs.forEach(function (input) {
        if (input.name && input.value) {
            params.set('f_' + input.name, input.value);
        }
    });

    var qs = params.toString();
    var url = window.location.pathname + (qs ? '?' + qs : '');
    history.replaceState(null, '', url);
}

/**
 * Remove all f_ filter params from the URL.
 */
function clearFiltersFromUrl() {
    var params = new URLSearchParams(window.location.search);
    var toRemove = [];
    params.forEach(function (_v, key) {
        if (key.indexOf('f_') === 0) toRemove.push(key);
    });
    if (toRemove.length === 0) return;

    for (var i = 0; i < toRemove.length; i++) params.delete(toRemove[i]);

    var qs = params.toString();
    var url = window.location.pathname + (qs ? '?' + qs : '');
    history.replaceState(null, '', url);
}

/**
 * Load filter values from URL query params and apply them.
 * @param {string} tableId - The table element ID
 * @param {object} dtApi - DataTable API instance
 */
function loadFiltersFromUrl(tableId, dtApi) {
    var formId = tableId + '-filter-form';
    var panelId = tableId + '-filter-panel';
    var form = document.getElementById(formId);
    var panel = document.getElementById(panelId);
    if (!form) return;

    var params = new URLSearchParams(window.location.search);
    var hasFilters = false;

    params.forEach(function (value, key) {
        if (key.indexOf('f_') === 0) {
            var fieldName = key.substring(2);
            var field = form.querySelector('[name="' + fieldName + '"]');
            if (field) {
                field.value = value;
                hasFilters = true;
            }
        }
    });

    if (hasFilters) {
        if (panel) panel.style.display = '';
        updateFilterBadge(tableId);
        dtApi.ajax.reload();
    }
}

/**
 * Initialize a DataTable with server-side processing
 * @param {string} tableId - The table element ID
 * @param {object} config - Configuration object
 * @param {string} config.apiUrl - Server-side data URL
 * @param {Array} config.columns - Column definitions
 * @param {Array} [config.defaultOrder] - Default sort order
 * @param {string} [config.filterId] - Filter form element ID
 * @param {Array} [config.filters] - Per-view filter overrides
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

            var dtApi = this.api();
            var generatedFormId = buildFilterPanel(tableId, config.columns, config.filters);
            if (generatedFormId) {
                config.filterId = generatedFormId;

                var form = document.getElementById(generatedFormId);
                if (form) {
                    // "Filtrar" button
                    form.addEventListener('submit', function (e) {
                        e.preventDefault();
                        updateFilterBadge(tableId);
                        syncFiltersToUrl(tableId);
                        dtApi.ajax.reload();
                    });

                    // "Limpiar" button
                    var clearBtn = form.querySelector('.filter-clear-btn');
                    if (clearBtn) {
                        clearBtn.addEventListener('click', function () {
                            form.reset();
                            updateFilterBadge(tableId);
                            clearFiltersFromUrl();
                            dtApi.ajax.reload();
                        });
                    }
                }

                // Restore filters from URL on page load
                loadFiltersFromUrl(tableId, dtApi);
            }
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
