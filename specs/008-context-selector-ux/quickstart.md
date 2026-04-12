# Quickstart: Cascading Context Selector UX

**Feature**: 008-context-selector-ux  
**Date**: 2026-04-11

## Prerequisites

- .NET 10.0 SDK
- SQL Server with Mentoory database deployed
- At least one user with multiple role assignments (or GlobalAdmin role)

## Build & Run

```bash
# Build all projects
dotnet build

# Run with Aspire (recommended — includes all services)
dotnet run --project Mentoory.Aspire.AppHost

# Or run web only
dotnet run --project Mentoory.Web
```

## Test the Feature

### 1. Test Cascade API Endpoints

```bash
# Authenticate first (get a session cookie), then:

# List roles for current user
curl -b cookies.txt https://localhost:5001/api/context/roles

# List incubators for a specific role
curl -b cookies.txt "https://localhost:5001/api/context/incubators?role=Mentor"

# List projects for a role + incubator
curl -b cookies.txt "https://localhost:5001/api/context/projects?role=Mentor&incubatorId=1"
```

### 2. Test Full-Page Selector

1. Log in with a user that has 2+ role assignments
2. Navigate to `/Context/Select`
3. Verify: 3 dropdowns appear (Rol, Incubadora, Proyecto)
4. Select a role → incubator dropdown populates
5. Select an incubator → project dropdown populates (or shows "Sin proyectos disponibles")
6. Click "Confirmar" → redirected to home or returnUrl

### 3. Test Auto-Skip

1. Log in with a user that has exactly ONE role assignment
2. Verify: selector page is never shown, context is auto-set

### 4. Test Top-Bar Modal

1. While logged in, click "Cambiar contexto" in the top-bar
2. Modal opens with 3 cascading dropdowns
3. Select role, incubator, optionally project
4. Click "Confirmar" → toast shows "Contexto actualizado exitosamente."
5. Page reloads with new context

### 5. Test GlobalAdmin

1. Log in as GlobalAdmin
2. Select "Administrador Global" role
3. Verify: ALL incubators appear in the dropdown
4. Select an incubator → ALL projects under it appear
5. Proceed without selecting a project → context set at incubator level

## Run Tests

```bash
# All tests
dotnet test

# Integration tests only
dotnet test tests/Mentoory.Tests.Integration

# Specific test class
dotnet test --filter "FullyQualifiedName~ContextSelectionTests"
```

## Key Files

| File | Purpose |
|------|---------|
| `Mentoory.Web/Controllers/ContextController.cs` | All context endpoints (GET + POST) |
| `Mentoory.Web/Views/Shared/_ContextSelector.cshtml` | Shared dropdown partial |
| `Mentoory.Web/Views/Context/Select.cshtml` | Full-page selector |
| `Mentoory.Web/Views/Shared/_TopBar.cshtml` | Top-bar with modal |
| `Mentoory.Web/wwwroot/js/context-selector.js` | Cascade dropdown JS logic |
| `Mentoory.Web/wwwroot/js/context-switcher.js` | AJAX context switch + modal integration |
| `Mentoory.Access.Application/Queries/ListContext*/` | Cascade query handlers |
