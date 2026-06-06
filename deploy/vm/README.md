# Single-VM deployment — Mentoory

Fixed-cost alternative to the Aspire / Azure Container Apps + Azure SQL stack. One Linux
VM runs everything via Docker Compose:

```
Caddy (auto-TLS, 80/443)
  └─ webapp   (.NET 10 Mentoory.Web, built from Mentoory.Web/Dockerfile)
  └─ mssql    (SQL Server 2022 Developer, loopback-only)
  └─ (future) attachments → Azure Blob via the VM managed identity
```

## Why a VM

Azure has **no native "stop at $X"** — budgets only alert, and Container Apps + Azure SQL
bill by usage. A VM's compute cost is **fixed** whether idle or busy, so the bill can't
surprise you. Logs/telemetry stay on the VM, so there is **no Log Analytics ingestion cost**.
For a literal kill-switch, wire a budget alert → runbook that **deallocates** the VM (below).

## Cost (centralus, ~fixed/month)

| Item | Choice | ~Cost |
|---|---|---|
| VM | `Standard_B2s` (2 vCPU, 4 GB) | $30–38 |
| OS disk | 64 GB StandardSSD | $5 |
| Static public IP | Standard | $3–4 |
| **Total** | | **~$40, predictable** |

> B2s (4 GB) is the floor for SQL + webapp. It's tight, especially if the Aspire dashboard
> runs. On OOM kills, set `VM_SIZE=Standard_B2ms` (8 GB, ~$60) before `provision-vm.sh`, or
> resize later: `az vm resize -g rg-Mentoory-D -n vm-mentoory-dev --size Standard_B2ms`.

## One-time setup

```bash
cd deploy/vm

# 1. Provision the VM (creates the RG if missing, locks SSH to your IP, opens 80/443).
./provision-vm.sh
#    -> prints the VM public IP.

# 2. (FUTURE USE) Provision attachments storage + grant the VM managed identity.
#    Skip unless an attachments feature needs it — nothing consumes blob storage today.
./provision-storage.sh

# 3. DNS: point an A record at the IP and wait for it to resolve:
#       <your APP_DOMAIN>  ->  <VM IP>
#    Caddy can't issue the TLS cert until this resolves publicly.

# 4. Ship the deploy files to the VM (first time only; deploy.sh syncs the repo after).
scp -r ../../deploy azureuser@<VM IP>:~/app/deploy

# 5. On the VM: configure secrets and start SQL.
ssh azureuser@<VM IP>
cd ~/app/deploy/vm
cp .env.example .env && nano .env      # set APP_DOMAIN, ACME_EMAIL, MSSQL_SA_PASSWORD,
                                       # MEDIATR_LICENSE_KEY (recommended)
docker compose up -d mssql             # wait until healthy: docker compose ps
exit

# 6. From your DEV MACHINE: first deploy — sync source, publish schema, build, start.
MSSQL_SA_PASSWORD='<same as VM .env>' ./deploy.sh <VM IP> --schema --logs
```

Visit `https://<your APP_DOMAIN>` — Caddy serves a valid Let's Encrypt cert automatically.
Log in as the seeded GlobalAdmin (provisioned by PostDeployment `002.SeedGlobalAdmin.sql`).

## Day-to-day — deploy updates

One command from your **dev machine** handles every redeploy. It rsyncs the repo to the VM
(never touching the VM's `.env`), rebuilds the image **on the VM**, and recreates only what
changed. Safe to run repeatedly (idempotent).

```bash
./deploy.sh <VM IP>                                  # code change
MSSQL_SA_PASSWORD='…' ./deploy.sh <VM IP> --schema   # code + schema change
./deploy.sh <VM IP> --no-build                       # recreate without rebuilding
./deploy.sh <VM IP> --logs                           # tail logs after
```

> The image builds on the VM (the Dockerfile COPYs from the synced source). On a 4 GB B2s
> the .NET SDK build is heavy but fine; it won't interrupt the running container until the
> new image is ready.

## Logs (no cloud log cost)

Everything runs on the VM, so there is **no Azure log cost** — no Log Analytics ingestion.

```bash
docker compose logs -f webapp           # live stream
docker compose logs --since 30m webapp  # last 30 minutes
docker compose logs --tail 200 webapp   # last 200 lines
```

Disk usage is capped by cloud-init via `/etc/docker/daemon.json`
(`{ "log-driver": "json-file", "log-opts": { "max-size": "10m", "max-file": "3" } }`) —
~30 MB/container, oldest rotated out. Docker caps by **size**, not time.

## Telemetry — on-demand Aspire dashboard ($0)

In-memory telemetry UI. **Nothing is persisted → $0, and it self-evicts.** Off by default to
save RAM; start it only when investigating.

```bash
# On the VM: point the app at the dashboard, then start it.
nano .env        # set OTEL_ENDPOINT=http://aspire-dashboard:18889
docker compose up -d webapp                       # picks up the new OTEL endpoint
docker compose --profile debug up -d aspire-dashboard

# From your dev machine: tunnel the loopback-bound UI and open it.
ssh -L 18888:localhost:18888 azureuser@<VM IP>    # then browse http://localhost:18888

# When done — free the RAM and stop exporting.
docker compose stop aspire-dashboard
nano .env        # clear OTEL_ENDPOINT
docker compose up -d webapp
```

**RAM:** ~150–250 MB while running. On a 4 GB B2s that's tight alongside SQL — fine for short
bursts; move to `Standard_B2ms` (8 GB) if you want it always on.
**Security:** runs `AUTHMODE=Unsecured` bound to `127.0.0.1` only — **never** published to the
public NSG. Always reach it through the SSH tunnel above. Do not add it to the `Caddyfile`.

## Storage (attachments) — FUTURE USE

No Mentoory code consumes object storage today; `provision-storage.sh` and the
`STORAGE_PROVIDER`/`BLOB_CONNECTION` env are **provisioned-but-not-yet-consumed** plumbing for
when an attachments feature lands. The deployment is fully functional without them. When that
feature exists: run `provision-storage.sh`, put the printed endpoint in `.env` (managed-identity
auth, no secret on disk), and `docker compose up -d webapp`. The script grants
`Storage Blob Data Contributor`; revisit the role if the feature needs container-ACL asserts.

## Backups (you own them now)

```bash
# On the VM — schedule nightly 03:30 SQL + storage backup, keep 7 days:
chmod +x ~/app/deploy/vm/backup.sh
( crontab -l 2>/dev/null; echo "30 3 * * * ~/app/deploy/vm/backup.sh >> ~/backup.log 2>&1" ) | crontab -
```

Restore: copy a `*.bak` into the `mssql` container and `RESTORE DATABASE`. For off-VM
durability, uncomment the `az storage blob upload-batch` line in `backup.sh`.

## Power schedule (cut cost ~half)

Deallocate the VM off-hours — compute billing stops while stopped (disk + static IP still
bill, ~$8/mo floor). `provision-schedule.sh` sets up **auto-start Mon–Fri 06:45** (Azure
Automation runbook + managed identity) and **auto-stop daily 19:00** (DevTest schedule),
Costa Rica time. Containers auto-recover on boot (`restart: unless-stopped`).
Always-on ~$40/mo → scheduled ~$20/mo.

```bash
./provision-schedule.sh provision   # create/refresh both schedules (idempotent)
./provision-schedule.sh start       # start now (after-hours testing) — runs until next 19:00
./provision-schedule.sh stop        # deallocate now (stop billing early)
./provision-schedule.sh status      # power state
./provision-schedule.sh disable     # pause both schedules; enable = resume
```

Static IP persists across stop/start, so DNS + TLS are unaffected. Configure
times/timezone/weekdays via env (`START_TIME`, `STOP_TIME`, `TIMEZONE_IANA`, `WEEKDAYS`, …).

## Optional: literal cost kill-switch

Fixed VM cost already removes surprises, but to make the site **stop** at a threshold:

1. Create a budget on `rg-Mentoory-D`.
2. Budget alert → Action Group → Automation Runbook running
   `az vm deallocate -g rg-Mentoory-D -n vm-mentoory-dev`.

Deallocated VM = $0 compute (you keep paying only disk + IP). The site goes down until you
`az vm start` — exactly the "stop rather than grow" behavior.

## Decommission the old Aspire / Azure-SQL path

Once this VM serves traffic, tear down the prior serverless stack to stop double-billing
(Container Apps, Azure SQL, its Storage, Log Analytics, ACR). Keep a final dacpac + data
export first. The Aspire AppHost stays in the repo for **local dev** — it is not used by this
deployment (the webapp container runs standalone from environment configuration).

## Files

| File | Purpose |
|---|---|
| `provision-vm.sh` | Create the RG (if missing) + VM + NSG (dev machine, needs `az`). |
| `provision-storage.sh` | Create the attachments Blob account + grant the VM managed identity (FUTURE USE). |
| `cloud-init.yaml` | First-boot Docker install + log cap + host firewall. |
| `docker-compose.yml` | caddy + webapp + mssql services (+ aspire-dashboard under the `debug` profile). |
| `Caddyfile` | Domain + auto-TLS reverse proxy (`{$APP_DOMAIN}`). |
| `.env.example` | Secrets/config template — copy to `.env` on the VM. |
| `deploy.sh` | Idempotent deploy/update — sync + build + recreate (dev machine). First deploy and every update. |
| `publish-dacpac-vm.sh` | Publish the `MentooryDb` schema via SSH tunnel (dev machine; or via `deploy.sh --schema`). |
| `backup.sh` | Nightly SQL + storage backup (cron on the VM). |
| `provision-schedule.sh` | Auto start/stop power schedule + manual `start`/`stop`/`status` control (dev machine). |
