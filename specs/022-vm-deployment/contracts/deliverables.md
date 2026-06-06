# Contract: Deliverables Inventory & FR Traceability

**Feature**: 022-vm-deployment

Every artifact this feature ships, with the requirements it satisfies. Used as the implementation checklist
source and acceptance traceability.

| # | File (repo-relative) | Purpose | Satisfies |
|---|---|---|---|
| 1 | `Mentoory.Web/Dockerfile` | Multi-stage build of `Mentoory.Web` (context = repo root), publish → aspnet:10.0 runtime, port 8080 | FR-009, FR-010 |
| 2 | `.dockerignore` (repo root) | Trim/clean build context (bin/obj/.git/node_modules/specs/etc.) | FR-009 |
| 3 | `deploy/vm/provision-vm.sh` | RG (if missing) + VM + static IP + NSG (80/443 public, 22→operator) | FR-001..006 |
| 4 | `deploy/vm/cloud-init.yaml` | First-boot Docker + compose, json-file log cap, ufw firewall | FR-004, FR-031 |
| 5 | `deploy/vm/docker-compose.yml` | `caddy` + `webapp` + `mssql` (+ `aspire-dashboard` debug profile); volumes; env | FR-007,011,013,014,030 |
| 6 | `deploy/vm/Caddyfile` | `{$APP_DOMAIN}` auto-TLS reverse proxy → `webapp:8080` | FR-008 |
| 7 | `deploy/vm/.env.example` | Secrets/config template (no admin/Syncfusion keys) | FR-024, FR-035 |
| 8 | `deploy/vm/deploy.sh` | Idempotent first-deploy + update (rsync + build-on-VM + recreate) | FR-019..023 |
| 9 | `deploy/vm/publish-dacpac-vm.sh` | Publish `MentooryDb` dacpac via SSH tunnel (sqlpackage) | FR-016..018 |
| 10 | `deploy/vm/provision-storage.sh` | Blob account + VM managed-identity role (future-use) | FR-025, FR-026 |
| 11 | `deploy/vm/provision-schedule.sh` | Power schedule + manual start/stop/status/enable/disable | FR-028, FR-029 |
| 12 | `deploy/vm/backup.sh` | Nightly SQL `.bak` + volume archive, retention/prune | FR-027 |
| 13 | `deploy/vm/README.md` | Setup, deploys, logs, storage, backups, power schedule, kill-switch, decommission | FR-031,032,033,034 |

## Cross-cutting requirements
- **FR-035** (env-overridable everywhere): each script reads `${VAR:-default}`; documented in
  [env-vars.md](./env-vars.md).
- **FR-036 / SC-009** (no app regression, Spanish UI intact): no application source modified — the Dockerfile
  builds existing code unchanged; verified by the quickstart smoke test (login + Spanish UI render).
- **FR-012** (`DefaultConnection`/`MentooryDb`): set in `docker-compose.yml` webapp env (item 5).
- **FR-015** (state on VM, backed up): volumes in item 5, captured by item 12.

## Out-of-scope confirmations (no deliverable)
- No application code change (research R1) — FR-010 met via configuration only.
- No `/health` Production endpoint (research R4).
- No EF migrations (Constitution XI) — schema via dacpac only.
- Cost kill-switch is documentation only (FR-032) — covered in README (item 13), no script.
