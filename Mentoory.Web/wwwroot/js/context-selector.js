// Mentoory - Cascading context selector logic for dropdowns

/**
 * Initialize the cascading context selector within the given container.
 * Guards against duplicate initialization to prevent listener accumulation.
 * @param {HTMLElement} container - The container element with data-mode attribute
 */
function initContextSelector(container) {
    if (container.dataset.csInitialized) {
        fetchRoles(container);
        return;
    }
    container.dataset.csInitialized = 'true';

    var roleSelect = container.querySelector('[data-cs="role"]');
    var incubatorSelect = container.querySelector('[data-cs="incubator"]');
    var projectSelect = container.querySelector('[data-cs="project"]');
    var confirmBtn = container.querySelector('[data-cs="confirm"]');

    if (!roleSelect || !incubatorSelect || !projectSelect || !confirmBtn) {
        return;
    }

    roleSelect.addEventListener('change', function () {
        onRoleChange(container);
    });

    incubatorSelect.addEventListener('change', function () {
        onIncubatorChange(container);
    });

    projectSelect.addEventListener('change', function () {
        onProjectChange(container);
    });

    fetchRoles(container);
}

function getSelectors(container) {
    return {
        role: container.querySelector('[data-cs="role"]'),
        incubator: container.querySelector('[data-cs="incubator"]'),
        project: container.querySelector('[data-cs="project"]'),
        confirm: container.querySelector('[data-cs="confirm"]')
    };
}

function fetchRoles(container) {
    var s = getSelectors(container);
    resetSelect(s.incubator, 'Seleccione una incubadora...');
    resetSelect(s.project, 'Seleccione un proyecto...');
    disableConfirm(s.confirm);
    clearHiddenFields(container);

    setLoading(s.role, true, container);
    hideError(container, 'role');

    fetch('/api/context/roles', {
        headers: { 'RequestVerificationToken': getAntiForgeryToken() }
    })
    .then(function (response) {
        if (!response.ok) throw new Error('Error fetching roles');
        return response.json();
    })
    .then(function (roles) {
        setLoading(s.role, false, container);
        populateSelect(s.role, roles, 'role', 'displayName', null, 'Seleccione un rol...');

        if (roles.length === 1) {
            autoSelectSingle(s.role);
            onRoleChange(container);
        }
    })
    .catch(function () {
        setLoading(s.role, false, container);
        showError(container, 'role', function () {
            fetchRoles(container);
        });
    });
}

function onRoleChange(container) {
    var s = getSelectors(container);
    var role = s.role.value;
    resetSelect(s.project, 'Seleccione un proyecto...');
    disableConfirm(s.confirm);
    clearHiddenFields(container);

    if (!role) {
        resetSelect(s.incubator, 'Seleccione una incubadora...');
        return;
    }

    fetchIncubators(container, role);
}

function fetchIncubators(container, role) {
    var s = getSelectors(container);
    setLoading(s.incubator, true, container);
    hideError(container, 'incubator');

    fetch('/api/context/incubators?role=' + encodeURIComponent(role), {
        headers: { 'RequestVerificationToken': getAntiForgeryToken() }
    })
    .then(function (response) {
        if (!response.ok) throw new Error('Error fetching incubators');
        return response.json();
    })
    .then(function (incubators) {
        setLoading(s.incubator, false, container);
        populateSelect(s.incubator, incubators, 'id', 'name', 'roleAssignmentExternalId', 'Seleccione una incubadora...');

        if (incubators.length === 1) {
            autoSelectSingle(s.incubator);
            onIncubatorChange(container);
        }
    })
    .catch(function () {
        setLoading(s.incubator, false, container);
        showError(container, 'incubator', function () {
            fetchIncubators(container, role);
        });
    });
}

function onIncubatorChange(container) {
    var s = getSelectors(container);
    var role = s.role.value;
    var incubatorId = s.incubator.value;

    if (!incubatorId) {
        resetSelect(s.project, 'Seleccione un proyecto...');
        disableConfirm(s.confirm);
        clearHiddenFields(container);
        return;
    }

    fetchProjects(container, role, incubatorId);
}

function fetchProjects(container, role, incubatorId) {
    var s = getSelectors(container);
    setLoading(s.project, true, container);
    hideError(container, 'project');

    fetch('/api/context/projects?role=' + encodeURIComponent(role) + '&incubatorId=' + encodeURIComponent(incubatorId), {
        headers: { 'RequestVerificationToken': getAntiForgeryToken() }
    })
    .then(function (response) {
        if (!response.ok) throw new Error('Error fetching projects');
        return response.json();
    })
    .then(function (projects) {
        setLoading(s.project, false, container);

        if (projects.length === 0) {
            populateEmptyProjects(s.project);
        } else {
            populateSelect(s.project, projects, 'id', 'name', 'roleAssignmentExternalId', 'Seleccione un proyecto...');

            if (projects.length === 1) {
                autoSelectSingle(s.project);
            }
        }

        updateHiddenFields(container);
        updateConfirmState(container);
    })
    .catch(function () {
        setLoading(s.project, false, container);
        showError(container, 'project', function () {
            fetchProjects(container, role, incubatorId);
        });
    });
}

function onProjectChange(container) {
    var s = getSelectors(container);
    var selectedOption = s.project.options[s.project.selectedIndex];
    var hiddenExternalId = container.querySelector('[name="roleAssignmentExternalId"]');
    var hiddenProjectId = container.querySelector('[name="selectedProjectId"]');
    var hiddenProjectName = container.querySelector('[name="selectedProjectName"]');

    if (selectedOption && selectedOption.value && selectedOption.dataset.externalId) {
        hiddenExternalId.value = selectedOption.dataset.externalId;
        hiddenProjectId.value = selectedOption.value;
        hiddenProjectName.value = selectedOption.textContent;
    } else {
        hiddenProjectId.value = '';
        hiddenProjectName.value = '';

        var incubatorOption = s.incubator.options[s.incubator.selectedIndex];
        if (incubatorOption && incubatorOption.dataset.externalId) {
            hiddenExternalId.value = incubatorOption.dataset.externalId;
        }
    }

    updateConfirmState(container);
}

// --- Utility functions ---

function populateSelect(select, items, valueKey, textKey, externalIdKey, placeholder) {
    select.innerHTML = '';
    var defaultOpt = document.createElement('option');
    defaultOpt.value = '';
    defaultOpt.textContent = placeholder;
    select.appendChild(defaultOpt);

    items.forEach(function (item) {
        var opt = document.createElement('option');
        opt.value = item[valueKey];
        opt.textContent = item[textKey];
        if (externalIdKey && item[externalIdKey]) {
            opt.dataset.externalId = item[externalIdKey];
        }
        select.appendChild(opt);
    });

    select.disabled = false;
}

function populateEmptyProjects(select) {
    select.innerHTML = '';
    var opt = document.createElement('option');
    opt.value = '';
    opt.textContent = 'Sin proyectos disponibles';
    select.appendChild(opt);
    select.disabled = true;
}

function resetSelect(select, placeholder) {
    select.innerHTML = '';
    var opt = document.createElement('option');
    opt.value = '';
    opt.textContent = placeholder;
    select.appendChild(opt);
    select.disabled = true;
}

function autoSelectSingle(select) {
    if (select.options.length === 2) {
        select.selectedIndex = 1;
        select.disabled = true;
    }
}

function disableConfirm(btn) {
    btn.disabled = true;
}

function updateConfirmState(container) {
    var s = getSelectors(container);
    s.confirm.disabled = !s.incubator.value;
}

function updateHiddenFields(container) {
    var s = getSelectors(container);
    var hiddenExternalId = container.querySelector('[name="roleAssignmentExternalId"]');
    var hiddenIncubatorId = container.querySelector('[name="selectedIncubatorId"]');
    var hiddenIncubatorName = container.querySelector('[name="selectedIncubatorName"]');
    var hiddenProjectId = container.querySelector('[name="selectedProjectId"]');
    var hiddenProjectName = container.querySelector('[name="selectedProjectName"]');

    var incubatorOption = s.incubator.options[s.incubator.selectedIndex];
    if (incubatorOption && incubatorOption.value) {
        hiddenIncubatorId.value = incubatorOption.value;
        hiddenIncubatorName.value = incubatorOption.textContent;
        hiddenExternalId.value = incubatorOption.dataset.externalId || '';
    }

    var projectOption = s.project.options[s.project.selectedIndex];
    if (projectOption && projectOption.value) {
        hiddenProjectId.value = projectOption.value;
        hiddenProjectName.value = projectOption.textContent;
        hiddenExternalId.value = projectOption.dataset.externalId || '';
    } else {
        hiddenProjectId.value = '';
        hiddenProjectName.value = '';
    }
}

function clearHiddenFields(container) {
    var fields = ['roleAssignmentExternalId', 'selectedIncubatorId', 'selectedIncubatorName', 'selectedProjectId', 'selectedProjectName'];
    fields.forEach(function (name) {
        var input = container.querySelector('[name="' + name + '"]');
        if (input) input.value = '';
    });
}

function setLoading(select, isLoading, container) {
    var spinnerId = select.dataset.cs;
    var spinner = container.querySelector('[data-spinner-for="' + spinnerId + '"]');
    if (spinner) {
        spinner.classList.toggle('d-none', !isLoading);
    }
    if (isLoading) {
        select.disabled = true;
    }
}

function showError(container, field, retryFn) {
    var errorEl = container.querySelector('[data-error-for="' + field + '"]');
    if (errorEl) {
        errorEl.innerHTML = 'Error al cargar opciones. <a href="#" class="text-danger">Intente nuevamente.</a>';
        errorEl.classList.remove('d-none');
        var retryLink = errorEl.querySelector('a');
        if (retryLink) {
            retryLink.addEventListener('click', function (e) {
                e.preventDefault();
                retryFn();
            });
        }
    }
}

function hideError(container, field) {
    var errorEl = container.querySelector('[data-error-for="' + field + '"]');
    if (errorEl) {
        errorEl.classList.add('d-none');
        errorEl.innerHTML = '';
    }
}

// Initialize page-mode selectors on DOMContentLoaded
document.addEventListener('DOMContentLoaded', function () {
    var pageContainers = document.querySelectorAll('[data-mode="page"]');
    pageContainers.forEach(function (container) {
        initContextSelector(container);
    });
});
