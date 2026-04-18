# Brainstorm Seed: Mentoring Execution — Scheduling, Sessions & Assignments (US5)

**Date:** 2026-04-18
**Status:** parked
**Spec:** —
**Parent roadmap:** [06-platform-roadmap-gap-analysis.md](./06-platform-roadmap-gap-analysis.md) (Phase C, hot streams)
**Blocked by:** [08-mentoring-plan.md](./08-mentoring-plan.md) must complete first
**Depends on:** [07-knowledge-module.md](./07-knowledge-module.md) (Subject duration, resources)

> This is a **seed document**. When starting the Execution stream, invoke `/spex:brainstorm` with this file as context. Consider **splitting into two specs**: scheduling+sessions, and assignments. They have different risk profiles.

---

## Problem Framing

Execution is where the value is actually delivered. Given an approved mentoring plan, the system:
1. Generates a calendar of proposed sessions (scheduling engine)
2. Lets any assigned mentor conduct sessions flexibly — cover topics in any order, log notes + decisions
3. Supports assignments: mentor creates, entrepreneur submits, mentor reviews/feedbacks

Today: module is an empty scaffold. No scheduling algorithm, no sessions, no assignments, no UI.

## Current State

- `Mentoory.Mentoring.Domain` / `.Application` / `.Infrastructure` empty
- `Mentoory.Mentoring.Tests` empty
- Mentor assignments exist in Tenant
- No cross-reference between an approved plan and any execution primitive

## Key FRs involved

- **FR-028** — Session calendar generation from subject duration, sessions/week, hours/session
- **FR-029** — Flexible, non-linear session execution — mentor can cover topics out of order
- **FR-030** — Session logs: notes, topics covered, decisions — timestamps + participants
- **FR-031** — Assignments linked to subjects; submission + review + feedback workflow
- **FR-057** — Any assigned mentor can conduct sessions (many-to-many)
- **FR-059** — Lead mentor flag has no permission difference (any assigned mentor can act)

## Dependencies & Integration Points

**Upstream (blockers):**
- Approved `MentoringPlan` (from US4)
- Knowledge Subjects with duration estimates (from US3)
- Mentor assignments (already in Tenant)

**Downstream (unblocks):**
- Notification: session reminders, assignment-due reminders (FR-040–044)
- Final Evaluation stage eligibility (Project advances to Final Evaluation once plan topics covered — TBD)

**Integration touch points:**
1. Read approved plan + plan topics + linked subjects (with durations) to generate calendar
2. Emit `MentoringSessionScheduledEvent` / `AssignmentCreatedEvent` for Notification
3. Plan status update when all topics covered? (TBD)
4. Project lifecycle: advancing to Mentoring stage is prerequisite; advancing to Final Evaluation is the natural exit

## Scope suggestion — **split into two specs**

### Spec A: Scheduling + Sessions (FR-028, 029, 030)
- Session calendar generation algorithm
- Session scheduling UI + manual adjustment
- Session conducting UI: select topics covered dynamically
- Session logging: notes, decisions, conducting mentor, timestamp
- Session status: Scheduled → In Progress → Completed → Canceled

### Spec B: Assignments (FR-031)
- `Assignment` entity linked to Subject + Project
- Creation UI (any assigned mentor)
- Submission UI (entrepreneur) with file/text content
- Review UI (any assigned mentor): feedback + approve / request revisions
- Notification integration: "assignment created", "submission received", "feedback ready"

Splitting reduces coordination risk since assignments don't block scheduling.

**Cross-referencing convention:** when Spec A (Scheduling+Sessions) and Spec B (Assignments) are created via `/speckit-specify`, each must list the other in the dependency table of its `brainstorm/00-overview.md` row + an explicit "Related specs" section in its own spec.md, so future planning/implementation agents generate consistent cross-links between the two.

## Open Design Questions (scheduling engine)

1. **Algorithm type:** rule-based distribution (round-robin across weeks) vs constraint solver (avoid mentor conflicts, respect time-of-day). Given SC-004 (<5s response), rule-based is pragmatic for the first spec.
2. **Input data shape:**
   - Subject duration (minutes) — from Knowledge
   - Sessions/week (int) — mentor+entrepreneur input
   - Hours/session (decimal) — mentor+entrepreneur input
   - Start date — mentor+entrepreneur input
   - Time slots / preferred days — in or out of scope for first spec?
3. **Adjustment policy:** once a calendar is generated, can it be re-generated? Can individual sessions be moved? Bulk shift?
4. **Mentor calendar conflicts:** if a mentor is assigned to multiple entrepreneurs, do we avoid overlaps? Requires cross-plan view. Likely out of scope for v1.
5. **Holidays / absence:** hard-coded skip list? Integrated with a calendar service? Out of scope for v1.
6. **What happens when subjects covered exceeds planned capacity?** Truncate the calendar, or extend?

## Open Design Questions (sessions)

7. **Flexible topic coverage:** mentor marks which plan topics were covered during the session. Persisted where? `SessionTopicsCovered` junction. Should this drive plan completion status?
8. **Session notes format:** plain text, markdown, rich text? Constitution implies simplicity; plain text or markdown is enough.
9. **Decisions log:** is this a structured list or free-form notes? Spec says "decisions made" suggesting structure — per-decision timestamp + who made it.
10. **Multi-mentor attendance:** does a session have one "conducting mentor" (current spec implies yes) or can multiple be attendees?
11. **Session cancellation edge case:** spec notes canceled sessions should not trigger stale reminders. Cancellation must propagate to notification outbox.

## Open Design Questions (assignments)

12. **Submission content types:** text only, file attachment, both? If files, blob storage decisions surface.
13. **Single vs multiple submissions:** can an entrepreneur re-submit after "request revisions"? Likely yes; keep submission history.
14. **Feedback required for reject?** Likely yes — no silent rejection.
15. **Assignment deadline:** hard (past-deadline submissions flagged/blocked?) or soft (late marker but accepted)?
16. **Visibility across mentors:** any assigned mentor can see the assignment's full history — consistent with FR-057/059.

## Known Architecture Constraints

- Scheduling + sessions touch tenant + project context heavily — pipeline behavior must enforce tenant scoping
- Notification integration: outbox writes in same transaction as session scheduling (FR-040 dedup concerns)
- Authorization: any assigned mentor acts; controller + handler enforce membership in `MentorAssignments`

## Success Criteria (scoped to Execution)

- SC-004: Session calendar generated within 5 seconds
- SC-007: Session reminders delivered within 2 minutes of scheduled trigger
- SC-008: Zero duplicate notifications per event per recipient

## Suggested Next Steps

1. Verify US4 Mentoring Plan has shipped with approved plans being persisted
2. Open `/spex:brainstorm` specifically for **Scheduling + Sessions** (Spec A)
3. Decide on 1–11 questions above
4. `/speckit-specify` for Spec A
5. In parallel (once Spec A is drafted), brainstorm **Assignments** (Spec B)

## Open Threads

- Split into Spec A (Scheduling+Sessions) + Spec B (Assignments) vs single spec
- Rule-based vs constraint scheduling engine
- Blob storage for assignment file submissions
- Session cancellation propagation to notification outbox
