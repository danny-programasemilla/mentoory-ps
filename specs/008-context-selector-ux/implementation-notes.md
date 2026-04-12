# Implementation Notes: Cascading Context Selector UX

## Design Decisions

### Decision: Cascading Dropdowns vs. Card Grid
- Chose cascading dropdowns (Role -> Incubator -> Project)
- Rationale: Card grid creates combinatorial explosion — N roles x M incubators x P projects = many cards. Cascading narrows progressively
- Rejected card grid: Hard to scan, no filtering, overwhelming for GlobalAdmin with many incubators

### Decision: Server-Side AJAX vs. Client-Side Filtering
- Chose server-side AJAX for cascade population
- Rationale: User preference — keeps data loading on server, avoids shipping full context tree to client
- Trade-off: Slightly slower interaction (network round-trip per dropdown change) but cleaner separation of concerns
- Note: Data volume is small so 500ms target is easily achievable

### Decision: Bootstrap Modal vs. Offcanvas for Top-Bar Switcher
- Chose Bootstrap 5 modal
- Rationale: Modals are a well-understood pattern already used in the project, less disruptive than offcanvas for a focused task
- Rejected offcanvas: Would work but may feel unfamiliar in this application's UI patterns

### Decision: Shared Partial View
- Both full page and modal render the same _ContextSelector.cshtml partial
- Rationale: Avoids duplication of dropdown markup, cascade JS wiring, and confirm button logic
- The partial needs to work in both form-POST (full page) and AJAX (modal) modes

### Decision: Auto-Select Single Options as Read-Only
- When a dropdown has exactly one option, it auto-selects and shows as disabled
- Rationale: User sees what was chosen (transparency) without wasting clicks
- Alternative considered: Skip the dropdown entirely — rejected because user loses visibility into what was auto-selected

## Key Existing Code

- `ContextController.cs` — current controller with GET/POST Select and AJAX Switch
- `Select.cshtml` — current card-based view (to be replaced)
- `_TopBar.cshtml` — top-bar with "Cambiar contexto" link (to gain modal trigger)
- `context-switcher.js` — existing AJAX switch logic with unsaved-changes check and area-role mapping
- `GetUserContextsQuery` — fetches user's role assignments
- `ListIncubatorContextOptionsQuery` — fetches all incubators+projects for GlobalAdmin
- `SetActiveContextCommand` — validates and activates a role assignment

## API Design Notes

Two new GET endpoints needed for cascade population:
- `GET /api/context/incubators?role={role}` — for GlobalAdmin returns all active incubators; for other roles returns only incubators from user's role assignments matching that role
- `GET /api/context/projects?role={role}&incubatorId={id}` — similar scoping logic

The existing `POST /api/context/switch` needs extension to accept optional incubator/project override parameters (for GlobalAdmin modal flow where the selected incubator/project differs from the role assignment's).
