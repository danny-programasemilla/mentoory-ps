# Quickstart: Access & Security Constitution

**Feature**: 005-access-security-constitution  
**Date**: 2026-04-08

## What This Feature Produces

A single governance document at `.specify/memory/access-security-constitution.md` containing 13 sections that formalize the platform's role hierarchy, scope boundaries, permission matrix, threat model, and enforcement guidelines. Plus two integration points:

1. A link added to `constitution.md` referencing the new document
2. A PR template access checklist section in `.github/PULL_REQUEST_TEMPLATE.md`

## Prerequisites

- Branch `005-access-security-constitution` checked out
- Familiarity with the spec (`specs/005-access-security-constitution/spec.md`)
- Research findings reviewed (`specs/005-access-security-constitution/research.md`)

## Authoring Approach

This is a **documentation task**, not a coding task. The work is:

1. **Write the governance document** — Structured Markdown following the 13-section hierarchy defined in `data-model.md`
2. **Tag all statements** — Every factual statement tagged as [CONFIRMED], [INFERRED], [RECOMMENDED], or [OPEN]
3. **Build the permission matrix** — 17+ rows following the row schema in `data-model.md`
4. **Write the developer checklist** — Actionable questions per FR-014
5. **Create the PR template section** — Access checklist for `.github/PULL_REQUEST_TEMPLATE.md`
6. **Link from constitution.md** — Add reference in the existing constitution

## Key Sources

| Content Needed | Source |
|---------------|--------|
| Role definitions | `Mentoory.Access.Domain/Enums/PlatformRole.cs` |
| Permission mapping | `Mentoory.Access.Application/Queries/CheckPermission/CheckPermissionHandler.cs` |
| Permission enum | `Mentoory.Access.Domain/Enums/Permission.cs` |
| Controller authorization | `[Authorize(Roles = "...")]` across all controllers |
| Tenant isolation | `Mentoory.Web/Infrastructure/Authorization/TenantContextMiddleware.cs` |
| Session management | `Mentoory.Web/Infrastructure/Authentication/SessionAuthenticationMiddleware.cs` |
| RoleAssignment model | `Mentoory.Access.Domain/Aggregates/RoleAssignment/RoleAssignment.cs` |
| User lifecycle | `Mentoory.Access.Domain/Aggregates/User/User.cs` |
| Project/Participant model | `Mentoory.Tenant.Domain/Aggregates/Project/Project.cs` |
| Existing constitution | `.specify/memory/constitution.md` |

## Validation

After writing, validate against:
- `specs/005-access-security-constitution/checklists/requirements.md` — quality checklist
- Success criteria SC-001 through SC-007 in the spec
- Constitution Principle X alignment (Role Hierarchy & Session Context)

## Next Command

After tasks are generated: `/speckit.tasks` then `/speckit.implement`
