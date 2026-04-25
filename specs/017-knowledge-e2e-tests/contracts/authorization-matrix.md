# Contract: Authorization × Route Coverage Matrix

**File**: drives `KnowledgeAuthorizationTests.cs` (method 6.1's `[Theory]` + `[InlineData]` set, method 6.2's `[Theory]`).

Every route under `/Coordination/Knowledge/**` and the Phase 9 addition to `/Administration/Projects/Create` is enumerated below with its expected behavior per role. The test generator will emit one `[InlineData]` per row × role combination that the row flags as "denied".

## Role Constants (used in test code)

| Role | Seed user | RoleLabel passed to `ContextSelection` |
|---|---|---|
| `GlobalAdmin` | `multirole@test.mentoory.com` | `"GlobalAdmin"` |
| `IncubatorAdmin` | `incadmin1@test.mentoory.com` | `"IncubatorAdmin"` |
| `ProjectCoordinator` | `coord1@test.mentoory.com` | `"ProjectCoordinator"` |
| *(anonymous)* | none | — |

## Route × Role Matrix

### A. Template CRUD (GlobalAdmin-only per NFR-K05)

| Route | Method | GlobalAdmin | IncubatorAdmin | ProjectCoordinator | Anonymous |
|---|---|---|---|---|---|
| `/Coordination/Knowledge/Templates` | GET | ✅ 200 | ❌ deny | ❌ deny | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Templates/Create` | GET | ✅ 200 | ❌ deny | ❌ deny | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Templates/Create` | POST | ✅ 302 | ❌ deny | ❌ deny | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Templates/{extId}` | GET | ✅ 200 | ❌ deny | ❌ deny | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Templates/{extId}/Edit` | GET | ✅ 200 | ❌ deny | ❌ deny | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Templates/{extId}/Edit` | POST | ✅ 302 | ❌ deny | ❌ deny | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Templates/{extId}/Archive` | POST | ✅ 302 | ❌ deny | ❌ deny | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Templates/{extId}/Modules` (+ Topics/Subjects/Resources) | any | ✅ | ❌ deny | ❌ deny | ↪ `/Access/Login` |

### B. Project-Clone CRUD (Coordinator/IncubatorAdmin/GlobalAdmin per NFR-K05)

| Route | Method | GlobalAdmin | IncubatorAdmin | ProjectCoordinator | Anonymous |
|---|---|---|---|---|---|
| `/Coordination/Knowledge/Projects` | GET | ✅ | ✅ | ✅ (scoped to context) | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Projects/{ksExtId}` | GET | ✅ | ✅ | ✅ (must match tenant) | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Projects/{ksExtId}/Modules` (+ Topics/Subjects/Resources) | any | ✅ | ✅ | ✅ | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Projects/{ksExtId}/Sync` | POST | ✅ | ✅ | ✅ | ↪ `/Access/Login` |
| `/Coordination/Knowledge/Projects/Clone` *(legacy route)* | GET/POST | ❌ retired | ❌ retired | ❌ retired | ↪ `/Access/Login` |

### C. Phase 9 Project Creation with KS Binding (Coordinator+ per post-amendment note)

| Route | Method | GlobalAdmin | IncubatorAdmin | ProjectCoordinator | Anonymous |
|---|---|---|---|---|---|
| `/Administration/Projects/Create` | GET | ✅ | ✅ (own incubator) | ✅ (own project — or deny per business rule) | ↪ `/Access/Login` |
| `/Administration/Projects/Create` | POST with valid KS template + own incubator | ✅ 302 | ✅ 302 | ✅/❌ per rule | ↪ `/Access/Login` |
| `/Administration/Projects/Create` | POST with foreign IncubatorId | ✅ (GlobalAdmin bypass) | ❌ deny | ❌ deny | ↪ `/Access/Login` |
| `/Administration/Projects/Create` | POST with missing KS template | 400/form-re-render (validation) | 400/form-re-render | 400/form-re-render | ↪ `/Access/Login` |

## Tenant Isolation (US6-3)

| Scenario | Actor | Target | Expected |
|---|---|---|---|
| Coord B attempts to GET coord A's project KS by ExternalId | `coordnorte@test.mentoory.com` (Incubadora Norte) | KS of `Proyecto Innovación` (Incubadora Primaria) | ≥ 400 OR body contains no names from A's tree |
| Coord A modifies `ActiveProjectId` cookie to B's projectId | `coord1` | B's project KS detail | Same — tenant filter on repository level rejects |

## Menu Visibility (US6-5)

| Role | "Plantillas de conocimiento" entry | "Estructuras del proyecto" entry |
|---|---|---|
| GlobalAdmin | visible | visible |
| IncubatorAdmin | hidden | visible |
| ProjectCoordinator | hidden | visible |

Assertion: `page.Locator("a", hasText: "Plantillas de conocimiento").Count` = expected-value.

## Theory `[InlineData]` Sets

### 6.1 `ProtectedRoutes_CoordinatorDenied_ForTemplateRoutes`

```csharp
[InlineData("/Coordination/Knowledge/Templates", "GET")]
[InlineData("/Coordination/Knowledge/Templates/Create", "GET")]
[InlineData("/Coordination/Knowledge/Templates/{seededKsExtId}", "GET")]
[InlineData("/Coordination/Knowledge/Templates/{seededKsExtId}/Edit", "GET")]
// …
```

The `{seededKsExtId}` placeholder is filled from `KnowledgeIntegrationHelpers.GetSeededKsTemplateExternalIdAsync()` in the theory setup — a known-constant Guid seeded by `005.SeedKnowledgeData.sql`.

### 6.2 `ProtectedRoutes_Unauthenticated_RedirectToLogin`

```csharp
[InlineData("/Coordination/Knowledge/Templates")]
[InlineData("/Coordination/Knowledge/Projects")]
[InlineData("/Coordination/Knowledge/Templates/Create")]
// …
```

## Assertion Pattern

For denials:

```csharp
var response = await page.GotoAsync(baseUrl + route);
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

var status = response?.Status ?? 200;
var stillOnProtectedRoute = page.Url.Contains(route, StringComparison.OrdinalIgnoreCase)
    && !page.Url.Contains("/Access/Login");

if (stillOnProtectedRoute)
{
    (status >= 400).Should().BeTrue(
        $"{role} must be denied on {method} {route} (got HTTP {status})");
}
else
{
    page.Url.Should().NotContain(route,
        $"{role} should be redirected away from {method} {route}");
}
```

This is the same pattern the existing `Templates_CoordinatorCannotAccess` test uses — extracted to a helper method on `KnowledgeAuthorizationTests` (private, scoped to that file since it's authz-specific).
