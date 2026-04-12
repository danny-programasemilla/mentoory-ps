# API Contracts: Cascade Context Selector

**Feature**: 008-context-selector-ux  
**Date**: 2026-04-11

## Endpoints

### GET /api/context/roles

Returns the authenticated user's distinct roles with Spanish display names.

**Authorization**: `[Authorize]`  
**Response**: `200 OK`

```json
[
  { "role": "GlobalAdmin", "displayName": "Administrador Global" },
  { "role": "Mentor", "displayName": "Mentor" }
]
```

**Error responses**:
- `401 Unauthorized` — no valid session

---

### GET /api/context/incubators?role={role}

Returns incubators available for the given role. For GlobalAdmin, returns ALL active incubators.

**Authorization**: `[Authorize]`  
**Query parameters**:
- `role` (string, required) — role name from `Roles.All` constants

**Response**: `200 OK`

```json
[
  { "id": 1, "name": "Incubadora TechStart", "roleAssignmentExternalId": "a1b2c3d4-..." },
  { "id": 2, "name": "Incubadora Innovación", "roleAssignmentExternalId": "e5f6g7h8-..." }
]
```

**Error responses**:
- `401 Unauthorized` — no valid session
- `400 Bad Request` — invalid role parameter

**Notes**:
- Results are ordered by incubator name (ascending)
- For GlobalAdmin: `roleAssignmentExternalId` is the GlobalAdmin's base assignment (same for all entries)
- For other roles: `roleAssignmentExternalId` is the specific assignment for that (user, role, incubator) combination

---

### GET /api/context/projects?role={role}&incubatorId={id}

Returns projects available for the given role+incubator combination.

**Authorization**: `[Authorize]`  
**Query parameters**:
- `role` (string, required) — role name
- `incubatorId` (long, required) — incubator ID

**Response**: `200 OK`

```json
[
  { "id": 10, "name": "Proyecto Alpha", "roleAssignmentExternalId": "x9y0z1a2-..." },
  { "id": 11, "name": "Proyecto Beta", "roleAssignmentExternalId": "b3c4d5e6-..." }
]
```

Empty array `[]` when no projects exist for the combination.

**Error responses**:
- `401 Unauthorized` — no valid session
- `400 Bad Request` — invalid parameters

**Notes**:
- Results are ordered by project name (ascending)
- For GlobalAdmin: returns ALL active projects under the incubator
- For other roles: returns only projects where the user has an active assignment with the given role

---

### POST /api/context/switch (Extended)

Switches the active context. Extended to accept optional GlobalAdmin override fields.

**Authorization**: `[Authorize]`, `[ValidateAntiForgeryToken]`  
**Request body**: `application/json`

```json
{
  "roleAssignmentExternalId": "a1b2c3d4-...",
  "incubatorId": 1,
  "incubatorName": "Incubadora TechStart",
  "projectId": 10,
  "projectName": "Proyecto Alpha"
}
```

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| roleAssignmentExternalId | Guid | Yes | The role assignment to activate |
| incubatorId | long? | No | Override incubator (GlobalAdmin only) |
| incubatorName | string? | No | Override incubator name (GlobalAdmin only) |
| projectId | long? | No | Override project (GlobalAdmin only) |
| projectName | string? | No | Override project name (GlobalAdmin only) |

**Response**: `200 OK`
```json
{ "message": "Contexto actualizado exitosamente." }
```

**Error responses**:
- `401 Unauthorized` — `{ "message": "Sesión inválida." }`
- `400 Bad Request` — `{ "message": "No se pudo cambiar el contexto." }`

**Notes**:
- Override fields are silently ignored if the resolved role is not GlobalAdmin
- Anti-forgery token must be sent via `RequestVerificationToken` header

## Request/Response Conventions

- All endpoints return JSON with camelCase property names (ASP.NET Core default)
- Anti-forgery: POST endpoints require `RequestVerificationToken` header (retrieved from `[name="__RequestVerificationToken"]` hidden input)
- Error responses use `{ "message": "..." }` shape for consistency with existing endpoints
