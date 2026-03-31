# Web Endpoint Contracts: Mentoory Platform

**Branch**: `001-mentory-platform-core` | **Date**: 2026-03-31

All endpoints return Razor views (HTML) unless noted as JSON. AJAX endpoints return JSON.
All authenticated endpoints require a valid session cookie. Tenant-scoped endpoints require an active context.

---

## Identity Area (Unauthenticated)

| Method | Route | Controller.Action | Auth | Description |
|--------|-------|-------------------|------|-------------|
| GET | `/Identity/Login` | Login.Index | No | Login form |
| POST | `/Identity/Login` | Login.Index | No | Validate credentials, create session |
| GET | `/Identity/Register` | Register.Index | No | Registration form |
| POST | `/Identity/Register` | Register.Index | No | Create user + send verification email |
| GET | `/Identity/VerifyEmail?token={token}` | VerifyEmail.Index | No | Verify email token |
| GET | `/Identity/ForgotPassword` | ForgotPassword.Index | No | Password reset request form |
| POST | `/Identity/ForgotPassword` | ForgotPassword.Index | No | Send password reset email |
| GET | `/Identity/ResetPassword?token={token}` | ResetPassword.Index | No | Password reset form |
| POST | `/Identity/ResetPassword` | ResetPassword.Index | No | Apply new password |
| POST | `/Identity/Logout` | Logout.Index | Yes | Invalidate session, clear cookie |

## Context Selection (Authenticated, pre-context)

| Method | Route | Controller.Action | Auth | Description |
|--------|-------|-------------------|------|-------------|
| GET | `/Context/Select` | Context.Select | Yes | Show available contexts |
| POST | `/Context/Select` | Context.Select | Yes | Set active context in session |
| POST | `/api/context/switch` | Context.Switch (JSON) | Yes | AJAX context switch |

## Platform Area (GlobalAdmin)

| Method | Route | Controller.Action | Roles | Description |
|--------|-------|-------------------|-------|-------------|
| GET | `/Platform/Incubators` | Incubators.Index | GlobalAdmin | Incubator list (DataTable) |
| POST | `/Platform/Incubators/Data` | Incubators.Data (JSON) | GlobalAdmin | DataTable server-side data |
| GET | `/Platform/Incubators/Create` | Incubators.Create | GlobalAdmin | Create form |
| POST | `/Platform/Incubators/Create` | Incubators.Create | GlobalAdmin | Submit new incubator |
| GET | `/Platform/Incubators/{externalId}` | Incubators.Details | GlobalAdmin | Detail view |
| GET | `/Platform/Incubators/{externalId}/Edit` | Incubators.Edit | GlobalAdmin | Edit form |
| POST | `/Platform/Incubators/{externalId}/Edit` | Incubators.Edit | GlobalAdmin | Submit changes |
| GET | `/Platform/Subscriptions` | Subscriptions.Index | GlobalAdmin | Subscription plan list |
| POST | `/Platform/Subscriptions/Data` | Subscriptions.Data (JSON) | GlobalAdmin | DataTable data |
| GET | `/Platform/Subscriptions/Create` | Subscriptions.Create | GlobalAdmin | Create plan form |
| POST | `/Platform/Subscriptions/Create` | Subscriptions.Create | GlobalAdmin | Submit plan |
| GET | `/Platform/Subscriptions/{externalId}` | Subscriptions.Details | GlobalAdmin | Plan details + features |
| POST | `/Platform/Subscriptions/{externalId}/AddFeature` | Subscriptions.AddFeature | GlobalAdmin | Add feature to plan |
| POST | `/Platform/Incubators/{externalId}/AssignPlan` | Incubators.AssignPlan | GlobalAdmin | Assign subscription to incubator |
| POST | `/Platform/Incubators/{externalId}/ApplyOverride` | Incubators.ApplyOverride | GlobalAdmin | Apply positive override |
| GET | `/Platform/Templates/Diagnostics` | Templates.Diagnostics | GlobalAdmin | Global diagnostic templates |
| GET | `/Platform/Templates/Knowledge` | Templates.Knowledge | GlobalAdmin | Global knowledge templates |
| GET | `/Platform/Users` | Users.Index | GlobalAdmin | All platform users |
| POST | `/Platform/Users/Data` | Users.Data (JSON) | GlobalAdmin | DataTable data |

## Administration Area (IncubatorAdmin)

| Method | Route | Controller.Action | Roles | Description |
|--------|-------|-------------------|-------|-------------|
| GET | `/Administration/Dashboard` | Dashboard.Index | IncubatorAdmin | Incubator overview |
| GET | `/Administration/Projects` | Projects.Index | IncubatorAdmin | Project list |
| POST | `/Administration/Projects/Data` | Projects.Data (JSON) | IncubatorAdmin | DataTable data |
| GET | `/Administration/Projects/Create` | Projects.Create | IncubatorAdmin | Create project form |
| POST | `/Administration/Projects/Create` | Projects.Create | IncubatorAdmin | Submit new project |
| GET | `/Administration/Projects/{externalId}` | Projects.Details | IncubatorAdmin | Project detail |
| GET | `/Administration/Users` | Users.Index | IncubatorAdmin | Incubator users |
| POST | `/Administration/Users/Data` | Users.Data (JSON) | IncubatorAdmin | DataTable data |
| GET | `/Administration/Users/Enroll` | Users.Enroll | IncubatorAdmin | Administrative enrollment form |
| POST | `/Administration/Users/Enroll` | Users.Enroll | IncubatorAdmin | Submit enrollment |

## Coordination Area (ProjectCoordinator)

| Method | Route | Controller.Action | Roles | Description |
|--------|-------|-------------------|-------|-------------|
| GET | `/Coordination/Dashboard` | Dashboard.Index | ProjectCoordinator | Project overview |
| GET | `/Coordination/Lifecycle` | Lifecycle.Index | ProjectCoordinator | Stage management |
| POST | `/Coordination/Lifecycle/Advance` | Lifecycle.Advance | ProjectCoordinator | Advance project stage |
| GET | `/Coordination/Participants` | Participants.Index | ProjectCoordinator | Participant list |
| POST | `/Coordination/Participants/Enroll` | Participants.Enroll | ProjectCoordinator | Enroll participant |
| POST | `/Coordination/Participants/AssignMentor` | Participants.AssignMentor | ProjectCoordinator | Assign mentor |
| GET | `/Coordination/Diagnostics` | Diagnostics.Index | ProjectCoordinator | Diagnostic forms |
| GET | `/Coordination/Diagnostics/Clone` | Diagnostics.Clone | ProjectCoordinator | Clone template form |
| POST | `/Coordination/Diagnostics/Clone` | Diagnostics.Clone | ProjectCoordinator | Execute clone |
| GET | `/Coordination/Diagnostics/{externalId}` | Diagnostics.Details | ProjectCoordinator | Form detail/customize |
| GET | `/Coordination/Knowledge` | Knowledge.Index | ProjectCoordinator | Knowledge structure |
| GET | `/Coordination/Knowledge/Clone` | Knowledge.Clone | ProjectCoordinator | Clone template |
| POST | `/Coordination/Knowledge/Clone` | Knowledge.Clone | ProjectCoordinator | Execute clone |
| GET | `/Coordination/Knowledge/{externalId}` | Knowledge.Details | ProjectCoordinator | Structure tree view |

## Mentoring Area (Mentor)

| Method | Route | Controller.Action | Roles | Description |
|--------|-------|-------------------|-------|-------------|
| GET | `/Mentoring/Dashboard` | Dashboard.Index | Mentor | Mentor overview |
| GET | `/Mentoring/Plans` | Plans.Index | Mentor | Mentoring plans list |
| GET | `/Mentoring/Plans/{externalId}` | Plans.Details | Mentor | Plan review/adjust |
| POST | `/Mentoring/Plans/{externalId}/AdjustTopic` | Plans.AdjustTopic (JSON) | Mentor | Include/exclude topic |
| POST | `/Mentoring/Plans/{externalId}/Approve` | Plans.Approve | Mentor | Approve plan |
| GET | `/Mentoring/Sessions` | Sessions.Index | Mentor | Session calendar |
| GET | `/Mentoring/Sessions/{externalId}` | Sessions.Details | Mentor | Session detail |
| POST | `/Mentoring/Sessions/{externalId}/Log` | Sessions.Log | Mentor | Log session notes |
| GET | `/Mentoring/Assignments` | Assignments.Index | Mentor | Assignment list |
| GET | `/Mentoring/Assignments/Create` | Assignments.Create | Mentor | Create assignment form |
| POST | `/Mentoring/Assignments/Create` | Assignments.Create | Mentor | Submit assignment |
| GET | `/Mentoring/Assignments/{externalId}` | Assignments.Details | Mentor | Assignment + submissions |
| POST | `/Mentoring/Assignments/{externalId}/Review` | Assignments.Review | Mentor | Submit review |

## Participant Area (Entrepreneur)

| Method | Route | Controller.Action | Roles | Description |
|--------|-------|-------------------|-------|-------------|
| GET | `/Participant/Dashboard` | Dashboard.Index | Entrepreneur | Entrepreneur overview |
| GET | `/Participant/Diagnostic` | Diagnostic.Index | Entrepreneur | Current diagnostic form |
| POST | `/Participant/Diagnostic/Submit` | Diagnostic.Submit | Entrepreneur | Submit responses |
| GET | `/Participant/Plan` | Plan.Index | Entrepreneur | View mentoring plan |
| GET | `/Participant/Sessions` | Sessions.Index | Entrepreneur | Upcoming sessions |
| GET | `/Participant/Assignments` | Assignments.Index | Entrepreneur | Assignments list |
| GET | `/Participant/Assignments/{externalId}` | Assignments.Details | Entrepreneur | Assignment detail |
| POST | `/Participant/Assignments/{externalId}/Submit` | Assignments.Submit | Entrepreneur | Submit work |

## Sponsor Area (Sponsor)

| Method | Route | Controller.Action | Roles | Description |
|--------|-------|-------------------|-------|-------------|
| GET | `/Sponsor/Dashboard` | Dashboard.Index | Sponsor | Read-only project overview |
| GET | `/Sponsor/Progress` | Progress.Index | Sponsor | Progress metrics |

---

## Shared AJAX Patterns

### DataTable Server-Side Request (POST)

```json
{
  "draw": 1,
  "start": 0,
  "length": 25,
  "search": { "value": "search term" },
  "order": [{ "column": 0, "dir": "asc" }],
  "filters": {
    "status": "active",
    "dateFrom": "2026-01-01"
  }
}
```

### DataTable Server-Side Response

```json
{
  "draw": 1,
  "recordsTotal": 150,
  "recordsFiltered": 42,
  "data": [
    { "externalId": "...", "name": "...", "status": "Activo", "actions": "<html>" }
  ]
}
```

### Toast Notification (set via TempData)

```csharp
// Controller sets toast via extension method
this.SetSuccessToast("Incubadora creada exitosamente");
this.SetErrorToast("Error al crear la incubadora");
```

### Error Response (AJAX)

```json
{
  "message": "Error de validación",
  "errors": {
    "Email": ["El correo electrónico ya está registrado"],
    "NationalId": ["El número de identificación ya existe en este país"]
  }
}
```
