# Review Guide: Single Fixed-Cost VM Deployment

**Generated**: 2026-06-06 | **Spec**: [spec.md](spec.md)

## Why This Change

Mentoory's current Azure path (Aspire orchestration → Container Apps + Azure SQL) bills by usage, and Azure
has no native "stop at $X" — budgets only alert. For a low-traffic dev/early-prod deployment that is hard to
reason about and easy to overspend on. This feature adds a **self-contained single-VM deployment** whose
compute cost is **fixed** regardless of traffic (~$40/month baseline), and which removes the Log Analytics
ingestion bill by keeping logs/telemetry on the VM. It ports a methodology already proven in `bds-ps/deploy/vm`.

## What Changes

Adds a new `deploy/vm/` directory of operator scripts and a new `Mentoory.Web/Dockerfile` + repo-root
`.dockerignore`. Operators can provision one Azure Linux VM and run a Docker Compose stack (Caddy auto-TLS →
`Mentoory.Web` → SQL Server 2022) reachable over HTTPS at their own domain, with the `MentooryDb` schema
published from the existing SSDT dacpac. Day-to-day updates ship via one idempotent `deploy.sh`. Operational
tooling adds nightly backups, an off-hours power schedule (~half cost), an on-demand $0 in-memory Aspire
telemetry viewer, and future-ready Azure Blob plumbing. **No application code changes; no breaking changes.**
The existing Aspire AppHost remains for local dev and is simply unused by this deployment.

## How It Works

- **Container image** (`Mentoory.Web/Dockerfile`): multi-stage, build context = repo root, `dotnet publish`
  the Web project (which transitively pulls the ~19 module projects + ServiceDefaults), runtime on
  `aspnet:10.0`, HTTP 8080. `dotnet publish` (not build) is required for `MapStaticAssets()`.
- **Standalone startup**: research confirmed `AspireAppsettings`/`APPSETTINGS_HASH` are set by the AppHost
  but read by nothing — the app boots from standard config, so the container only needs env vars
  (`ConnectionStrings__DefaultConnection`, `MediatR__LicenseKey`). No code change.
- **Compose** (`docker-compose.yml`, project `mentoory`): `caddy` (auto-TLS, reverse proxy → `webapp:8080`),
  `webapp` (Production, forwarded headers, DataProtection volume so redeploys don't log everyone out),
  `mssql` (2022 Developer, loopback-only, memory-capped, healthchecked), and an opt-in `aspire-dashboard`
  under a `debug` profile (loopback-only, in-memory, $0).
- **Schema**: `publish-dacpac-vm.sh` builds `Mentoory.Db/MentooryDb.sqlproj` and publishes via `sqlpackage`
  over an SSH tunnel to the loopback-only SQL — never exposing 1433 publicly. PostDeployment seeds
  (incl. `002.SeedGlobalAdmin`) are embedded in the dacpac, so first-login admin comes from the DB.
- **Provision/deploy**: `provision-vm.sh` (RG + VM + NSG + static IP, SSH locked to operator IP),
  `cloud-init.yaml` (Docker + log cap + ufw), `deploy.sh` (rsync repo → VM, build image on the VM, recreate
  changed containers, never touches the VM `.env`). Ops: `backup.sh`, `provision-schedule.sh`,
  `provision-storage.sh` (future-use). Full detail in `plan.md`, `research.md`, and `contracts/`.

## When It Applies

**Applies when**:
- Deploying Mentoory to a single Azure VM for predictable fixed cost (dev or low-traffic prod).
- An operator has `az`/`ssh`/`rsync`/`sqlpackage`, an Azure subscription, and controls a DNS A record.

**Does not apply when**:
- High-availability / multi-region / autoscaling is required (single-VM by design — resilience traded for cost).
- The app needs object storage today (Blob is provisioned as future plumbing only; nothing consumes it yet).
- You want managed automatic SQL backups / managed certs — those become operator-owned here (backup.sh, Caddy).

## Key Decisions

1. **No application code change for standalone startup** — verified `AspireAppsettings`/`APPSETTINGS_HASH`
   are unread. Alternative (a config shim to read the base64 blob) was rejected as unnecessary.
2. **Build the image on the VM** (rsync + `docker compose build`) — alternative (build locally, push to ACR)
   rejected to avoid an extra Azure service + registry auth, keeping the stack self-contained.
3. **SQL in a loopback-only container + SSH-tunnel dacpac publish** — alternative (NSG-restricted public 1433)
   rejected: spec mandates SQL never public (SC-004). EF migrations rejected per Constitution XI (SSDT only).
4. **Storage role = `Storage Blob Data Contributor`, future-use** — the reference needed `Owner` for ACL
   asserts; Mentoory has no blob consumer, so the elevated role is unjustified now. Including storage at all
   was an explicit product choice (future-proofing).
5. **DataProtection keyring on a named volume** — prevents logout/antiforgery invalidation on every redeploy.
6. **Full parity with `bds-ps/deploy/vm`** minus FundingPlatform-specific config (no Syncfusion, Mailgun, AI,
   FundingAgreement, or `ADMIN_DEFAULT_PASSWORD`) — minimizes risk by reusing a proven methodology.

## Areas Needing Attention

- **Forwarded headers nuance** (research R2): the container sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`
  and listens HTTP-only, so `UseHttpsRedirection()` is a safe no-op behind Caddy. ASP.NET's env-enabled
  ForwardedHeaders trusts loopback proxies by default while Caddy is a separate container; the reference works
  with just the env var (Secure cookies + no redirect loop cover the functional needs). If absolute-URL scheme
  issues appear, the documented fallback is to clear `KnownNetworks`/`KnownProxies`. Worth a reviewer's eye.
- **Preview base images** (research R11): `sdk:10.0`/`aspnet:10.0` track .NET 10 preview; pin digests if
  reproducibility matters.
- **B2s memory headroom**: 4 GB is the floor for SQL + webapp; the telemetry dashboard pushes it. README
  documents the B2ms upgrade path — confirm the cap (`MSSQL_MEMORY_LIMIT_MB=2048`) is acceptable.
- **No automated tests**: this is infra scripting; validation is the manual `quickstart.md` runbook. Reviewers
  should sanity-check the scripts (ideally `shellcheck`, T020) since there's no CI gate on them yet.

## Open Questions

- Exact DataProtection keyring path inside `aspnet:10.0` (running user) — to confirm during T003/T007
  implementation (commonly `/home/app/.aspnet/DataProtection-Keys`). Not blocking; the volume mount target is
  adjusted to the image's actual user.
- Production domain/region/timezone values are operator-supplied via `.env`/env overrides (no default domain
  baked in) — intentional, not an open design question.

## Review Checklist

- [ ] Key decisions are justified
- [ ] Breaking changes are documented with migration guidance (none — additive; old path decommissioned by operator)
- [ ] Scope matches the stated boundaries (single-VM, no HA, storage future-use)
- [ ] Success criteria are achievable (SC-001…SC-010 each have a validation task)
- [ ] No unstated assumptions (AppHost-config dependency resolved; SQL never public; SSDT-only schema)
- [ ] Scripts are env-overridable with documented defaults and fail clearly (missing `.env`, SQL pw mismatch)

---

<!-- Code phase sections are appended below this line by the phase-manager command -->
