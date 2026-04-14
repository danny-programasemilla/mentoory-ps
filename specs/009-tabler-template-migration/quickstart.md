# Quickstart: Tabler Admin Template Migration

**Date:** 2026-04-13
**Feature:** 009-tabler-template-migration

## Prerequisites

- .NET 10.0 SDK
- Node.js / npm (for downloading Tabler packages — only needed once for asset extraction)
- Access to the Mentoory codebase on branch `009-tabler-template-migration`

## Setup Steps

### 1. Install Tabler Assets

Download and extract the Tabler packages into `wwwroot/lib/`:

```bash
# Create temporary directory for npm download
mkdir -p /tmp/tabler-install && cd /tmp/tabler-install

# Download packages
npm pack @tabler/core
npm pack @tabler/icons-webfont

# Extract and copy to project
tar -xzf tabler-core-*.tgz
tar -xzf tabler-icons-webfont-*.tgz

# Copy Tabler core
mkdir -p <project-root>/Mentoory.Web/wwwroot/lib/tabler/css
mkdir -p <project-root>/Mentoory.Web/wwwroot/lib/tabler/js
cp package/dist/css/tabler.min.css <project-root>/Mentoory.Web/wwwroot/lib/tabler/css/
cp package/dist/js/tabler.min.js <project-root>/Mentoory.Web/wwwroot/lib/tabler/js/

# Copy Tabler Icons webfont (from second extracted package)
mkdir -p <project-root>/Mentoory.Web/wwwroot/lib/tabler-icons-webfont/fonts
cp package/dist/tabler-icons.min.css <project-root>/Mentoory.Web/wwwroot/lib/tabler-icons-webfont/
cp package/dist/fonts/* <project-root>/Mentoory.Web/wwwroot/lib/tabler-icons-webfont/fonts/

# Cleanup
cd - && rm -rf /tmp/tabler-install
```

### 2. Remove Old Bootstrap

```bash
rm -rf Mentoory.Web/wwwroot/lib/bootstrap/
```

### 3. Build and Run

```bash
dotnet build
dotnet run --project Mentoory.Web
# Or with Aspire:
dotnet run --project Mentoory.Aspire.AppHost
```

### 4. Verify

- Navigate to login page — should show split-panel auth layout
- Log in — should see Tabler sidebar with Tabler Icons
- Navigate between areas — verify sidebar active state and breadcrumbs
- Open context-switcher modal — verify cascade dropdowns work
- Visit a page with a DataTable — verify table renders and paginates

## Key Files

| File | Purpose |
|------|---------|
| `Views/Shared/_Layout.cshtml` | Main layout (Tabler `.page` structure) |
| `Views/Shared/_AuthLayout.cshtml` | Split-panel auth layout (new) |
| `Views/Shared/_Navigation.cshtml` | Sidebar (Tabler `.navbar-vertical`) |
| `Views/Shared/_TopBar.cshtml` | Page header with context display |
| `Views/Shared/_Footer.cshtml` | Footer (Tabler `.footer`) |
| `Views/Shared/_Breadcrumbs.cshtml` | Breadcrumbs in page header |
| `Infrastructure/Menu/MenuConfiguration.cs` | Icon definitions (Tabler Icons webfont) |
| `wwwroot/css/mentoory.css` | Custom overrides + DataTables fixes |
| `wwwroot/lib/tabler/` | Tabler CSS and JS |
| `wwwroot/lib/tabler-icons-webfont/` | Tabler Icons webfont |

## CSS Load Order

```html
<link rel="stylesheet" href="~/lib/tabler/css/tabler.min.css" />
<link rel="stylesheet" href="~/lib/tabler-icons-webfont/tabler-icons.min.css" />
<link rel="stylesheet" href="~/lib/datatables/dataTables.bootstrap5.min.css" />
<link rel="stylesheet" href="~/css/mentoory.css" />
```

## JS Load Order

```html
<script src="~/lib/jquery/dist/jquery.min.js"></script>
<script src="~/lib/tabler/js/tabler.min.js"></script>
<script src="~/lib/datatables/dataTables.min.js"></script>
<script src="~/lib/datatables/dataTables.bootstrap5.min.js"></script>
<script src="~/js/site.js"></script>
<!-- Additional custom scripts as needed -->
```
