# Contract: Configuration / Environment Variables

**Feature**: 022-vm-deployment

Two configuration surfaces: (A) the VM's `.env` (runtime, consumed by docker compose) and (B) script-time
environment variables (consumed by provisioning/deploy scripts, with documented defaults).

---

## A. Runtime `.env` (on the VM; template = `deploy/vm/.env.example`)

`.env` is copied from `.env.example`, filled by the operator, never committed, and never overwritten by
`deploy.sh`.

| Variable | Required | Default (in example) | Consumer | Purpose |
|---|---|---|---|---|
| `APP_DOMAIN` | **Yes** | `mentoory-dev.example.com` | Caddyfile, webapp | TLS domain + app base URL |
| `ACME_EMAIL` | **Yes** | `you@example.com` | Caddy | Let's Encrypt registration/recovery |
| `MSSQL_SA_PASSWORD` | **Yes** | `ChangeMe_Strong1!` | mssql + webapp conn string | SA password (SQL complexity rules) |
| `MEDIATR_LICENSE_KEY` | Recommended | _(empty)_ | webapp → `MediatR__LicenseKey` | MediatR 14 license; empty runs with community notice |
| `STORAGE_PROVIDER` | No | _(unset → no-op)_ | webapp → `Storage__Provider` | Future blob/local switch; unused today |
| `BLOB_CONNECTION` | No | _(empty)_ | webapp → `ConnectionStrings__blobs` | Blob endpoint URI or conn string (future) |
| `OTEL_ENDPOINT` | No | _(empty = no export)_ | webapp → `OTEL_EXPORTER_OTLP_ENDPOINT` | Point at `http://aspire-dashboard:18889` to view telemetry |

**Derived (set in `docker-compose.yml`, not in `.env`)** — webapp container env:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`
- `HTTP_PORTS=8080`
- `ConnectionStrings__DefaultConnection=Server=mssql,1433;Database=MentooryDb;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True`
- `OTEL_SERVICE_NAME=mentoory-web`, `OTEL_EXPORTER_OTLP_PROTOCOL=grpc`
- `Logging__LogLevel__Default=Warning`, `Logging__LogLevel__Microsoft.AspNetCore=Warning`,
  `Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Warning`

> **Explicitly NOT present** (vs. the reference): no `ADMIN_DEFAULT_PASSWORD` (GlobalAdmin seeded by
> PostDeployment `002.SeedGlobalAdmin.sql`), no `SYNCFUSION_LICENSE_KEY` (Mentoory does not use Syncfusion),
> no Mailgun/Notifications/AI/FundingAgreement keys (FundingPlatform-specific).

---

## B. Script-time environment (provisioning & deploy)

### `provision-vm.sh`
| Var | Default | Meaning |
|---|---|---|
| `RESOURCE_GROUP` | `rg-Mentoory-D` | RG name (created if missing) |
| `LOCATION` | `centralus` | Azure region |
| `VM_NAME` | `vm-mentoory-dev` | VM name |
| `VM_SIZE` | `Standard_B2s` | VM size (bump to `Standard_B2ms` for 8 GB) |
| `ADMIN_USER` | `azureuser` | SSH admin user |
| `OS_DISK_GB` | `64` | OS disk size |
| `MYIP` | auto-detected | Public IP locked for SSH (override if detection fails) |

### `provision-storage.sh`
| Var | Default | Meaning |
|---|---|---|
| `RESOURCE_GROUP`/`LOCATION`/`VM_NAME` | as above | Target context |
| `STORAGE_ACCOUNT` | `stmentoorydev` | Globally-unique account name (3–24, lowercase alnum) |

Grants the VM identity `Storage Blob Data Contributor`; prints the blob endpoint. **Future use only.**

### `provision-schedule.sh`
| Var | Default | Meaning |
|---|---|---|
| `RESOURCE_GROUP`/`VM_NAME`/`LOCATION` | as above | Target |
| `AUTOMATION_ACCOUNT` | `aa-mentoory` | Automation account |
| `RUNBOOK_NAME` | `StartMentooryVM` | Start runbook |
| `SCHEDULE_NAME` | `weekday-0645-cr` | Weekly start schedule |
| `START_TIME` | `0645` | Auto-start (HHMM) |
| `STOP_TIME` | `1900` | Auto-stop (HHMM) |
| `TIMEZONE_IANA` | `America/Costa_Rica` | Schedule TZ (IANA) |
| `TIMEZONE_WINDOWS` | `Central America Standard Time` | DevTest TZ (Windows id) |
| `UTC_OFFSET` | `-06:00` | Start ISO offset |
| `WEEKDAYS` | `Monday..Friday` | Start days |

### `deploy.sh`
| Var | Default | Meaning |
|---|---|---|
| `ADMIN_USER` | `azureuser` | SSH user |
| `APP_DIR` | `/home/${ADMIN_USER}/app` | Remote repo path |
| `MSSQL_SA_PASSWORD` | _(required only with `--schema`)_ | Must match VM `.env` |

### `publish-dacpac-vm.sh`
| Var | Default | Meaning |
|---|---|---|
| `ADMIN_USER` | `azureuser` | SSH user |
| `MSSQL_SA_PASSWORD` | **required** | Must match VM `.env` |
| `DACPAC_CONFIG` | `Release` | Build config for the dacpac |

### `backup.sh` (on VM)
| Var | Default | Meaning |
|---|---|---|
| `BACKUP_DIR` | `$HOME/backups` | Output dir |
| `BACKUP_KEEP_DAYS` | `7` | Retention |
| (`MSSQL_SA_PASSWORD` sourced from `.env`) | — | For `BACKUP DATABASE` |
