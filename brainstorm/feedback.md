## 🔍 Code Review — PR #8
_Branch: `013-table-filtering` → `develop`_

---

### Blockers (must fix before merge)

#### 1. `DashboardController` injects `IMediator` directly — violates web-layer convention
**File:** `Mentoory.Web/Areas/Administration/Controllers/DashboardController.cs`

```csharp
// Added:
public class DashboardController(IMediator mediator) : Controller
...
var result = await mediator.Send(new GetDashboardMetricsQuery(incubatorId));
```

All controllers must use `MediatRExecutor` (the project wrapper), not raw `IMediator`. The standard pattern is:
```csharp
public class DashboardController(MediatRExecutor mediator) : Controller
...
var result = await mediator.SendOrThrowAsync(new GetDashboardMetricsQuery(incubatorId));
```

Using raw `IMediator.Send()` also bypasses the failure logging convention. When `result.IsSuccess` is false, this PR silently returns empty data — no log, no trace. `SendAndLogIfFailureAsync` handles this automatically.

---

#### 2. `GetDashboardMetricsQueryHandler` crosses module boundaries — Clean Architecture violation
**File:** `Mentoory.Tenant.Application/Queries/GetDashboardMetrics/GetDashboardMetricsQueryHandler.cs`

```csharp
using Mentoory.Access.Domain.Repositories;  // ← foreign module domain reference
...
IRoleAssignmentRepository roleAssignmentRepository,  // ← injecting Access domain repo into Tenant application
```

`Mentoory.Tenant.Application` must not import from `Mentoory.Access.Domain`. Each module's Application layer may only depend on its own Domain. Cross-module reads must go through:
- A MediatR query sent to the Access module, or
- A shared read model / integration event

This introduces a hard project reference from `Mentoory.Tenant` → `Mentoory.Access.Domain` that the architecture explicitly forbids.

---

#### 3. `ListProjectsHandler` — `Enum.ToString()` inside EF query will throw at runtime
**File:** `Mentoory.Tenant.Application/Queries/ListProjects/ListProjectsHandler.cs`

```csharp
query = query.Where(p => p.CurrentStageType.ToString().ToLower().Contains(stageFilter.ToLowerInvariant()));
```

EF Core cannot translate `Enum.ToString()` to SQL. This will throw `InvalidOperationException: The LINQ expression could not be translated` the first time this filter is used. Fix options:
- Accept the enum value as an integer and compare: `&& Enum.TryParse<StageType>(stageFilter, out var stage)` then `Where(p => p.CurrentStageType == stage)` (exact match, semantically correct for an enum)
- Or use a `LIKE` workaround with `EF.Functions.Like`, but prefer enum equality for a bounded-value type

---

### Warnings (should fix)

#### 4. Case-normalization inconsistency across filter handlers
Some handlers use `.ToUpper()/.ToUpperInvariant()` (Access handlers), others use `.ToLower()/.ToLowerInvariant()` (Tenant handlers). Pick one convention and apply it everywhere — the SQL collation makes it moot in practice, but inconsistency is a maintenance hazard.

#### 5. Filter dictionary keys are magic strings with no shared constants
**All filter handlers + `datatable-helper.js` FILTER_TYPE_REGISTRY**

Keys like `"email"`, `"firstName"`, `"accountStatus"`, `"currentStageType"` are raw string literals on both the server (C# dictionaries) and client (JS registry). If a JS key is renamed, the server-side filter silently returns all rows — no compilation error, no test failure. Consider a static class of filter key constants in the shared layer, or at minimum document the contract explicitly.

#### 6. `DataTableViewComponent` — removed `apiUrl` parameter is a silent contract change
**File:** `Mentoory.Web/ViewComponents/DataTableViewComponent.cs`

The old `apiUrl` parameter is gone from the component signature. If any view still passes it via anonymous object, it silently gets dropped without a compile error. Verify no callers in any remaining view pass `apiUrl` — a thorough search is needed since the component is invoked dynamically.

#### 7. `MenuItem.BadgeCount` added as a mutable property inconsistent with existing design
**File:** `Mentoory.Web/Infrastructure/Menu/MenuItem.cs`

All other `MenuItem` properties are read-only (`{ get; }`), set only via constructor. `BadgeCount` is added as `{ get; set; }` — mutable post-construction. This breaks the immutability pattern; either initialize it in the constructor or make it `init`.

---

### Suggestions (optional)

**S1. `currentStageType` filter is a text `Contains` on an enum — semantically wrong even if fixed.**
An enum field like `CurrentStageType` should be filtered by exact equality (dropdown), not substring search. A `Contains` filter on `"Stage1"` would match both `Stage10` and `Stage1`. Even after fixing the EF translation, the semantic is incorrect.

**S2. `GetDashboardMetricsQueryHandler` is missing `AsNoTracking()`**
```csharp
var userCount = await roleAssignmentRepository.Query()  // ← no AsNoTracking()
    .Where(ra => ra.IncubatorId == request.IncubatorId && ra.IsActive)
    ...
```
Read-only query handlers must include `AsNoTracking()` per the coding standards.

**S3. `GetDashboardMetricsQueryHandler` class should be `sealed`**
All other query handlers in the codebase are `sealed`. The new handler is not. Small inconsistency.

---

### ✅ Positive observations

- **Filter panel architecture is clean**: auto-generating the panel from `FILTER_TYPE_REGISTRY` keyed on render function signatures is genuinely clever. Zero view-level markup changes is a real win.
- **URL persistence via `f_*` params**: bookmarkable filtered views are a well-known UX pattern, implemented correctly with symmetric sync/load helpers.
- **Design system migration is thorough**: all `fas fa-*` icons replaced with `ti ti-*`, all `badge bg-*` replaced with `renderStatus()`/`renderAccountStatus()`, Bootstrap utility lib removed in favour of Tabler — consistent and complete.
- **`_AuthLayout.cshtml`**: introducing a dedicated auth layout and pointing `_ViewStart.cshtml` at it is exactly right. Auth views no longer need `Layout = "_Layout"` in every file.
- **`DataTableViewComponent` simplification**: dropping `apiUrl` and `filterId` from the component and letting each view's script own its URL construction reduces coupling. Clean.
- **Dashboard metrics**: the empty-state / populated-state branching in `Dashboard/Index.cshtml` is a good UX improvement and the `data-testid` attributes show test-awareness.
- **Filter block guard `is { Count: > 0 }`**: skips the dictionary lookup entirely when no filters are sent — efficient.

---

**Summary**: Issues 1–3 are blockers. #2 (cross-module domain reference) is the most architecturally significant and requires a design decision before it can be resolved. The filter feature itself (JS + server-side) is well-conceived; the issues are in the wiring layer.