# Brainstorm Overview

Last updated: 2026-04-13

## Sessions

| # | Date | Topic | Status | Spec |
|---|------|-------|--------|------|
| 01 | 2026-04-11 | context-selector-ux | spec-created | 008 |
| 02 | 2026-04-13 | tabler-template-migration | spec-created | 009 |
| 03 | 2026-04-13 | design-system-ux-polish | spec-created | 010, 011 |
| 04 | 2026-04-13 | notification-email-delivery | spec-created | 011 |
| 05 | 2026-04-13 | table-polish | spec-created | 012 |
| 06 | 2026-04-13 | table-filtering | spec-created | 013 |

## Open Threads
- GlobalAdmin incubator dropdown may need search/filter at scale (from #01)
- Tabler Icons rendering approach: inline SVG vs webfont (from #02)
- Logo SVG delivery mechanism: extract from PDF or create fresh? (from #03)
- CSS variable prefix: --mentory- vs --mentoory- (from #03)
- Global --tblr-primary override impact on non-primary Tabler components (from #03)
- Global card shadow/hover styles may need tuning after visual QA (from #03 revisit)
- Should the icon registry be extensible by individual views? (from #05)
- Consider standardizing date formatting across tables in a follow-up (from #05)
- Filter panel animation: CSS transitions vs Bootstrap collapse (from #06)
- URL param namespacing strategy to avoid conflicts with existing query params (from #06)
- Filter panel layout on tables with 7+ filterable columns (from #06)
- Navigation/menu redesign (capability-based instead of role-based) — needs own brainstorm and spec
- Should ProjectCoordinator be able to do individual user creation?
- Automatic reminder emails before invitation expiration
- Should LoginAttemptEvent be enriched with IP/User-Agent, or should web layer publish a separate notification event? (from #04)
- IP geolocation for login alerts — deferred to future spec (from #04)
- Password reset email migration from Access to Notification domain — future spec (from #04)
- Admin dashboard for notification monitoring — future spec (from #04)

## Parked Ideas
(none)
