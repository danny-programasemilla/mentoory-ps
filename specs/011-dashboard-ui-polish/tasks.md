# Tasks: Dashboard Rewrite & UI Polish

**Input**: Design documents from `specs/011-dashboard-ui-polish/`
**Prerequisites**: plan.md (required), spec.md (required), research.md

**Tests**: E2E test updates included as requested by user.

**Organization**: Tasks grouped by user story — US1 (bug fix), US2 (polish), US3 (regression QA).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: User Story 1 - Bug-Free Dashboard Layout (Priority: P1)

**Goal**: Fix card-status-start pattern, enhance stat card typography, add data-testid attributes. Dashboard renders without visual bugs.

**Independent Test**: Navigate to `/Administration/Dashboard` as IncubatorAdmin — cards render horizontally, colored strips contained, no vertical text.

### Implementation for User Story 1

- [X] T001 [US1] Rewrite navigation cards to use correct Tabler child-div card-status-start pattern (bg-primary, bg-success, bg-info) in `Mentoory.Web/Areas/Administration/Views/Dashboard/Index.cshtml`
- [X] T002 [US1] Enhance stat cards with prominent metric typography (h1 for count, text-secondary for description) in `Mentoory.Web/Areas/Administration/Views/Dashboard/Index.cshtml`
- [X] T003 [US1] Add data-testid attributes to all dashboard cards (stat-card-users, stat-card-projects, nav-card-projects, nav-card-users, nav-card-batch) in `Mentoory.Web/Areas/Administration/Views/Dashboard/Index.cshtml`
- [X] T004 [US1] Verify build compiles clean with `dotnet build` (zero warnings)

**Checkpoint**: Dashboard renders without layout bugs — no vertical text, no full-height colored lines, no overlapping content.

---

## Phase 2: User Story 2 - Visual Polish & Brand Warmth (Priority: P2)

**Goal**: Add card shadows, hover transitions, and brand warmth to shared CSS. Dashboard feels polished and professional.

**Independent Test**: Cards have visible shadow at rest, navigation cards lift on hover with deepened shadow, coral brand is visually present.

### Implementation for User Story 2

- [X] T005 [US2] Add card box-shadow at rest and transition property (box-shadow 0.2s ease, transform 0.2s ease) in `Mentoory.Web/wwwroot/css/mentoory.css`
- [X] T006 [US2] Add interactive card hover state with translateY(-2px) lift and elevated shadow via .card-link-hover class in `Mentoory.Web/wwwroot/css/mentoory.css`
- [X] T007 [US2] Apply .card-link-hover class to navigation cards in `Mentoory.Web/Areas/Administration/Views/Dashboard/Index.cshtml`
- [X] T008 [US2] Verify brand coral color is visually present in avatar backgrounds, card-status-start strips, and link hover states in `Mentoory.Web/wwwroot/css/mentoory.css`
- [X] T009 [US2] Verify build compiles clean with `dotnet build` (zero warnings)

**Checkpoint**: Dashboard cards have depth, hover effects work, brand identity is felt.

---

## Phase 3: E2E Tests (Priority: P2)

**Goal**: Update E2E tests to verify correct card rendering and catch regressions.

**Independent Test**: `dotnet test --filter "DashboardRendering"` passes.

### E2E Test Implementation

- [X] T010 [US1] Enhance IncubatorAdminDashboard_ShouldRender_WithContent to assert stat cards and nav cards are visible via data-testid selectors in `tests/Mentoory.Tests.E2E/Tests/DashboardRenderingTests.cs`
- [X] T011 [US1] Add test IncubatorAdminDashboard_ShouldRender_StatCardMetrics to verify stat cards display numeric values and nav card links render horizontally in `tests/Mentoory.Tests.E2E/Tests/DashboardRenderingTests.cs`
- [X] T012 [US1] Run E2E tests with `dotnet test tests/Mentoory.Tests.E2E --filter "DashboardRendering"` and verify all pass

**Checkpoint**: E2E tests pass, confirming card structure is correct.

---

## Phase 4: User Story 3 - No Regressions on Key Pages (Priority: P3)

**Goal**: Verify shared CSS changes don't break other pages.

**Independent Test**: Navigate to each QA page — layouts intact, no styling artifacts.

### Visual QA

- [X] T013 [US3] Visual QA: Users Index at `/Administration/Users` — table layout, sidebar, topbar intact
- [X] T014 [US3] Visual QA: Projects Index at `/Administration/Projects` — card/table layout intact
- [X] T015 [US3] Visual QA: Diagnostics Index at `/Coordination/Diagnostics` — page renders correctly
- [X] T016 [US3] Visual QA: Batch Upload at `/Administration/BatchUpload` — page renders correctly
- [X] T017 [US3] Visual QA: Sidebar navigation on any page — active states, hover effects, section headers correct
- [X] T018 [US3] Visual QA: Login page at `/Access/Login` — auth gradient panel and decorative elements intact

**Checkpoint**: All 6 QA pages render correctly. No regressions.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and cleanup.

- [X] T019 Run full build verification with `dotnet build` (zero warnings)
- [X] T020 Run full E2E test suite with `dotnet test tests/Mentoory.Tests.E2E` to ensure no regressions
- [X] T021 Run quickstart.md validation steps

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (US1 - Bug Fix)**: No dependencies — start immediately
- **Phase 2 (US2 - CSS Polish)**: Depends on T001 completion (correct markup must exist before styling)
- **Phase 3 (E2E Tests)**: Depends on T001-T003 completion (data-testid attributes must exist)
- **Phase 4 (US3 - Visual QA)**: Depends on Phase 1 + Phase 2 completion (all CSS changes must be in place)
- **Phase 5 (Polish)**: Depends on all phases completion

### User Story Dependencies

- **US1 (P1)**: Independent — can start immediately
- **US2 (P2)**: Depends on T001 (card markup must be correct before adding hover classes)
- **US3 (P3)**: Depends on US1 + US2 (all changes must be in place before QA)

### Within Each User Story

- View markup changes (T001-T003) before CSS additions (T005-T008)
- CSS additions before E2E tests (tests need rendered output to verify)
- All code changes before visual QA

### Parallel Opportunities

- T001, T002, T003 modify the same file — execute sequentially
- T005, T006, T008 modify the same file — execute sequentially
- T010, T011 modify the same file — execute sequentially
- T013-T018 are independent visual QA tasks — can run in parallel

---

## Parallel Example: Visual QA Phase

```bash
# All QA tasks can run in parallel (different pages, no dependencies):
Task: "Visual QA: Users Index"
Task: "Visual QA: Projects Index"
Task: "Visual QA: Diagnostics Index"
Task: "Visual QA: Batch Upload"
Task: "Visual QA: Sidebar navigation"
Task: "Visual QA: Login page"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Fix card-status-start, enhance stat cards, add data-testid
2. **STOP and VALIDATE**: Dashboard renders without bugs
3. Continue to Phase 2 for polish

### Incremental Delivery

1. Phase 1 (US1) → Bug-free dashboard → validate
2. Phase 2 (US2) → Polished dashboard → validate
3. Phase 3 (E2E) → Automated verification → validate
4. Phase 4 (US3) → Regression-free across app → validate
5. Phase 5 → Final verification

---

## Notes

- T001-T003 and T005-T008 each touch the same files — cannot be parallelized within their groups
- Visual QA tasks (T013-T018) require running the app — use `dotnet run --project Mentoory.Web`
- E2E tests require the Playwright + Testcontainers infrastructure (auto-managed by PlaywrightFixture)
- All UI text must remain in Spanish (constitution Principle IX)
- Use Context7 for any Tabler pattern lookups during implementation
