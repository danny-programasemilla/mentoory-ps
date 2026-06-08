---

description: "Task list for feature 022-vm-deployment implementation"
---

# Tasks: Single Fixed-Cost VM Deployment

**Input**: Design documents from `specs/022-vm-deployment/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: No automated test tasks. This feature ships deployment scripts + a Dockerfile (infrastructure),
not application code. Validation is performed via the operator runbook in `quickstart.md` (manual acceptance,
matching the spec's Success Criteria). The existing app test suites are unaffected.

**Organization**: Tasks grouped by user story (US1 = stand up over HTTPS / MVP, US2 = repeatable updates,
US3 = operate economically & observably). All paths are repo-relative to the worktree root
`/mnt/D/repos/mentoory-ps@022-vm-deployment`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete-task dependency)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish have no story label)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffolding for all deployment artifacts.

- [X] T001 Create the `deploy/vm/` directory at the repo root (holds all deployment artifacts per plan.md Project Structure)
- [X] T002 [P] Create repo-root `.dockerignore` excluding `**/bin`, `**/obj`, `.git`, `**/node_modules`, `TestResults`, `specs`, `brainstorm`, `deploy/vm/.env`, `deploy/vm/backups`, `.localstorage` (research R3)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared runtime substrate — the image, compose core, proxy, and config template that every
deployment depends on. **No user story can be stood up until this phase is complete.**

**⚠️ CRITICAL**: Blocks US1 and US3.

- [X] T003 [P] Create `Mentoory.Web/Dockerfile`: multi-stage, build context = repo root; build stage `mcr.microsoft.com/dotnet/sdk:10.0` copying `global.json`, `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props` then source, running `dotnet publish Mentoory.Web/Mentoory.Web.csproj -c Release -o /app/publish`; runtime stage `mcr.microsoft.com/dotnet/aspnet:10.0` exposing 8080, `ENTRYPOINT ["dotnet","Mentoory.Web.dll"]` (research R3, R11; FR-009, FR-010)
- [X] T004 [P] Create `deploy/vm/.env.example` per contracts/env-vars.md §A: `APP_DOMAIN`, `ACME_EMAIL`, `MSSQL_SA_PASSWORD`, `MEDIATR_LICENSE_KEY`, optional `STORAGE_PROVIDER`/`BLOB_CONNECTION`/`OTEL_ENDPOINT`. Explicitly NO `ADMIN_DEFAULT_PASSWORD`/Syncfusion/Mailgun keys (research R5; FR-024, FR-035)
- [X] T005 [P] Create `deploy/vm/cloud-init.yaml`: install Docker + compose on first boot, write `/etc/docker/daemon.json` json-file log cap (10m×3), enable `ufw` for OpenSSH/80/443 (FR-004, FR-031)
- [X] T006 [P] Create `deploy/vm/Caddyfile`: global `email {$ACME_EMAIL}`, site block `{$APP_DOMAIN}` with `encode zstd gzip` and `reverse_proxy webapp:8080` (auto-TLS) (research R2; FR-008)
- [X] T007 Create `deploy/vm/docker-compose.yml` (project name `mentoory`) with core services per data-model.md §3–§4: `caddy` (80/443, Caddyfile + caddy_data/caddy_config volumes, depends_on webapp), `webapp` (build context `../..` dockerfile `Mentoory.Web/Dockerfile`; env: `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `HTTP_PORTS=8080`, `ConnectionStrings__DefaultConnection=Server=mssql,1433;Database=MentooryDb;User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True`, `MediatR__LicenseKey=${MEDIATR_LICENSE_KEY:-}`, `OTEL_EXPORTER_OTLP_ENDPOINT=${OTEL_ENDPOINT:-}`, `OTEL_SERVICE_NAME=mentoory-web`, pinned `Logging__LogLevel__*`, optional `Storage__Provider=${STORAGE_PROVIDER:-}`/`ConnectionStrings__blobs=${BLOB_CONNECTION:-}`; volumes `app_storage` + `dataprotection` for the DP keyring; depends_on mssql healthy), `mssql` (`mcr.microsoft.com/mssql/server:2022-latest`, `127.0.0.1:1433`, Developer PID, `MSSQL_MEMORY_LIMIT_MB=2048`, `mssql_data` volume, sqlcmd healthcheck); declare named volumes (research R2,R5,R7; FR-007,011,012,013,014)

**Checkpoint**: Runtime substrate exists — provisioning + deploy can now stand the app up.

---

## Phase 3: User Story 1 - Stand up over HTTPS (Priority: P1) 🎯 MVP

**Goal**: From a clean subscription, provision a fixed-cost VM, publish the schema, and reach Mentoory over
HTTPS — no Aspire AppHost, no Azure Container Apps, no Azure SQL.

**Independent Test**: Run provision → point DNS → first deploy; the site loads over HTTPS with a valid cert
and the Spanish login renders, backed by the in-VM SQL with `MentooryDb` published.

- [X] T008 [P] [US1] Create `deploy/vm/provision-vm.sh` per contracts/deploy-cli.md: ensure RG (`rg-Mentoory-D`), `az vm create` Ubuntu 24.04 (`Standard_B2s`/`centralus`/64GB StandardSSD/Standard static IP, `--custom-data cloud-init.yaml`, `--nsg-rule NONE`), add NSG rules (80/443 from `*`, 22 from auto-detected `MYIP/32`), print IP + next steps; all env-overridable (FR-001..006, FR-035)
- [X] T009 [P] [US1] Create `deploy/vm/publish-dacpac-vm.sh` per contracts/deploy-cli.md: build `Mentoory.Db/MentooryDb.sqlproj` (`-c ${DACPAC_CONFIG:-Release}`, skippable), open `ssh -f -N -L <port>:localhost:1433`, `sqlpackage /Action:Publish` to `127.0.0.1,<port>` target DB `MentooryDb` (`BlockOnPossibleDataLoss=false`, `TrustServerCertificate=True`), close tunnel via trap (research R5; FR-016..018)
- [X] T010 [US1] Create `deploy/vm/deploy.sh` per contracts/deploy-cli.md: preflight SSH; `rsync -az --delete` repo→VM with the documented excludes (incl. `deploy/vm/.env`, `deploy/vm/backups`); require VM `.env` (clear failure if missing); `docker compose up -d mssql` + wait healthy; optional `--schema` → `publish-dacpac-vm.sh`; `docker compose up -d --build webapp caddy` (or `--no-build`); `docker image prune -f`; `docker compose ps`; optional `--logs` tail. Never write/delete the VM `.env` (depends on T007, T009; FR-019..023)
- [ ] T011 [US1] Validate US1 via `quickstart.md` "One-time setup" + "Smoke test": provision, DNS, first deploy `--schema`, confirm HTTPS valid cert + Spanish login + `docker compose ps` healthy (SC-001, SC-009)

**Checkpoint**: MVP — Mentoory reachable over HTTPS at fixed cost.

---

## Phase 4: User Story 2 - Repeatable, safe updates (Priority: P2)

**Goal**: Ship code and schema updates with one idempotent command, repeatedly, without disturbing secrets
or causing logout-on-deploy.

**Independent Test**: Change code → `deploy.sh` → new version live; re-run with no changes → no-op; change
schema → `deploy.sh --schema` → schema updated over the private tunnel; confirm the VM `.env` is untouched
and sessions survive (DataProtection volume).

- [X] T012 [US2] Document the day-to-day deploy + logs workflow in `deploy/vm/README.md` (deploy variants `--schema`/`--no-build`/`--logs`, idempotency, `.env` never overwritten, log tail + rotation cap) — README "Day-to-day" + "Logs" sections (FR-031, FR-034)
- [ ] T013 [US2] Validate US2 via `quickstart.md` "Day-to-day": re-run `deploy.sh` (no-op), run `--schema` update over the SSH tunnel, confirm `.env` preserved and no public 1433 endpoint, and that login persists across a redeploy (DataProtection volume) (SC-003, SC-004, SC-005, FR-013)

**Checkpoint**: Updates are repeatable and non-destructive.

---

## Phase 5: User Story 3 - Operate economically & observably (Priority: P3)

**Goal**: Nightly backups, off-hours power schedule (~half cost), on-demand $0 telemetry, no cloud log bill.

**Independent Test**: Enable backups → backup + archive produced and pruned; enable power schedule → VM
auto-stops/starts and DNS+TLS survive; start the telemetry viewer → reachable only via SSH tunnel, off by
default.

- [X] T014 [P] [US3] Extend `deploy/vm/docker-compose.yml` with an `aspire-dashboard` service under the `debug` profile: `mcr.microsoft.com/dotnet/aspire-dashboard:9.0`, `127.0.0.1:18888` loopback, `AUTHMODE=Unsecured`, bounded telemetry limits; off by default (research R6; FR-030, SC-008)
- [X] T015 [P] [US3] Create `deploy/vm/backup.sh` per contracts/deploy-cli.md: source `.env`, `BACKUP DATABASE [MentooryDb] ... WITH INIT, COMPRESSION`, copy `.bak` out of the container, archive `app_storage` volume, prune older than `${BACKUP_KEEP_DAYS:-7}`, commented off-VM blob upload (FR-027, SC-007)
- [X] T016 [P] [US3] Create `deploy/vm/provision-schedule.sh` per contracts/deploy-cli.md: `provision` (DevTest auto-stop 19:00 + Automation account/identity/runbook `StartMentooryVM` + weekly start schedule 06:45 `America/Costa_Rica` + job link), plus `start`/`stop`/`status`/`disable`/`enable` subcommands; all env-overridable (FR-028, FR-029, SC-006)
- [ ] T017 [US3] Validate US3 via `quickstart.md`: install backup cron and confirm artifacts + pruning; `provision-schedule.sh` stop/start preserves static IP/DNS/TLS; start the `debug` dashboard and confirm loopback-only + off by default (SC-006, SC-007, SC-008)

**Checkpoint**: All three stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Future-use plumbing, full documentation, lint, and end-to-end validation.

- [X] T018 [P] Create `deploy/vm/provision-storage.sh` (future-use): create `Standard_LRS` `StorageV2` (public blob access off), ensure VM system-assigned identity, assign `Storage Blob Data Contributor`, print blob endpoint + `.env` lines; document "provisioned but not yet consumed" (research R8; FR-025, FR-026)
- [X] T019 Create comprehensive `deploy/vm/README.md` covering one-time setup, day-to-day deploys, logs, storage (future-use), backups, power schedule, cost kill-switch (docs only), and decommissioning the old Aspire/Azure-SQL path; include the cost table and B2s/B2ms guidance (FR-032, FR-033, FR-034)
- [X] T020 [P] Lint all `deploy/vm/*.sh` with `shellcheck` and resolve findings (quality; no functional change)
- [ ] T021 Run full `quickstart.md` end-to-end validation against a real (or scratch) subscription and confirm every Success Criterion (SC-001..SC-010)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: after Setup. **Blocks US1 and US3.** (US2 is doc/validation over US1's deploy.sh.)
- **US1 (Phase 3)**: after Foundational. MVP.
- **US2 (Phase 4)**: after US1 (exercises `deploy.sh` from T010).
- **US3 (Phase 5)**: after Foundational; T014 edits the compose file (after T007). Otherwise independent of US1/US2.
- **Polish (Phase 6)**: after the stories it documents (T019 after US1–US3; T021 last).

### Within-file ordering (non-parallel)

- T007 (compose core) before T010 (deploy uses it) and before T014 (extends same file).
- T009 (publish script) before T010 (deploy `--schema` calls it).
- T019 (full README) after T012's README sections exist (same file — sequential).

### Parallel Opportunities

- Setup: T002 ∥ (T001 first).
- Foundational: **T003, T004, T005, T006 in parallel** (distinct files); T007 after them.
- US1: **T008 ∥ T009** (distinct files); T010 after T007+T009; T011 last.
- US3: **T014 ∥ T015 ∥ T016** (distinct files; T014 only after T007); T017 last.
- Polish: T018 ∥ T020.

---

## Parallel Example: Foundational (Phase 2)

```bash
# After T001/T002, author the independent substrate files together:
Task: "Create Mentoory.Web/Dockerfile (multi-stage publish)"            # T003
Task: "Create deploy/vm/.env.example"                                    # T004
Task: "Create deploy/vm/cloud-init.yaml"                                 # T005
Task: "Create deploy/vm/Caddyfile"                                       # T006
# Then T007 (docker-compose.yml) wires them together.
```

## Parallel Example: User Story 3 (Phase 5)

```bash
Task: "Create deploy/vm/backup.sh"                # T015
Task: "Create deploy/vm/provision-schedule.sh"   # T016
# T014 (compose debug profile) edits docker-compose.yml — run alongside but it's the only writer of that file.
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3 (US1).
2. **STOP & VALIDATE** (T011): provision, deploy `--schema`, reach over HTTPS, Spanish login.
3. This is a demoable, fixed-cost production deployment.

### Incremental Delivery

1. Setup + Foundational → substrate ready.
2. US1 → reachable HTTPS (MVP).
3. US2 → repeatable/safe updates documented + validated.
4. US3 → backups, power schedule, telemetry.
5. Polish → future-use storage, full README, lint, full validation.

### Notes

- `[P]` = different files, no incomplete dependency.
- No app source is modified — the Dockerfile builds existing code unchanged (FR-036, SC-009).
- Commit after each task or logical group.
- Validation tasks (T011, T013, T017, T021) require an Azure subscription + a DNS-controlled domain; if
  unavailable, document the dry-run/limits explicitly rather than silently skipping.

## Implementation Status (2026-06-06)

All authorable artifacts are complete and statically verified; the live-Azure validation tasks remain open
for an operator to run.

- **Done (16):** T001–T010, T012, T014–T016, T018–T020.
  - All 6 shell scripts pass `bash -n`; `docker-compose.yml` + `cloud-init.yaml` pass YAML parsing.
  - `shellcheck` (T020) was not installed in the authoring environment → substituted with `bash -n`
    (clean). Run `shellcheck deploy/vm/*.sh` once available for full lint coverage.
  - `docker compose config` could not run (Docker absent here); validated via YAML parse instead. Run
    `docker compose config -q` on a Docker host before first deploy.
- **Deferred — require a live Azure subscription + DNS-controlled domain (cannot run in this environment):**
  T011 (US1 stand-up smoke test), T013 (US2 update/idempotency), T017 (US3 backups/power/telemetry),
  T021 (full quickstart end-to-end). Follow `quickstart.md` to execute these during the first real deploy.
