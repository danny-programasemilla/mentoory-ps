# Contract: Governance Update — `access-security-constitution.md`

**Target:** `.specify/memory/access-security-constitution.md`
**Action:** Additive section; semantic minor version bump.

## New section — verbatim draft

Append the following section at the end of the existing constitution, before the version footer:

```markdown
## Audit Trail Obligations

### Scope

A **sensitive command** is any MediatR command class whose name
matches the pattern:

```
^(Assign|Approve|Correct|Advance|Login|Register|SetActive).*Command$
```

This pattern is the canonical definition. The architecture test
`Mentoory.Tests.Architecture.AuditCoverageTests` enforces it at
build time.

Current categories and their intent:

- `Assign*` — granting access or responsibility (roles, mentors, ...).
- `Approve*` — endorsing an artifact for progression (plans, advancements, ...).
- `Correct*` — altering a recorded value after the fact.
- `Advance*` — transitioning a workflow forward (stages, phases).
- `Login*` — authentication attempts.
- `Register*` — user / entity onboarding.
- `SetActive*` — selecting operational context (tenants, projects, roles).

### Obligation

Every sensitive command MUST be decorated with `[Audited]`
(`Mentoory.Shared.Application.Audit.AuditedAttribute`). The
architecture test fails CI if the obligation is violated.

- **Default mode** (`AuditMode.Automatic`): the `AuditingBehavior`
  captures a uniform payload (user, tenant, role, correlation,
  outcome, redacted command payload).
- **Escape hatch** (`AuditMode.Manual`): used when the entry needs
  domain-specific detail (e.g., before/after text from an aggregate).
  The handler calls `IAuditService.LogAsync` directly and is
  responsible for its own audit entry.

### Extension procedure

When a new sensitive command category enters the codebase (e.g.,
`Delete*`, `Revoke*`, `Reset*`):

1. Extend the regex in `Mentoory.Tests.Architecture.AuditCoverageTests`
   AND this section's scope list, in the same pull request.
2. Add the new event-type constant to
   `Mentoory.Shared.Application.Audit.AuditEventTypes`.
3. Apply `[Audited]` to the new command.
4. Update the `access-security-constitution.md` version footer
   per its SemVer policy.

Extending the regex WITHOUT updating this section (or vice versa)
constitutes a constitution violation and MUST be rejected in code
review.

### Non-obligations

- Query handlers are NOT audited in v1.
- `ValidationException`s (short-circuited by `ValidatorBehavior`)
  are NOT audited — they represent input noise, not security events.
- Audit failures MUST NOT fail the wrapped business command
  (best-effort semantics preserved from the pre-016 `AuditService`).

### Audience

All contributors writing new commands. Reviewers enforce this at
code-review time; CI enforces at build time; the constitution is
the canonical reference.
```

## Version bump

`.specify/memory/access-security-constitution.md` carries its own version footer. Per its SemVer policy:
- Additive new section + new enforcement mechanism → **MINOR** bump.
- Update the "Sync Impact Report" HTML comment at the top.
- Update the `**Version**: X.Y.Z` line at the bottom.
- Update `**Last Amended**: 2026-04-18`.

## Cross-link

Add a pointer from `.specify/memory/constitution.md` § "Related Governance Documents" — the constitution already references `access-security-constitution.md`, so no new link is needed. However, add one sentence to the "Specification Validation & Enforcement" checklist (item 9 or new item 10):

```
10. Do all sensitive commands carry [Audited] per
    access-security-constitution.md § "Audit Trail Obligations"?
    (Enforced by Mentoory.Tests.Architecture.AuditCoverageTests.)
```

This entails a **PATCH** bump to `constitution.md` (clarification, not a new principle).
