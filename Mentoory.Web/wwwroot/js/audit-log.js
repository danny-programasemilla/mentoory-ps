// Mentoory - Audit log table initialization

var AUDIT_OUTCOME_OPTIONS = [
    { value: '', label: 'Todos' },
    { value: 'Success', label: 'Éxito' },
    { value: 'Failure', label: 'Fallo' }
];

var AUDIT_EVENT_TYPE_OPTIONS = [
    { value: '', label: 'Todos' },
    { value: 'Context.Activated', label: 'Contexto activado' },
    { value: 'Role.Assigned', label: 'Rol asignado' },
    { value: 'User.Registered', label: 'Usuario registrado' },
    { value: 'User.LoggedIn', label: 'Inicio de sesión' },
    { value: 'Answer.Corrected', label: 'Respuesta corregida' }
];

function renderAuditOutcome(outcome) {
    if (outcome === 'Success') return renderStatus('Éxito', 'success');
    if (outcome === 'Failure') return renderStatus('Fallo', 'danger');
    return renderStatus(outcome || '', 'secondary');
}

function formatAuditTimestamp(isoString) {
    if (!isoString) return '';
    var date = new Date(isoString);
    if (isNaN(date.getTime())) return isoString;
    return date.toISOString().replace('T', ' ').slice(0, 19);
}

function buildAuditDetailPanel(row) {
    function line(label, value) {
        if (value === null || value === undefined || value === '') return '';
        return '<div class="col-md-4 col-sm-6 mb-2">' +
            '<div class="text-secondary small">' + escapeHtml(label) + '</div>' +
            '<div class="text-truncate">' + escapeHtml(String(value)) + '</div>' +
            '</div>';
    }

    var prettyDetails = '';
    if (row.details) {
        try {
            prettyDetails = JSON.stringify(JSON.parse(row.details), null, 2);
        } catch (e) {
            prettyDetails = row.details;
        }
    }

    var html = '<div class="p-3 bg-light-subtle">';
    html += '<div class="row g-2">';
    html += line('EntityType', row.entityType);
    html += line('EntityId', row.entityId);
    html += line('CorrelationId', row.correlationId);
    html += line('ExceptionType', row.exceptionType);
    html += line('IpAddress', row.ipAddress);
    html += line('IncubatorId', row.incubatorId);
    html += line('ProjectId', row.projectId);
    html += line('UserId', row.userId);
    html += '</div>';
    if (prettyDetails) {
        html += '<div class="mt-3">';
        html += '<div class="text-secondary small mb-1">Details</div>';
        html += '<pre class="mb-0" style="white-space:pre-wrap;word-break:break-word;max-height:320px;overflow:auto;">'
            + escapeHtml(prettyDetails) + '</pre>';
        html += '</div>';
    }
    html += '</div>';
    return html;
}

function initAuditLogTable(apiUrl) {
    var table = initDataTable('auditLogTable', {
        apiUrl: apiUrl,
        columns: [
            {
                data: 'occurredAtUtc',
                render: function (data) { return formatAuditTimestamp(data); }
            },
            { data: 'eventType' },
            { data: 'userEmail', defaultContent: '' },
            { data: 'action' },
            {
                data: 'outcome',
                render: function (data) { return renderAuditOutcome(data); }
            },
            { data: 'roleContext', defaultContent: '' },
            {
                data: null,
                orderable: false,
                className: 'text-end',
                render: function () {
                    return '<button type="button" class="btn btn-icon btn-ghost-secondary btn-sm audit-expand"' +
                        ' title="Ver detalles"><i class="ti ti-chevron-down"></i></button>';
                }
            }
        ],
        defaultOrder: [[0, 'desc']],
        filters: [
            { column: 'outcome', type: 'select', options: AUDIT_OUTCOME_OPTIONS },
            { column: 'eventType', type: 'select', options: AUDIT_EVENT_TYPE_OPTIONS },
            { column: 'userEmail', type: 'text', placeholder: 'Correo electrónico' },
            { column: 'action', filterable: false },
            { column: 'roleContext', filterable: false }
        ]
    });

    var tableEl = document.getElementById('auditLogTable');
    if (tableEl) {
        tableEl.addEventListener('click', function (e) {
            var btn = e.target.closest('.audit-expand');
            if (!btn) return;
            var tr = btn.closest('tr');
            if (!tr) return;
            var rowApi = table.row(tr);
            if (rowApi.child.isShown()) {
                rowApi.child.hide();
                btn.innerHTML = '<i class="ti ti-chevron-down"></i>';
            } else {
                rowApi.child(buildAuditDetailPanel(rowApi.data())).show();
                btn.innerHTML = '<i class="ti ti-chevron-up"></i>';
            }
        });
    }

    return table;
}
