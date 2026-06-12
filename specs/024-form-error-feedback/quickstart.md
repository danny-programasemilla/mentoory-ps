# Quickstart: verifying inline form-error feedback

**Feature**: 024-form-error-feedback

Razor views compile at build time (no runtime compilation), so **restart the app** after changes:

```bash
dotnet run --project Mentoory.Aspire.AppHost
```

Then hard-refresh (Ctrl+F5).

## Wave 1 — standard CRUD (US1)

1. Go to `https://localhost:7061/Platform/Incubators/Create`.
2. Submit with the name empty → the **Name** field shows a danger border, a soft red glow (persisting when focused), and an alert-circle icon inside on the right; one message appears beneath it. **No** summary block at the top.
3. Type a valid name → the error styling clears.
4. Force a non-field failure (e.g. create a duplicate / trigger the persistence error path) → a **red toast** appears with the page-level message.
5. Repeat the empty-submit check on each Wave-1 form: Incubators Edit, Projects Create, Users Enroll, Users RegisterInternal, Diagnostics Clone, Knowledge CreateTemplate. Each invalid field is highlighted; no top summary.

## Wave 2 — auth (US2)

1. Go to `/Access/Login`. Submit wrong credentials → a **red toast** shows the failure (not a top summary).
2. Submit with an empty field → inline field error treatment.
3. Repeat on ForgotPassword, ResetPassword, ChangePassword.

## Wave 3 — atypical (US3)

1. BatchUpload: submit without a file → field error treatment adapted for the file input (border + glow + message; no overlapping inside icon).
2. Participant Diagnostic: submit with a required question unanswered → that question shows an inline error consistent with the pattern.

## Cross-cutting checks

- **Build**: `dotnet build Mentoory.Web/Mentoory.Web.csproj` → 0 warnings, 0 errors.
- **Spanish**: all error copy is in Spanish.
- **A11y**: invalid fields expose `aria-invalid`; the toast region is announced (`aria-live`/`role="alert"`).
- **No duplication**: on every form, each error appears once (field only), never also in a top summary.
