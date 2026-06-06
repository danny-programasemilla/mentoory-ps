# Contract: Command-Line Interfaces

**Feature**: 022-vm-deployment

Defines the invocation contract, behavior, and guarantees for each script. All scripts use
`set -euo pipefail`, verify prerequisites (`az`/`rsync`/`sqlpackage`/`ssh` as applicable), and print
actionable errors. All are runnable from the dev machine except `backup.sh` (runs on the VM).

---

## `provision-vm.sh` (dev machine)
- **Usage**: `./provision-vm.sh`
- **Pre**: `az` installed + logged in.
- **Does**: ensure RG → `az vm create` (Ubuntu 24.04, size/disk/IP per env) with `--custom-data cloud-init.yaml`
  and `--nsg-rule NONE` → add NSG rules (80/443 from `*`, 22 from `MYIP/32`) → print VM public IP + next steps.
- **Idempotency**: re-runnable green-field; `az group create` is idempotent. (Re-running full `vm create` on an
  existing VM errors — documented; intended for first provision / post-teardown.)
- **Exit**: non-zero if not logged in, IP undetectable, or create fails.

## `provision-storage.sh` (dev machine, after provision-vm)
- **Usage**: `./provision-storage.sh`
- **Does**: create `Standard_LRS` `StorageV2` (public blob access off) → ensure VM system identity →
  assign `Storage Blob Data Contributor` on the account → print blob endpoint + the `.env` lines.
- **Idempotency**: safe to re-run (create/assign are tolerant); role propagation may take minutes.
- **Note**: future-use plumbing; the deployment runs fully without it.

## `provision-schedule.sh` (dev machine)
- **Usage**: `./provision-schedule.sh {provision|start|stop|status|disable|enable}`
- **`provision`**: create/refresh DevTest auto-stop + Automation account/identity/runbook/schedule + job link
  (idempotent — recreates the schedule since Automation schedules are largely immutable).
- **`start`/`stop`**: `az vm start` / `az vm deallocate` now (manual override of schedule).
- **`status`**: print power state.
- **`disable`/`enable`**: toggle both schedules without changing current power state.
- **Exit**: non-zero on `az` failure; subcommand required.

## `deploy.sh` (dev machine) — first deploy AND every update
- **Usage**: `./deploy.sh <vm-ip-or-host> [--schema] [--no-build] [--logs]`
- **Flags**: `--schema` publish dacpac (needs `MSSQL_SA_PASSWORD`); `--no-build` recreate without rebuild;
  `--logs` tail webapp+caddy after.
- **Steps**: (1) preflight SSH; (2) `rsync -az --delete` repo→VM excluding `.git`, `**/bin`, `**/obj`,
  `**/node_modules`, `deploy/vm/.env`, `deploy/vm/backups`, `TestResults`, `.localstorage`; (3) require VM
  `.env`, `docker compose up -d mssql`, wait for healthy; (4) if `--schema`, run `publish-dacpac-vm.sh`;
  (5) `docker compose up -d --build webapp caddy` (or no `--build`), `docker image prune -f`; (6) `ps`;
  (7) optional log tail.
- **Guarantees**: never writes/deletes the VM `.env` (FR-023); idempotent — no-op when nothing changed;
  fails clearly if `.env` missing (FR-023) or SQL never healthy.
- **Exit**: non-zero on SSH failure, missing `.env`, SQL-not-healthy, or unknown flag.

## `publish-dacpac-vm.sh` (dev machine)
- **Usage**: `./publish-dacpac-vm.sh <vm-ip-or-host> [--skip-build]`
- **Pre**: `sqlpackage` installed; `MSSQL_SA_PASSWORD` set.
- **Does**: build `Mentoory.Db/MentooryDb.sqlproj` (`-c $DACPAC_CONFIG`, unless `--skip-build`) → open
  `ssh -f -N -L <localport>:localhost:1433` tunnel → `sqlpackage /Action:Publish` to `127.0.0.1,<localport>`,
  target DB `MentooryDb`, `BlockOnPossibleDataLoss=false`, `TrustServerCertificate=True` → close tunnel (trap).
- **Guarantee**: SQL never publicly exposed — access only via the loopback tunnel (SC-004).
- **Exit**: non-zero if `sqlpackage` missing, dacpac not found, tunnel/publish fails.

## `backup.sh` (on the VM, via cron)
- **Usage**: `./backup.sh` (install: `30 3 * * * ~/deploy/vm/backup.sh >> ~/backup.log 2>&1`)
- **Does**: source `.env` → `BACKUP DATABASE [MentooryDb] ... WITH INIT, COMPRESSION` → copy `.bak` out of
  container → archive `app_storage` volume (if present) → prune artifacts older than `BACKUP_KEEP_DAYS`.
- **Exit**: non-zero if `.env`/password missing or backup fails.

---

## Acceptance mapping (selected)
- `provision-vm.sh` → FR-001..006, SC-001. `deploy.sh` → FR-019..023, SC-003, SC-005.
- `publish-dacpac-vm.sh` → FR-016..018, SC-004. `provision-schedule.sh` → FR-028,029, SC-006.
- `backup.sh` → FR-027, SC-007. `provision-storage.sh` → FR-025,026. compose `debug` profile → FR-030, SC-008.
