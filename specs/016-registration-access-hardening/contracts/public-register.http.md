# HTTP Contract: Public Self-Registration

**Feature**: 016-registration-access-hardening
**Endpoint**: `POST /Access/Register` (MVC action `Mentoory.Web.Areas.Access.Controllers.RegisterController.Index`)
**Audience**: Unauthenticated visitors (public)

This contract captures the response shape that the public self-registration endpoint MUST exhibit after this feature ships. The central invariants are **indistinguishable success-shaped responses** and a **single generic in-form failure**.

---

## Request

- **Method**: `POST`
- **Path**: `/Access/Register` (area `Access`, controller `Register`, action `Index`)
- **Attributes**: `[AllowAnonymous]`, `[ValidateAntiForgeryToken]`, `[EnableRateLimiting("registration")]`
- **Content-Type**: `application/x-www-form-urlencoded`

### Form body

| Field | Type | Constraints |
|-------|------|-------------|
| `Email` | string | Required; valid email format |
| `Country` | string | Required |
| `NationalId` | string | Required |
| `FirstName` | string | Required; max 100 chars |
| `LastName` | string | Required; max 100 chars |
| `Password` | string | Required; min 12 chars; at least one upper, lower, digit, special; must NOT contain Email or NationalId (per `PasswordIdentifyingDataRule`) |
| `__RequestVerificationToken` | string | Anti-forgery token; required |

---

## Response classes

The endpoint produces exactly three response classes. Callers MUST NOT be able to distinguish response class A from response class B.

### Class A — Success-shaped (fresh creation path)

Triggered when all validator rules pass AND the submitted (email, national-id) combination does not collide with an existing account.

- **Status**: `302 Found`
- **Location header**: `/Access/Register/Success`
- **Body**: short redirect body emitted by MVC (unchanged from today)

Follow-up GET `/Access/Register/Success` returns:

- **Status**: `200 OK`
- **Content-Type**: `text/html; charset=utf-8`
- **Body**: renders `Success.cshtml`. Visible content: "Revise su correo electrónico para confirmar su cuenta." + secondary line directing users who believe they already have an account toward password recovery. The secondary line is constant — it appears unconditionally, so its presence does not disclose the outcome.

Side effects: `User` is persisted; `EmailVerificationToken` is generated; verification email is queued via the existing token pipeline.

### Class B — Success-shaped (uniqueness-conflict path; oracle closure)

Triggered when all validator rules pass BUT the submitted email OR national-id collides with an existing account.

- **Status**: `302 Found`
- **Location header**: `/Access/Register/Success`
- **Body**: short redirect body

Follow-up GET: **byte-for-byte identical** to Class A. Same view, same HTML, same headers affecting rendering.

Side effects: none. No user is created, no verification email is sent, no other notification is emitted.

**Invariant (SC-016-01)**: An HTTP observer comparing two submissions that differ only in whether they are fresh or conflicting cannot tell them apart from the response.

### Class C — Generic failure (validator-rule failure)

Triggered when any validator rule fails — missing field, bad email format, weak password, password contains identifying data, any other rule.

- **Status**: `200 OK`
- **Content-Type**: `text/html; charset=utf-8`
- **Body**: re-rendered `Index.cshtml` with a single generic banner: `"No fue posible completar el registro. Revise los datos e intente nuevamente."`
- **No field-level error attribution is rendered.** No ModelState errors are surfaced to the view beyond the generic banner. The form values the user submitted ARE preserved in the fields (so they do not have to re-type).

**Invariant (SC-016-02)**: Two Class-C responses are byte-for-byte identical. A Class-C response is visibly distinct from Class A/B (status 200 vs 302, body = form re-render vs redirect).

---

## Logging contract

Every POST to this endpoint emits exactly one structured log entry at `LogLevel.Information`:

```
Public registration outcome. Email: {Email}, Outcome: {Outcome}, CorrelationId: {CorrelationId}, ClientIp: {ClientIp}
```

| `Outcome` value | When |
|------|------|
| `Success` | Class A (fresh creation) |
| `DuplicateEmail` | Class B with email conflict |
| `DuplicateNationalId` | Class B with national-id conflict |
| `ValidatorFailure:<propertyName>` | Class C; `<propertyName>` is the first ModelState key with an error, e.g. `ValidatorFailure:Password` |

`NationalId` is NEVER written to the log.

---

## Rate limiting

Per existing configuration (`registration` policy):

- **Production**: 3 requests / 15 minutes / IP
- **Development**: 1000 requests / 1 minute / IP

Unchanged by this feature.

---

## Anti-forgery

Unchanged. `[ValidateAntiForgeryToken]` stays in force; token issued by the GET `/Access/Register/Index` action.

---

## Explicit non-goals

- Timing-channel parity between Class A and Class B. The response body + status + headers are identical; wall-clock differences exist and are out of scope.
- Protection against TLS-layer attacks. Visible-output parity is the target; TLS-layer constant-time behaviour is not.
