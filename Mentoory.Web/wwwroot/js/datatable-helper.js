// Mentoory - Reusable DataTable initialization

/**
 * Initialize a DataTable with server-side processing
 * @param {string} tableId - The table element ID
 * @param {object} config - Configuration object
 * @param {string} config.apiUrl - Server-side data URL
 * @param {Array} config.columns - Column definitions
 * @param {Array} [config.defaultOrder] - Default sort order
 * @param {string} [config.filterId] - Filter form element ID
 * @returns {DataTable} The initialized DataTable instance
 */
function initDataTable(tableId, config) {
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
            processing: 'Procesando...',
            lengthMenu: 'Mostrar _MENU_ registros',
            zeroRecords: 'No se encontraron resultados',
            info: 'Mostrando _START_ a _END_ de _TOTAL_ registros',
            infoEmpty: 'Mostrando 0 a 0 de 0 registros',
            infoFiltered: '(filtrado de _MAX_ registros totales)',
            search: 'Buscar:',
            paginate: {
                first: 'Primero',
                last: 'Último',
                next: 'Siguiente',
                previous: 'Anterior'
            }
        },
        responsive: true,
        dom: '<"row"<"col-sm-12"tr>><"row"<"col-sm-5"i><"col-sm-7"p>>'
    });
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
