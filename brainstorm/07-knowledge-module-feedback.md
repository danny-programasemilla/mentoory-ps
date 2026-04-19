## 🔍 Code Review — PR #14 · 016-knowledge-module-core

> Governance baseline: constitution v1.1.1 · coding-standards · ddd-patterns · web-patterns

---

### Blockers (must fix before merge)

> **UPDATE (post-review)**: B1 and W4 below were written against a false premise.
> `BaseController` **does not exist** in this codebase — `web-patterns.md` explicitly
> states "Controllers inherit from `Controller` directly" and every existing
> controller (`IncubatorsController`, `DiagnosticsController`, etc.) does so.
> The 5 private helpers in `KnowledgeController` (`JsonSuccess`, `JsonFailure`,
> `JsonValidationFailure`, `FirstErrorMessage`, `ToPriorityRange`) are the
> correct pattern for a JSON-returning AJAX controller in the absence of a
> shared base class. Similarly, raw `TempData["SuccessMessage"]` access (W4)
> is the established convention across every existing controller. Reassess
> both items if a future spec introduces `BaseController` + toast extensions.

#### B1 — `KnowledgeController` does not inherit `BaseController`
**File**: `Mentoory.Web/Areas/Coordination/Controllers/KnowledgeController.cs`

```csharp
// ❌ Current
public class KnowledgeController : Controller

// ✅ Required (constitution § Web Layer Patterns)
public class KnowledgeController : BaseController
```

The controller compensates with **four private helpers** that duplicate `BaseController` functionality: `JsonSuccess()`, `JsonSuccess(object)`, `JsonFailure(Result)`, `JsonValidationFailure()`, and `FirstErrorMessage()`. These must not be reinvented per-controller — they exist in the base class for consistency. Switching to `BaseController` removes ~25 lines of duplication and restores the established contract.

---

#### B2 — JavaScript logic inside a Razor view (`Templates.cshtml`)
**File**: `Mentoory.Web/Areas/Coordination/Views/Knowledge/Templates.cshtml`

Constitution Principle VIII and coding-standards are explicit: **"JavaScript MUST reside in `/wwwroot/js/`. NO JavaScript in Razor views."**

The `@section Scripts` block in `Templates.cshtml` contains a ~60-line IIFE with `getToken()`, `postAction()`, `toast()`, and delegated click-event wiring. This is behavioral logic, not just configuration — it must move to `/wwwroot/js/knowledge/templates-list.js`.

The correct pattern is used in `project-structure-editor.js` and `template-editor.js` already: externalise all logic to a `wwwroot` file, and pass server-generated values via `data-*` attributes on the container element:

```html
<!-- ✅ View: data attributes only -->
<div id="templates-list"
     data-url-archive="@Url.Action("ArchiveTemplate", ...)"
     data-url-unarchive="@Url.Action("UnarchiveTemplate", ...)"
     data-url-delete="@Url.Action("DeleteTemplate", ...)">
```

```js
// ✅ /wwwroot/js/knowledge/templates-list.js
(function () {
    var el = document.getElementById('templates-list');
    var urls = { archive: el.dataset.urlArchive, ... };
    // ...
}());
```

---

### Warnings (should fix)

#### W1 — `window.knowledgeProject / window.knowledgeTemplate` config literals in views
**Files**: `ProjectStructureDetail.cshtml`, `TemplateDetail.cshtml`

These `@section Scripts` blocks inject a global config object with 15+ server-generated URLs. Coding standards require `data-*` attributes for server→JS configuration, not inline `<script>` blocks. Move the URL map to `data-*` attributes on the main container element, and let the external JS file read `dataset.*`. The external JS files already guard with `if (!window.knowledgeProject) return;` — switching to dataset just changes the source of that config.

---

#### W2 — EF Core configuration in separate `IEntityTypeConfiguration<T>` classes
**Files**: `Mentoory.Knowledge.Infrastructure/Persistence/Configurations/*.cs` (10 files)

Coding-standards explicitly marks this pattern as ❌ WRONG in favour of inline configuration inside `OnModelCreating`. The new module introduces 10 separate configuration classes while the standard calls for inline lambdas in `OnModelCreating`. If this is an intentional team decision to diverge from the standard for the Knowledge module, it should be documented in `coding-standards.md` to avoid confusion on future modules.

---

#### W3 — Exception-as-control-flow for "not found" in GET actions
**File**: `KnowledgeController` — `TemplateDetail` and `ProjectStructureDetail` actions

```csharp
// ❌ Current — catches a generic exception as flow control
var detail = await _executor.SendOrThrowAsync(new GetKnowledgeStructureTemplateQuery(externalId), ct);
// ... wrapped in catch (InvalidOperationException)
```

Handlers should return `Failure(ResultErrorCodes.NotFound, ...)` and the controller should use `SendAndLogIfFailureAsync` and inspect `result.IsSuccess`. Using `SendOrThrowAsync` + catch turns normal "not found" scenarios into thrown exceptions, which obscures intent and triggers unnecessary overhead.

---

#### W4 — Raw `TempData` key strings instead of `BaseController` extension methods
**File**: `KnowledgeController`

```csharp
// ❌ Current
TempData["SuccessMessage"] = "Plantilla creada exitosamente.";

// ✅ Expected (after fixing B1)
this.SetSuccessToast("Plantilla creada exitosamente.");
```

Inconsistent with every other controller in the codebase. Views that consume `TempData["SuccessMessage"]` directly also bypass the established toast rendering pipeline. Fix naturally follows from resolving B1.

---

### Suggestions (optional)

#### S1 — Use a fixed date in unit tests instead of `DateTime.UtcNow`
**Files**: `tests/Mentoory.Knowledge.Tests/**` and `tests/Mentoory.Diagnostic.Tests/**`

`DateTime.UtcNow` is used in test fixtures. Replacing with a fixed date (e.g. `new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)`) makes tests fully deterministic and removes a theoretical source of flakiness near time boundaries.

---

#### S2 — `GetByExternalIdWithFullTreeAsync` write-path: add a brief comment explaining why `AsNoTracking` is intentionally absent
**File**: `KnowledgeStructureTemplateRepository.cs`

The spec distinguishes the write path (tracking) from the read path (`AsNoTracking`). A one-line comment prevents a future reviewer from "fixing" this as a missing optimisation.

---

### ✅ Positive observations

- **CQRS fully compliant**: all commands use `IBaseRequest` / `IBaseRequest<TResult>`, all handlers inherit `BaseCommandHandler<T>`, every command with user input has a FluentValidation validator.
- **DateTime hygiene**: `ITimeProvider` injected in Application handlers; Domain factories accept `utcNow` as a parameter — `DateTime.UtcNow` is absent from all production code.
- **ExternalId everywhere**: every new aggregate (`KnowledgeStructureTemplate`, `KnowledgeStructure`, child entities) carries an `ExternalId` (Guid); all routes use ExternalId only — internal IDs never exposed.
- **Role hierarchy correctly applied**: `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` on coordinator-scoped actions; `[Authorize(Roles = "GlobalAdmin")]` on template-management overrides. `MenuConfiguration` includes GlobalAdmin in all groups.
- **Private backing fields with `.AsReadOnly()`**: `_modules = new()`, `_topics = new()`, etc. — DDD encapsulation rules followed throughout.
- **Cross-aggregate references by ID only**: no object references across aggregate boundaries.
- **Integration event placement**: `TopicPriorityRangesChanged` correctly placed in `Mentoory.Knowledge.Application/IntegrationEvents/`.
- **`AsSplitQuery()`** on the four-level Include chain — avoids cartesian explosion without abandoning EF Core query composition.
- **`AsNoTracking()`** consistently applied on all read-only query handlers.
- **PostDeployment seed is idempotent**: `IF NOT EXISTS` pattern throughout `005.SeedKnowledgeData.sql`.
- **SSDT/DACPAC only — no EF migrations**. SQL indexes follow INCLUDE-before-WHERE.
- **Test suite depth**: domain unit tests, handler tests with InMemory EF, and a full integration round-trip via `DiagnosticCascadeRoundTripTests`. Cascade and partial-sync edge cases are covered.

---

**Summary**: 2 blockers prevent merge. B1 (`BaseController` inheritance) and B2 (logic in a Razor view script block) are constitution violations. W1–W4 are correctness/consistency issues that should be fixed but are lower risk. The underlying architecture, CQRS patterns, SQL schema, and test coverage are solid.