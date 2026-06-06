# Phase 0 Research: Single Fixed-Cost VM Deployment

**Feature**: 022-vm-deployment
**Date**: 2026-06-06

This document resolves the open technical questions for porting the `bds-ps/deploy/vm`
methodology to Mentoory. Each item: **Decision / Rationale / Alternatives**.

---

## R1. Standalone startup without the Aspire AppHost (FR-010) — the headline risk

**Decision**: No application code change is required. `Mentoory.Web` already starts from
standard ASP.NET Core configuration (`appsettings.json` + `appsettings.{Environment}.json`
+ environment variables). The container supplies configuration via environment variables.

**Rationale**: A repo-wide search shows `AspireAppsettings` and `APPSETTINGS_HASH` are
**only set** by `Mentoory.Aspire.AppHost/AppHost.cs` (lines 24–25) and **never read** by
any application code. `AppsettingsLoader` serializes appsettings (minus `ConnectionStrings`
and `Logging`) purely so the AppHost can force a resource diff (`// <— forces a spec change`).
`Program.cs` reads everything via `builder.Configuration` (MediatR key via `MediatR:LicenseKey`,
DB via `GetConnectionString("DefaultConnection")`, audit options via section). Therefore a
standalone container that sets `ConnectionStrings__DefaultConnection` and `MediatR__LicenseKey`
(plus standard ASP.NET env) boots identically to the Aspire run mode.

**Alternatives considered**: (a) Add an env-driven configuration shim to read the base64
`AspireAppsettings` blob — rejected as unnecessary (nothing consumes it). (b) Keep the AppHost
in the container — rejected; the AppHost is an orchestrator, not a runtime dependency of the web app.

**Plan impact**: FR-010 is satisfied by configuration wiring alone. Spec's edge case
"Application requires AppHost-injected configuration" resolves to "it does not."

---

## R2. Reverse proxy, TLS, and forwarded headers behind Caddy

**Decision**: Caddy terminates TLS and reverse-proxies plain HTTP to the webapp on an internal
port (8080). The webapp container sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` and listens
HTTP-only (`HTTP_PORTS=8080`, no HTTPS port). `Caddyfile` uses `{$APP_DOMAIN}` and `{$ACME_EMAIL}`
so the domain/email come from `.env`.

**Rationale**: Matches the reference exactly. Two subtleties verified against `Program.cs`:
- `app.UseHttpsRedirection()` is present, but with **no HTTPS port configured** it cannot
  determine a redirect target and becomes a no-op (logs a warning at startup). So there is
  **no redirect loop** behind Caddy. Confirmed by the reference running the same way.
- `options.Cookie.SecurePolicy = CookieSecurePolicy.Always` is fine: the browser↔Caddy leg is
  HTTPS, so Secure cookies are delivered. `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` ensures the
  app sees `X-Forwarded-Proto=https` for correct scheme in generated URLs.

**Alternatives considered**: Terminating TLS in the app (rejected — manual cert management, the
whole point of Caddy is auto-TLS); using Nginx + certbot (rejected — Caddy is simpler, reference parity).

**Risk note**: ASP.NET's env-enabled ForwardedHeaders trusts loopback proxies by default; Caddy
is a separate container on the compose network. In practice the reference works with just the env
var (HttpsRedirection no-op + Secure cookies cover the functional needs). If absolute-URL scheme
problems surface, the fallback is to configure `ForwardedHeadersOptions` to clear
`KnownNetworks`/`KnownProxies`. Documented as a known fallback, not a required change.

---

## R3. Container image for `Mentoory.Web` (FR-009)

**Decision**: New multi-stage Dockerfile at `Mentoory.Web/Dockerfile`, build context = repo root.
- Build stage: `mcr.microsoft.com/dotnet/sdk:10.0` → `dotnet publish Mentoory.Web/Mentoory.Web.csproj -c Release`.
- Runtime stage: `mcr.microsoft.com/dotnet/aspnet:10.0`, copy publish output, `ENTRYPOINT dotnet Mentoory.Web.dll`, expose 8080.
- Copy the central-build files first for restore-layer caching: `global.json`, `Directory.Build.props`,
  `Directory.Build.targets`, `Directory.Packages.props`, then the project tree, then publish.

**Rationale**: `Mentoory.Web.csproj` transitively references the ~19 module Application/Infrastructure
projects + `ServiceDefaults` (Domain projects come transitively). `dotnet publish` on the Web csproj
restores and compiles exactly that closure — no need to build the full solution. Central Package
Management (`Directory.Packages.props`) and `global.json` (SDK 10.0.0, `allowPrerelease: true`) must be
present in the build context. **No `nuget.config` exists** → default nuget.org feed, which hosts the
.NET 10 prerelease packages. `MapStaticAssets()` + `.WithStaticAssets()` require the static-web-assets
manifest that `dotnet publish` emits — hence publish (not just build), and the runtime image must contain
the full publish output including `wwwroot`.

**Alternatives considered**: `dotnet build` + copy bin (rejected — misses publish-time static asset
manifest and trimming of build artifacts); building the whole `.sln` (rejected — pulls test projects and
unused modules, slower, larger). A per-project `COPY *.csproj` restore-cache optimization is possible but
optional given build happens on the VM and correctness > marginal cache speed.

**A `.dockerignore` (repo root) is required** to keep the build context small and correct: exclude
`**/bin`, `**/obj`, `.git`, `**/node_modules`, `TestResults`, `specs`, `brainstorm`, `deploy/vm/.env`,
`deploy/vm/backups`, `.localstorage`.

---

## R4. Health checks and container liveness

**Decision**: Do **not** rely on `/health` in the webapp container. The compose `mssql` service keeps a
SQL healthcheck + `depends_on: condition: service_healthy`; the webapp service gets a lightweight
container healthcheck hitting the app's HTTP port root (or omitted, matching the reference which had none).

**Rationale**: `ServiceDefaults.MapDefaultEndpoints()` maps `/health` and `/alive` **only in Development**
(verified). In Production those endpoints are unmapped, so a `/health` healthcheck would always fail. The
deploy script's readiness gate is the SQL healthcheck (as in the reference), which is what actually gates
schema publish + app start.

**Alternatives considered**: Mapping health endpoints in Production (rejected — security note in
ServiceDefaults, and out of scope); custom health endpoint (rejected — unnecessary for a single-VM deploy).

---

## R5. Database: container, connectivity, and schema publish (FR-011, FR-016–018)

**Decision**:
- `mssql` = `mcr.microsoft.com/mssql/server:2022-latest`, Developer edition, bound to `127.0.0.1:1433`
  (loopback only), `MSSQL_MEMORY_LIMIT_MB` capped (~2048 on B2s), SA auth, healthchecked via `sqlcmd`.
- Webapp connects over the private compose network:
  `ConnectionStrings__DefaultConnection = "Server=mssql,1433;Database=MentooryDb;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True"`.
- Schema publish: build the dacpac from `Mentoory.Db/MentooryDb.sqlproj` (`Microsoft.Build.Sql/2.0.0`
  SDK, `dotnet build`), then `sqlpackage /Action:Publish` to `127.0.0.1:<tunnel-port>` over an SSH tunnel
  to the VM's loopback SQL, target DB `MentooryDb`. PostDeployment scripts are embedded in the dacpac
  automatically (the sqlproj declares `Script.PostDeployment.sql` which `:r`-includes the numbered seeds).

**Rationale**: Mirrors the reference's `publish-dacpac-vm.sh` and Mentoory's existing
`publish-mentoorydb.sh`/SSDT conventions (Constitution XI — no EF migrations). Loopback binding + SSH
tunnel keeps SQL off the public internet (SC-004). GlobalAdmin and reference data are seeded by the
PostDeployment scripts (`001.SeedRoles`, `002.SeedGlobalAdmin`, `003`, `016`, `017`, `018`) → first-login
admin credentials come from the dacpac, **not** from an app env var.

**Alternatives considered**: Exposing 1433 to a locked NSG range (rejected — violates "never public");
running schema via EF migrations (rejected — forbidden by constitution); SA-less SQL auth (rejected for a
self-contained dev/prod VM — SA in a loopback-only container is acceptable and matches the reference).

**Plan impact**: The spec's "admin sentinel credential" in FR-014 is **not required** — drop
`ADMIN_DEFAULT_PASSWORD` from `.env.example` (Mentoory has no such app setting; the reference's
`Admin__DefaultPassword` was FundingPlatform-specific).

---

## R6. Telemetry (Aspire dashboard) and OTLP export gating

**Decision**: On-demand `aspire-dashboard` service under a compose `debug` profile, loopback-bound,
`AUTHMODE=Unsecured`, in-memory limits set. The webapp sets `OTEL_EXPORTER_OTLP_ENDPOINT` from
`${OTEL_ENDPOINT:-}` (empty by default).

**Rationale**: `ServiceDefaults.AddOpenTelemetryExporters()` wires the OTLP exporter **only when**
`OTEL_EXPORTER_OTLP_ENDPOINT` is non-empty (verified). So empty = zero overhead/no export; setting it to
`http://aspire-dashboard:18889` + starting the debug profile streams telemetry. Identical to the reference.

**Alternatives considered**: Always-on dashboard (rejected — RAM cost on B2s); persistent telemetry store
(rejected — cost, the whole point is $0 in-memory).

---

## R7. Data protection key persistence (FR-013)

**Decision**: Mount a named volume at the data-protection keyring path
(`/root/.aspnet/DataProtection-Keys` for the aspnet image's default user, or the `$HOME/.aspnet/...`
of the configured user) so keys survive container recreation.

**Rationale**: Without persistence, every redeploy regenerates the keyring → all auth cookies
(`Mentoory.Session`) and antiforgery tokens invalidate (everyone logged out) and any DP-encrypted data
becomes unreadable. The reference solved this with a `dataprotection` volume; same approach. Exact in-image
path to be confirmed against the aspnet:10.0 image's running user during implementation (commonly `app`
user → `/home/app/.aspnet/DataProtection-Keys`).

**Alternatives considered**: Configuring `PersistKeysToFileSystem` + a known path in code (cleaner but a
code change — defer; the volume-mount approach needs no app change). Azure Key Vault DP provider (rejected —
adds cloud dependency, defeats the self-contained goal).

---

## R8. Object storage plumbing for the future (FR-025, FR-026)

**Decision**: Ship `provision-storage.sh` (creates `Standard_LRS` `StorageV2`, assigns the VM's
system-assigned managed identity `Storage Blob Data Contributor`, prints the blob endpoint) and wire
optional `STORAGE_PROVIDER`/`BLOB_CONNECTION` env on the webapp as **no-ops by default**. Document clearly:
provisioned, not yet consumed.

**Rationale**: Mentoory has **no** blob/attachment code today (verified — no `BlobServiceClient`,
`IFileStorage`, `Azure.Storage`, or attachment consumers). So unlike the reference (which needed
`Storage Blob Data Owner` for `GetAccessPolicy`), Mentoory needs no specific data role yet — `Contributor`
is a sufficient, least-surprise default for future use; revisit when an attachments feature lands.

**Alternatives considered**: Omit storage entirely (the user explicitly chose to include future-ready
plumbing); grant `Owner` like the reference (rejected — no consumer asserts container ACLs, so the
elevated role is unjustified now).

---

## R9. Provisioning, power schedule, backups, kill-switch (FR-001–006, 027–032)

**Decision**: Port the reference scripts 1:1 with Mentoory naming/defaults, all env-overridable:
- `provision-vm.sh`: RG (`rg-Mentoory-D`), VM (`vm-mentoory-dev`), `Standard_B2s`, `centralus`, Ubuntu 24.04,
  64 GB StandardSSD, Standard static IP, NSG (80/443 public, 22 → operator IP), `cloud-init.yaml` custom-data.
- `cloud-init.yaml`: Docker install, json-file log cap (10m×3), ufw (SSH/80/443).
- `provision-schedule.sh`: Automation runbook start (weekday 06:45) + DevTest auto-stop (19:00),
  `America/Costa_Rica`, manual start/stop/status/enable/disable.
- `backup.sh`: nightly `BACKUP DATABASE [MentooryDb]` (compressed) + `app_storage`/volume archive,
  7-day retention, commented off-VM blob upload.
- Kill-switch: README section only (budget alert → Action Group → runbook `az vm deallocate`).

**Rationale**: These are infra orchestration scripts with no Mentoory-specific logic beyond names and the
DB name (`MentooryDb` vs `fundingdb`) and compose project name (`mentoory`). Reference parity minimizes risk.

**Alternatives considered**: Reimplementing the power schedule with native Azure Automation schedules only
(the reference's hybrid runbook-start + DevTest-stop is already proven; keep it).

---

## R10. Deploy workflow (FR-019–024)

**Decision**: Port `deploy.sh` (dev-machine, idempotent): preflight SSH → `rsync -az --delete` repo to VM
(excludes `.git`, `**/bin`, `**/obj`, `**/node_modules`, `deploy/vm/.env`, `deploy/vm/backups`, `TestResults`,
`.localstorage`) → ensure `mssql` healthy → optional `--schema` (calls `publish-dacpac-vm.sh`) → build webapp
image **on the VM** → recreate changed containers → optional `--no-build`/`--logs`. Never touches the VM `.env`;
fails clearly if `.env` missing on first deploy.

**Rationale**: Build-on-VM keeps the dev machine thin and matches the reference; rsync excludes protect VM
state. Idempotency from `docker compose up -d --build` being a no-op when unchanged.

**Alternatives considered**: Building the image on the dev machine + pushing to a registry (rejected — adds
ACR cost + registry auth, defeats the no-extra-Azure-services goal); `git pull` on the VM (rejected — rsync
of the working tree handles uncommitted local state and is what the reference uses).

---

## R11. Base images / SDK availability

**Decision**: Use `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`
(preview-channel tags) and `mcr.microsoft.com/dotnet/aspire-dashboard:9.0` for the debug profile;
`mcr.microsoft.com/mssql/server:2022-latest` for the DB.

**Rationale**: `global.json` pins SDK 10.0.0 with `allowPrerelease: true`; the `10.0` image tag tracks the
matching preview SDK. The aspire-dashboard 9.0 image is forward-compatible as an OTLP receiver (reference
uses it). Pin exact digests later if reproducibility becomes a concern.

**Alternatives considered**: Building against a pinned preview tag (e.g. `10.0.100-preview`) — acceptable but
`10.0` is simpler and the build happens on the VM where the tag resolves at build time.

---

## Summary of plan-affecting deltas vs. the spec

1. **FR-010 needs no code change** — standalone startup works via env configuration (R1).
2. **Drop `ADMIN_DEFAULT_PASSWORD`** from `.env.example` — Mentoory seeds GlobalAdmin via PostDeployment;
   no app admin-password setting exists (R5).
3. **Storage role = `Storage Blob Data Contributor`** (not Owner) — no ACL-asserting consumer yet (R8).
4. **No `/health` healthcheck in Production** — readiness keys off SQL health (R4).
5. Everything else is faithful reference parity with Mentoory naming and `MentooryDb`.
