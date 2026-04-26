## 🔍 Code Review — PR #10

**PR:** Add platform roadmap gap analysis and per-feature brainstorm seeds
**Type:** Docs-only — 6 markdown files (766 additions, 2 deletions)

---

### Blockers (must fix before merge)

**1. RBAC gap description conflates two concerns — will produce wrong spec fix**

`06-platform-roadmap-gap-analysis.md` states controllers "use `[Authorize(Roles=...)]` only; none call `CheckPermission` query." The framing implies replacing role-based guards rather than layering permission checks on top. Clarify explicitly: the fix is to add `CheckPermissionQuery` calls **in addition to** — not instead of — existing `[Authorize]` attributes. As written, a future `/speckit.specify` session could generate a spec that removes working authorization.

**2. Tenant leakage fix guidance is too vague and may break GlobalAdmin operations**

The risk register recommends "add EF Core query filters in `OnModelCreating`" for `RoleAssignmentRepository.Query()` returning an unscoped `IQueryable<RoleAssignment>`. A global query filter tied to `ITenantContext` will silently hide data from `GlobalAdmin` cross-tenant operations. The seed must recommend either **scoped query filters with a GlobalAdmin bypass** or an explicit tenant-aware query method, not a blanket filter. This vague guidance will produce a broken spec.

**3. `RegisterUser` enumeration vulnerability is understated — should be a security blocker, not a backlog item**

`RegisterUserHandler` returns field-keyed errors `("NationalId", "Ya existe una cuenta...")` vs `("Email", "Ya existe una cuenta...")` on a public endpoint. This allows unauthenticated callers to enumerate whether a national ID or email is already registered. The document lists this as a scorecard footnote. It must be promoted to the **risk register** with Phase A priority, not left as a passive hardening observation.

---

### Warnings (should fix)

**4. `ProjectsController` missing `ProjectCoordinator` role is a constitution violation, not a backlog item**

The document lists this as a hardening item. Per the constitution, `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` is required on project-scoped controllers — the current attribute `"IncubatorAdmin,GlobalAdmin"` is a shipped constitution violation. Move this to the **Phase A bug-fix** gate, not the scorecard footnotes.

**5. `GetIncubatorExternalIdAsync` anti-pattern not captured in the gap analysis**

`ProjectsController` resolves the incubator's `ExternalId` by firing a full `ListIncubatorsQuery` and calling `.FirstOrDefault()` — an unnecessary DB query that will return wrong results for multi-assignment users. This "resolve ID via list query" pattern should be added to the Phase C authorization rigor stream.

**6. Notification outbox design leaves a false open question for the spec author**

`10-cross-cutting-hardening.md` line ~734 frames "shared DbContext vs. dual-write" as an open question, then later notes dual-write fails with module-scoped DbContexts. The seed should commit to a default recommendation (coordinator/interceptor over a shared `IUnitOfWork`) rather than leaving both options open. As-is, the spec session will likely choose dual-write and produce a broken implementation.

**7. Audit pipeline behavior scope is ambiguous and will invite over-engineering**

The before/after state capture question (`10-cross-cutting-hardening.md` ~line 783) is left open. Commit to a v1 scope: **audit command inputs + user/tenant context only**; entity-level diffs are out of scope. The current framing risks a spec that attempts full change tracking, which contradicts the MediatR pipeline behavior approach described elsewhere.

**8. Phase D `AsNoTracking` audit implicitly accepts that current query handlers may be non-compliant**

`CLAUDE.md` already requires `AsNoTracking()` on read-only paths as a code review standard. Pushing an audit to Phase D implies it's acceptable to ship query handlers without it. Either confirm all existing handlers are compliant, or surface any known offenders as immediate fix items.

---

### Suggestions (optional)

**9. Dependency graph missing `FormTemplate.DefaultKnowledgeStructureId` FK as a cross-module migration gate**

`07-knowledge-module.md` correctly calls out this FK, but the roadmap dependency graph doesn't flag it as a coordinated SSDT change touching both the Knowledge and Diagnostics schemas. This should be a single PR per SSDT conventions — add a note so the spec author doesn't plan two sequential migrations.

**10. `07-knowledge-module.md` open question #3 (Topic identity) is already answered — promote it to a decided note**

The seed asks "Does `Question.TopicId` point to the cloned topic or the template topic?" and immediately answers it. Leaving it as an open question wastes the brainstorm session. Mark it decided: "Diagnostic questions reference the cloned Topic (per-project); templates reference template topics."

**11. Phase exit criteria don't include a test coverage gate**

The gap analysis notes Knowledge, Mentoring, Notification, and Subscription have zero test files. Each phase's exit criteria should explicitly require unit tests for all new domain methods and handler tests for all new command/query handlers — not defer coverage to Phase D.

**12. `09-mentoring-execution.md` split recommendation should reference SpecKit's linked-spec convention**

The Spec A / Spec B split is the right call. Add a note that related specs should reference each other in their `brainstorm/00-overview.md` dependency table so future agents generate the cross-links correctly.

---

### ✅ Positive observations

- The 35% completion estimate is calibrated against actual FR counts, not feature names — credible and verifiable.
- Constitution compliance assessment (no `DateTime.UtcNow`, no AutoMapper, no Dapper as primary access, no repository injection in controllers) was verified against the codebase and is accurate.
- Dependency graph correctly identifies Knowledge as the single critical-path blocker with genuinely independent parallel streams.
- All 10 hardening items were verified against the codebase. Items 1, 4, 6, 7, and 9 are confirmed accurate.
- Per-module seeds consistently reference the correct constitution constraints (ExternalId on external entities, `IBaseRequest`/`BaseCommandHandler`, FluentValidation, `AsNoTracking` on read paths, forbidden patterns list). These will produce constitution-compliant specs.
- `RoleAssignmentRepository.Query()` unscoped `IQueryable` is flagged — a real issue that required real code inspection to find.
- The "What's Out of Scope" section explicitly matches Spec 001 assumptions, which prevents scope creep in future planning sessions.

---

_Reviewed against `.specify/memory/constitution.md`, `.claude/coding-standards.md`, `.claude/ddd-patterns.md`, `.claude/web-patterns.md`, `.claude/common-issues.md`. Codebase references verified: `ProjectsController.cs`, `RegisterUserHandler.cs`, `RoleAssignmentRepository.cs`._

🤖 Generated with [Claude Code](https://claude.ai/claude-code)