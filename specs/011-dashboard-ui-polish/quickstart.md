# Quickstart: Dashboard Rewrite & UI Polish

## Verify Changes

### 1. Build
```bash
dotnet build
```
Must compile with zero warnings.

### 2. Run E2E Tests
```bash
dotnet test tests/Mentoory.Tests.E2E --filter "DashboardRendering"
```
All dashboard rendering tests must pass.

### 3. Visual QA

Start the app:
```bash
dotnet run --project Mentoory.Web
```

Navigate to each page and verify:

| Page | URL | What to check |
|------|-----|---------------|
| Dashboard | `https://localhost:7061/Administration/Dashboard` | Cards render horizontally, colored strips contained, shadows visible, hover lift works |
| Users | `https://localhost:7061/Administration/Users` | Table layout intact, no regressions |
| Projects | `https://localhost:7061/Administration/Projects` | Layout intact |
| Diagnostics | `https://localhost:7061/Coordination/Diagnostics` | Layout intact |
| Batch Upload | `https://localhost:7061/Administration/BatchUpload` | Layout intact |
| Login | `https://localhost:7061/Access/Login` | Gradient panel and decorations intact |

### Login Credentials (test environment)
- **IncubatorAdmin**: `incadmin1@test.mentoory.com` / `Test123!@#`
- **GlobalAdmin**: `admin@mentoory.com` / `123abc987`

## Files Changed

| File | Change |
|------|--------|
| `Mentoory.Web/Areas/Administration/Views/Dashboard/Index.cshtml` | Rewrite — correct Tabler card patterns |
| `Mentoory.Web/wwwroot/css/mentoory.css` | Add — card shadows, hover effects, typography |
| `tests/Mentoory.Tests.E2E/Tests/DashboardRenderingTests.cs` | Update — add card structure assertions |
