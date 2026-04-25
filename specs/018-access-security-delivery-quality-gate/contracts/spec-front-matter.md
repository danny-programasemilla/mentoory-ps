# Contract: Spec Front-Matter

Defines the optional YAML front-matter at the top of every `spec.md` and the specific key the coverage tool reads.

## Syntax

YAML front-matter, opened by a line containing exactly `---`, closed by another line containing exactly `---`, must begin at line 1 of the file.

```markdown
---
access-security: true
---

# Feature Specification: ...
```

## Recognised keys

| Key | Type | Purpose |
|-----|------|---------|
| `access-security` | `bool` | When `true`, this spec's feature is subject to floor-category enforcement (FR-008). Default: `false` if key absent or front-matter absent. |
| `feature-num` | `string` | Optional. If present, cross-checked against the directory name's prefix; mismatch is a parse warning (not an error). |
| `status` | `string` | Optional. Freely-used field; tool does not enforce. |

Any other key is ignored by the tool (reserved for future use or human reference).

## Absence policy

- If `spec.md` has NO front-matter block at all, it is treated as a non-opted-in spec. Floor-category enforcement does not apply. Requirement-identifier traceability still applies (FR-003).
- If `spec.md` has front-matter but omits `access-security`, same as above.
- If front-matter is malformed (opens with `---` but does not close within 30 lines, or YAML syntax error), tool exits with code 2 (EH-001 + research.md #9).

## Feature-018 opt-in requirements

Two specs MUST include `access-security: true` as part of this feature's implementation:

1. `specs/016-registration-access-hardening/spec.md` — the feature whose coverage the gate asserts first.
2. `specs/018-access-security-delivery-quality-gate/spec.md` — this spec; opting itself in is a sanity check (tests of the coverage tool itself are expected to cover every FR/SC in this spec via `[Trait("Spec", …)]`).

Other existing specs (001–015, 017) do NOT need front-matter added as part of this feature. Organic migration happens as those areas are next touched.

## Parser behaviour

- Only the first 100 lines of `spec.md` are scanned for front-matter. This bounds worst-case parse time (NFR-001).
- YAML parser supports only the flat key-value subset: `key: value`, one pair per line, values limited to `true`, `false`, quoted strings, and unquoted strings without special chars. Nested mappings, lists, anchors, etc. are not supported (and are not needed).
- Unknown keys are preserved but ignored; no warning emitted.

## Example specs

### Opted in

```markdown
---
access-security: true
---

# Feature Specification: Password Reset

**Feature Branch**: `019-password-reset`
...
```

### Not opted in (default)

```markdown
# Feature Specification: Dashboard Polish

**Feature Branch**: `020-dashboard-polish`
...
```

No front-matter. Tool treats as non-access-security; still enforces FR/SC traceability.

## Interaction with `Coverage: N/A` exclusion markers

Orthogonal. Front-matter controls floor-category enforcement; exclusion markers control individual identifier enforcement. A spec can be `access-security: true` AND have exclusion markers for specific identifiers. The markers work identically regardless of front-matter.
