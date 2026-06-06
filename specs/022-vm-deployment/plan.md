# Implementation Plan: Single Fixed-Cost VM Deployment

**Branch**: `022-vm-deployment` | **Date**: 2026-06-06 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/022-vm-deployment/spec.md`

## Summary

Add a self-contained, fixed-cost deployment path for Mentoory: one Azure Linux VM running a Docker
Compose stack (Caddy auto-TLS → `Mentoory.Web` container → SQL Server 2022 container), provisioned and
operated by a set of shell scripts under a new `deploy/vm/` directory, plus a new `Mentoory.Web/Dockerfile`
and a repo-root `.dockerignore`. The app runs standalone (no Aspire AppHost) purely from environment
configuration — research confirms `AspireAppsettings`/`APPSETTINGS_HASH` are set by the AppHost but never
read, so **no application code change is required**. Schema is published from the existing SSDT dacpac
(`Mentoory.Db`) over an SSH tunnel to the loopback-only SQL container. Operational tooling (idempotent
deploy, nightly backups, off-hours power schedule, on-demand in-memory Aspire dashboard, cost kill-switch
docs) ports the proven `bds-ps/deploy/vm` methodology with Mentoory naming and the `MentooryDb` database.

This feature is **deployment/infrastructure only** — it adds artifacts and a container image definition;
it does not change application domain logic, CQRS, DDD, or the database schema content (it only publishes
the existing schema).

## Technical Context

**Language/Version**: Bash scripts (POSIX/bash), Dockerfile, YAML (cloud-init, docker-compose); the
application being containerized is C# / .NET 10.0 (SDK 10.0.0, `allowPrerelease`).
**Primary Dependencies**: Docker Engine + Compose plugin (on the VM), Caddy 2 (auto-TLS), SQL Server 2022
(Developer), Azure CLI `az` (dev machine, provisioning), `sqlpackage` + .NET SDK (dev machine, dacpac),
`rsync` + OpenSSH (deploy + tunnel), `jq`/`yq` already used by spex tooling.
**Storage**: SQL Server data on a VM named volume; ASP.NET DataProtection keys on a named volume; optional
Azure Blob (`Standard_LRS` `StorageV2`) provisioned for future use (not consumed by current code).
**Testing**: Manual operator runbook validation (quickstart.md) — provision, deploy, reach over HTTPS,
schema publish, stop/start, backup. No automated test project is added (infra scripts; the existing app
test suites are unaffected). Optional `shellcheck` lint on the scripts.
**Target Platform**: Azure Ubuntu 24.04 LTS VM (default `Standard_B2s`, `centralus`); operated from a
Linux/macOS dev machine with `az`, `ssh`, `rsync`, `sqlpackage`.
**Project Type**: Deployment tooling for an existing modular-monolith web application.
**Performance Goals**: Fixed recurring cost (~$40/month baseline B2s), independent of request volume
(SC-002); idempotent redeploys; schema publish over a private channel only.
**Constraints**: SQL never exposed to the public internet (loopback + SSH tunnel); the VM's `.env` is never
overwritten by deploys; no Azure Container Apps, no Azure SQL, no Log Analytics ingestion cost; Spanish UI
and all app features unchanged (SC-009).
**Scale/Scope**: Single-instance, single-VM (no HA/multi-region) — a deliberate trade of resilience for
predictable cost. ~11 deliverable files under `deploy/vm/` + `Mentoory.Web/Dockerfile` + `.dockerignore`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The Mentoory Constitution (v1.1.2) governs application code (Clean Architecture, CQRS, DDD, etc.). This
feature adds **no application code**; it adds deployment scripts, a Dockerfile, and a `.dockerignore`. The
relevant gates and their status:

| # | Constitution rule | Applies? | Status |
|---|---|---|---|
| I | Clean Architecture layer boundaries | No app code added | ✅ N/A — unchanged |
| II | CQRS patterns | No app code added | ✅ N/A — unchanged |
| III | DDD constraints / `ExternalId` | No domain changes | ✅ N/A — unchanged |
| IV | Integration events | None added | ✅ N/A |
| V | Zero-warnings (`TreatWarningsAsErrors`) | Dockerfile compiles existing code as-is | ✅ Build uses existing source; no new warnings introduced |
| VI | DateTime via `ITimeProvider` | No app code | ✅ N/A |
| VII | Naming conventions | Shell/infra files | ✅ N/A (infra files follow reference naming) |
| VIII | File organization (JS in wwwroot, PostDeploy outside Db/) | Honored — PostDeployment already at `Mentoory.Db.PostDeployment/`; we only publish it | ✅ Respected |
| IX | Spanish-first UI | No UI text added; app UI unchanged | ✅ SC-009 preserves it |
| X | Role hierarchy & session context | No authorization changes | ✅ N/A |
| XI | **SSDT/DACPAC database strategy (no EF migrations)** | **Directly relevant** | ✅ Schema published from the existing `MentooryDb.sqlproj` dacpac incl. PostDeployment; no EF migrations introduced |

**Gate result: PASS.** No violations; no Complexity Tracking entries required. The only constitution-touching
area (XI) is satisfied by reusing the existing SSDT dacpac and PostDeployment scripts (research R5).

## Project Structure

### Documentation (this feature)

```text
specs/022-vm-deployment/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — technical decisions (R1–R11)
├── data-model.md        # Phase 1 output — config/resource/volume model
├── quickstart.md        # Phase 1 output — operator runbook
├── contracts/           # Phase 1 output — env-var, deploy-CLI, file-inventory contracts
│   ├── env-vars.md
│   ├── deploy-cli.md
│   └── deliverables.md
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

New and touched paths for this feature (no existing app code modified):

```text
deploy/
└── vm/
    ├── README.md                # One-time setup, deploys, logs, storage, backups, power schedule,
    │                            #   kill-switch, decommissioning the Aspire/Azure-SQL path
    ├── provision-vm.sh          # Create RG (if missing) + VM + NSG (dev machine, az)
    ├── provision-storage.sh     # Create Blob account + grant VM managed identity (future-use plumbing)
    ├── provision-schedule.sh    # Power schedule (auto start/stop) + manual start/stop/status/enable/disable
    ├── cloud-init.yaml          # First-boot Docker install, log cap, ufw firewall
    ├── docker-compose.yml       # caddy + webapp + mssql (+ aspire-dashboard under `debug` profile)
    ├── Caddyfile                # {$APP_DOMAIN} reverse proxy with auto-TLS
    ├── .env.example             # Secrets/config template (copied to .env on the VM; never committed)
    ├── deploy.sh                # Idempotent first-deploy + update (rsync + build-on-VM + recreate)
    ├── publish-dacpac-vm.sh     # Publish MentooryDb dacpac via SSH tunnel (sqlpackage)
    └── backup.sh                # Nightly SQL .bak + volume archive (cron on the VM)

Mentoory.Web/
└── Dockerfile                   # NEW multi-stage build (context = repo root); publishes Mentoory.Web

.dockerignore                    # NEW (repo root) — keep build context small/correct
```

**Structure Decision**: A dedicated top-level `deploy/vm/` directory mirrors the reference layout exactly,
keeping all deployment concerns isolated from the solution. The `Dockerfile` lives beside `Mentoory.Web`
(its subject) while building from the repo root so it can `COPY` the central build files and the full
referenced-project closure. `.dockerignore` must be at the repo root (the build context root).

## Phase 0: Outline & Research

**Status: COMPLETE** → see [research.md](./research.md).

All unknowns resolved (R1–R11). Headline outcomes: standalone startup needs no code change (R1);
HttpsRedirection is a no-op behind Caddy with HTTP-only ports (R2); publish via SSH tunnel keeps SQL private
(R5); GlobalAdmin is seeded by PostDeployment so no admin env var is needed (R5); OTLP export is endpoint-gated
(R6); DataProtection volume prevents logout-on-deploy (R7); storage role downgraded to Contributor as
future-use plumbing (R8).

## Phase 1: Design & Contracts

**Status: COMPLETE** → see [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md).

- **data-model.md**: the deployment "model" — Azure resource set, configuration (env) model, compose
  services, named volumes, and their relationships/lifecycle. (No application data entities are added.)
- **contracts/env-vars.md**: the `.env` contract (every variable, default, required/optional, consumer).
- **contracts/deploy-cli.md**: the command-line contract for `deploy.sh`, `publish-dacpac-vm.sh`,
  `provision-*.sh` (args, flags, env, exit behavior, idempotency guarantees).
- **contracts/deliverables.md**: the file inventory mapped to FRs (acceptance traceability).
- **quickstart.md**: the end-to-end operator runbook (zero → HTTPS), matching SC-001.

### Agent context update

The repo `CLAUDE.md` has no `<!-- SPECKIT START -->`/`<!-- SPECKIT END -->` markers, and the project uses
the `agent-context` extension (`/speckit-agent-context-update`) to manage its context block. Plan defers the
context refresh to that extension hook (offered after this command) rather than hand-editing CLAUDE.md.

## Post-Design Constitution Re-Check

Unchanged from the pre-Phase-0 check: **PASS**. The Phase 1 design adds only deployment artifacts; the one
relevant principle (XI, SSDT/DACPAC) remains satisfied by reusing the existing dacpac + PostDeployment.
No new violations, no complexity to justify.

## Complexity Tracking

No constitution violations — table intentionally omitted.
