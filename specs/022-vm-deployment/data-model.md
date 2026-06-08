# Phase 1 Data Model: Single Fixed-Cost VM Deployment

**Feature**: 022-vm-deployment
**Date**: 2026-06-06

This feature adds **no application data entities** (no aggregates, tables, or DTOs). The "model" here is the
deployment model: the Azure resources, the configuration surface, the runtime services, and the durable
volumes — and how they relate and live/die. It maps the spec's Key Entities to concrete artifacts.

---

## 1. Azure Resource Set (created by provisioning scripts)

| Resource | Default name (env-overridable) | Created by | Notes |
|---|---|---|---|
| Resource group | `rg-Mentoory-D` (`RESOURCE_GROUP`) | `provision-vm.sh` | Created if missing (idempotent) |
| Linux VM | `vm-mentoory-dev` (`VM_NAME`) | `provision-vm.sh` | Ubuntu 24.04, `Standard_B2s` (`VM_SIZE`), 64 GB StandardSSD (`OS_DISK_GB`) |
| Static public IP | (VM-associated, Standard SKU) | `provision-vm.sh` | Persists across stop/start → stable DNS/TLS |
| Network security group | `${VM_NAME}NSG` | `provision-vm.sh` | Inbound: 80/443 from `*`; 22 from operator IP `/32` |
| System-assigned managed identity | (on the VM) | `provision-storage.sh` / `provision-schedule.sh` | Used for Blob (future) and Automation start |
| Storage account | `stmentoorydev` (`STORAGE_ACCOUNT`) | `provision-storage.sh` | `Standard_LRS` `StorageV2`, public blob access off — **future use** |
| Role assignment | `Storage Blob Data Contributor` on the account → VM identity | `provision-storage.sh` | Least-surprise default; no consumer yet |
| Automation account | `aa-mentoory` (`AUTOMATION_ACCOUNT`) | `provision-schedule.sh` | Free SKU; system identity → `Virtual Machine Contributor` on the VM |
| Runbook | `StartMentooryVM` (`RUNBOOK_NAME`) | `provision-schedule.sh` | PowerShell REST start via managed identity |
| Automation schedule | `weekday-0645-cr` (`SCHEDULE_NAME`) | `provision-schedule.sh` | Weekly, weekday start |
| DevTest auto-shutdown schedule | `shutdown-computevm-${VM_NAME}` | `provision-schedule.sh` | Daily stop |

**Lifecycle**: provisioning is idempotent/re-runnable; deleting the RG stops all billing and a re-run of
`provision-vm.sh` recreates the green-field. Stop/start (manual or scheduled) deallocates/reallocates compute
only; disk + static IP persist.

---

## 2. Configuration Model (the `.env` surface)

`.env` lives only on the VM (copied from `.env.example`, never committed, never overwritten by deploys).
Full variable contract in [contracts/env-vars.md](./contracts/env-vars.md). Grouped here by purpose:

- **Proxy / TLS**: `APP_DOMAIN`, `ACME_EMAIL`.
- **Database**: `MSSQL_SA_PASSWORD` → feeds both the `mssql` container and the webapp's
  `ConnectionStrings__DefaultConnection` (`Server=mssql,1433;Database=MentooryDb;User Id=sa;...`).
- **Application**: `MEDIATR_LICENSE_KEY` (→ `MediatR__LicenseKey`), pinned log levels.
- **Storage (future/optional)**: `STORAGE_PROVIDER` (default unset/no-op), `BLOB_CONNECTION` (blob endpoint URI).
- **Telemetry (optional)**: `OTEL_ENDPOINT` (empty = no export).

**Provisioning-time config** (env vars consumed by the scripts, not in `.env`): `RESOURCE_GROUP`, `LOCATION`,
`VM_NAME`, `VM_SIZE`, `OS_DISK_GB`, `ADMIN_USER`, `MYIP`, `STORAGE_ACCOUNT`, `AUTOMATION_ACCOUNT`,
`RUNBOOK_NAME`, `SCHEDULE_NAME`, `START_TIME`, `STOP_TIME`, `TIMEZONE_IANA`, `TIMEZONE_WINDOWS`,
`UTC_OFFSET`, `WEEKDAYS`, `BACKUP_DIR`, `BACKUP_KEEP_DAYS`, `ADMIN_USER`/`APP_DIR`/`DACPAC_CONFIG` (deploy).

---

## 3. Runtime Services (docker compose, project name `mentoory`)

| Service | Image | Ports | Depends on | Profile | Purpose |
|---|---|---|---|---|---|
| `caddy` | `caddy:2-alpine` | 80, 443 (public) | `webapp` | default | Auto-TLS reverse proxy → `webapp:8080` |
| `webapp` | built from `Mentoory.Web/Dockerfile` | (internal 8080) | `mssql` (healthy) | default | The .NET 10 Mentoory app, standalone |
| `mssql` | `mcr.microsoft.com/mssql/server:2022-latest` | `127.0.0.1:1433` (loopback) | — | default | MentooryDb host; memory-capped; healthchecked |
| `aspire-dashboard` | `mcr.microsoft.com/dotnet/aspire-dashboard:9.0` | `127.0.0.1:18888` (loopback) | — | `debug` | On-demand in-memory telemetry viewer |

**webapp environment (key bindings)**: `ASPNETCORE_ENVIRONMENT=Production`,
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `HTTP_PORTS=8080`,
`ConnectionStrings__DefaultConnection=...`, `MediatR__LicenseKey=${MEDIATR_LICENSE_KEY}`,
`OTEL_EXPORTER_OTLP_ENDPOINT=${OTEL_ENDPOINT:-}`, `OTEL_SERVICE_NAME=mentoory-web`,
pinned `Logging__LogLevel__*`, optional `Storage__Provider`/`ConnectionStrings__blobs`.

---

## 4. Durable Volumes (on the VM data disk)

| Volume | Mounted in | Holds | Captured by backup? |
|---|---|---|---|
| `mssql_data` | `mssql` | SQL Server data/log files (MentooryDb) | Via `BACKUP DATABASE` → `.bak` |
| `dataprotection` | `webapp` | ASP.NET DataProtection keyring | Indirectly (keys persist across recreate; FR-013) |
| `caddy_data`, `caddy_config` | `caddy` | Issued TLS certs + proxy state | Not critical (re-issued on demand) |
| `app_storage` | `webapp` | LocalFilesystem attachments (only if `STORAGE_PROVIDER=LocalFilesystem`) | Yes — volume archive |

**Key relationship**: `dataprotection` volume ⇒ redeploys do not invalidate `Mentoory.Session` cookies or
antiforgery tokens (FR-013, edge case). `mssql_data` + the nightly `.bak` ⇒ recoverable database (FR-027).

---

## 5. Schema Artifact

- **Source**: `Mentoory.Db/MentooryDb.sqlproj` (`Microsoft.Build.Sql/2.0.0`) → `MentooryDb.dacpac`.
- **Embedded**: `Script.PostDeployment.sql` + numbered seeds (`001.SeedRoles`, `002.SeedGlobalAdmin`,
  `003.SeedDefaultSubscriptionPlan`, `005.SeedKnowledgeData`, `016.SeedCountries`,
  `017.SeedSystemConfiguration`, `018.SeedProjectPublicFlag`; `000`/`004` are example/test data).
- **Target**: database `MentooryDb` on the loopback-only `mssql` container, reached via SSH tunnel.
- **Consequence**: GlobalAdmin and reference data exist after first publish — first-login credentials come
  from the dacpac, not from any app env var.

---

## 6. Backup Artifact

- Nightly (cron on VM): compressed `MentooryDb` `.bak` + (if used) `app_storage` tar.gz, into `~/backups`.
- Retention: `BACKUP_KEEP_DAYS` (default 7); older artifacts pruned.
- Optional off-VM copy to Blob (commented line) for durability.

---

## 7. Traceability (spec Key Entities → this model)

| Spec entity | Where modeled |
|---|---|
| VM Deployment Host | §1 (VM, IP, NSG, identity) |
| Container Stack | §3 + §4 |
| Secrets File | §2 (`.env`) + contracts/env-vars.md |
| Schema Artifact | §5 |
| Backup Artifact | §6 |
| Object-Storage Account (future) | §1 (storage account + role) + §2 |
| Power Schedule | §1 (Automation + DevTest) + contracts/deploy-cli.md |
