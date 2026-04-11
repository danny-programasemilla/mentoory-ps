## Summary

<!-- Brief description of what this PR does and why -->

## Changes

<!-- List the key changes made -->

-

## Access & Security Checklist (Required)

> **All items below must be completed.** PRs with incomplete access sections will be rejected during review.
> Reference: [Access & Security Constitution](.specify/memory/access-security-constitution.md) Section 10

- [ ] **Business capability**: What is being added/modified?
  <!-- Describe in business terms -->
- [ ] **Permitted roles**: Which roles may perform this action?
  <!-- List roles. Consult Permission Matrix (Section 6) -->
- [ ] **Scope**: What scope applies? (Platform / Incubator / Project)
  <!-- Specify scope filter for data isolation -->
- [ ] **Category**: Platform, Incubator, or Project feature?
  <!-- Platform = GlobalAdmin only -->
- [ ] **Impact**: Can this affect users, permissions, or visibility?
  <!-- If yes, describe safeguards -->
- [ ] **Backend enforcement**: What checks enforce authorization?
  <!-- [Authorize], CheckPermissionQuery, ITenantContext -->
- [ ] **Audit events**: What audit events are required?
  <!-- Consult Permission Matrix "Audit" column -->
- [ ] **Security tests**: What tests prove boundaries hold?
  <!-- Minimum: 1 positive + 1 negative authorization test -->

## Test Plan

<!-- How was this tested? -->

-
