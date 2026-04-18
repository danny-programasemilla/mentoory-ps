# Brainstorm Seed: Mentoring Plan (US4)

**Date:** 2026-04-18
**Status:** parked
**Spec:** —
**Parent roadmap:** [06-platform-roadmap-gap-analysis.md](./06-platform-roadmap-gap-analysis.md) (Phase B, hot stream)
**Blocked by:** [07-knowledge-module.md](./07-knowledge-module.md) must complete first

> This is a **seed document**. When starting the Mentoring Plan stream (after Knowledge is delivered), invoke `/spex:brainstorm` with this file as context.

---

## Problem Framing

The Mentoring Plan is the **core product value**: it translates a completed diagnostic into a personalized list of priority topics that the mentor + entrepreneur will work through. The system auto-suggests based on score aggregation and priority mapping, then the humans collaboratively adjust and approve.

Today: the module is an empty scaffold. Diagnostic publishes `DiagnosticCompletedEvent` with no subscriber. The score-aggregation query `GetTopicScoreAggregationHandler` exists but is not consumed. No `MentoringPlan` aggregate, no generation logic, no adjust/approve UI.

## Current State

- `Mentoory.Mentoring.Domain` / `.Application` / `.Infrastructure` projects exist, empty
- `Mentoory.Mentoring.Tests` exists, empty
- `DiagnosticCompletedEvent` is published in Diagnostic, unsubscribed
- `TopicScoreDto` + `GetTopicScoreAggregationHandler` exist in Diagnostic and return `TotalScore` + `Count` + `AverageScore` per topic
- Mentor assignments live in Tenant module (many-to-many with lead flag) and are usable

## Key FRs involved

- **FR-025** — Aggregate scores per topic from Initial evaluation responses → cumulative score
- **FR-025a** — Topic configurable score ranges → priority levels (High/Medium/Low/Not Applicable)
- **FR-025b** — Auto-include High+Medium; Low optional; Not Applicable excluded
- **FR-025c** — Surface SWOT + ODSR as interpretive context, not in suggestion logic
- **FR-026** — Collaborative adjustment: add excluded/optional, remove auto-included with justification
- **FR-027** — Persist with priority, manual overrides, audit record (who approved, when)
- **FR-018b** — Answer corrections do NOT retroactively regenerate plan (plan is a snapshot)
- **FR-045** — Plan approvals are audit-worthy events

## Dependencies & Integration Points

**Upstream (blockers):**
- Knowledge module (US3) — Topics with priority score ranges must exist
- Diagnostic `GetTopicScoreAggregationHandler` — already exists
- Mentor assignments in Tenant module — already exists

**Downstream (unblocks):**
- Mentoring Execution (US5) — scheduling engine reads from the approved plan
- Assignments (US5.b) — linked to subjects within plan topics

**Integration touch points:**
1. Subscribe to `DiagnosticCompletedEvent` (with `EvaluationStage == Initial`) to auto-trigger generation
2. Query Diagnostic's `GetTopicScoreAggregationHandler` for aggregated scores
3. Query Knowledge for each topic's priority ranges + SWOT/ODSR context (summary may be computed from Diagnostic responses, TBD)
4. Emit `MentoringPlanApprovedEvent` for Notification (Mentor + Entrepreneur) + downstream scheduling
5. Register in MediatR `AuditingBehavior` for plan-approval audit events (FR-045)

## Scope for the first spec

**In scope:**
- Domain: `MentoringPlan` aggregate (root) + `PlanTopic` entity
- Priority mapping logic: cumulative score → priority level via topic's score ranges
- Generation handler: triggered by `DiagnosticCompletedEvent` OR by explicit `GenerateSuggestedPlanCommand`
- Collaborative adjustment commands: `AddPlanTopic`, `RemovePlanTopic`, `SetPlanTopicJustification`, `ReorderPlanTopics`
- Approval command: `ApproveMentoringPlan` with audit stamp
- UI: mentor + entrepreneur review/adjust/approve view in `Mentoory.Web/Areas/Coordination/` or a new `Mentoring` area
- Plan status lifecycle: Draft → Adjusting → Approved → (future: Completed)

**Out of scope (first spec):**
- Plan re-generation after answer correction (FR-018b: plan is snapshot)
- Multi-mentor consensus (any mentor's approval finalizes — confirm in brainstorm)
- Plan versioning (snapshot only)

## Known Architecture Constraints

- Read model for plan view should use `AsNoTracking()` (CLAUDE.md code-review rule)
- Plan aggregate must emit domain events for adjustments (for audit + cross-module)
- External IDs on `MentoringPlan` and `PlanTopic` for URL routing
- Tenant scoping: `MentoringPlan.IncubatorId` + `ProjectId`

## Open Design Questions (for the focused brainstorm)

1. **Trigger semantics:** Is plan auto-generated immediately on diagnostic completion, or is the `Generate` action a manual mentor click? Spec implies auto-generation with human adjustment afterward.
2. **SWOT / ODSR summary computation:** these are per-answer-option classifications (Diagnostic side). How do we roll up to a per-topic summary? Candidate: count of each S/W/O/T among selected options; display the dominant class. Needs a decision.
3. **Who can generate vs adjust vs approve?** Per spec permission matrix, any assigned mentor can generate/adjust/approve. Confirm no restriction to lead mentor.
4. **Manual override justification — required or optional?** Spec says "with justification note." Make it required for removals; optional for additions?
5. **Plan status visibility:** does the entrepreneur see the plan in Draft status, or only once the mentor marks it "ready for review"? Friction vs transparency trade-off.
6. **Concurrent edits between mentor and entrepreneur:** spec edge case says "last-write-wins with conflict notification or optimistic concurrency." Pick one approach.
7. **Plan snapshot retention:** if plan must be a snapshot (immune to answer corrections), should we snapshot the topic priority ranges too — in case Knowledge-side ranges change? Likely yes, store as copy at plan creation time.
8. **Low-priority topics UX:** presented as optional checkboxes; default unchecked? Default checked?

## Success Criteria (scoped to Mentoring Plan)

- SC-003: Mentor + Entrepreneur can review and finalize a plan in a single session (<45 min)
- Auto-generated plan accurately reflects FR-025b rules (High+Medium in, Low optional, N/A excluded)
- Plan approval emits an auditable event (FR-045)
- Plan view shows cumulative score, priority level, SWOT + ODSR context, justification notes (FR-025c + FR-027)

## Suggested Next Steps

1. Verify Knowledge module has shipped with topic priority-range fields populated
2. Open `/spex:brainstorm` with this seed + `07-knowledge-module.md` outcomes as context
3. Resolve open questions 1–8
4. `/speckit-specify`

## Open Threads

- SWOT/ODSR summary algorithm (most frequent class? weighted by score?)
- Plan snapshot — does it copy topic priority ranges too?
- Concurrency policy for mentor/entrepreneur co-editing
- Low-priority topic default selection state
