# Quickstart: Mentoory Single-VM Deployment

**Feature**: 022-vm-deployment

The operator runbook — zero to HTTPS-served Mentoory at a fixed cost. This is also the manual acceptance
test for SC-001. Run from a dev machine with `az`, `ssh`, `rsync`, and `sqlpackage` installed and
`az login` done.

> All names/region/size/domain/timezone are env-overridable (see contracts/env-vars.md). Defaults below.

## One-time setup

```bash
cd deploy/vm

# 1. Provision the VM (creates rg-Mentoory-D if missing, locks SSH to your IP, opens 80/443).
./provision-vm.sh
#    -> prints the VM public IP.

# 2. (Optional, future-use) Provision the attachments storage account + grant the VM identity.
#    Prints the BLOB_CONNECTION endpoint. Skip unless/until an attachments feature needs it.
./provision-storage.sh

# 3. DNS: point an A record at the printed IP and wait for it to resolve publicly:
#       mentoory-dev.<your-domain>  ->  <VM IP>
#    Caddy cannot issue the TLS cert until this resolves.

# 4. Ship the deploy files to the VM (first time only; deploy.sh syncs the repo after).
scp -r ../../deploy azureuser@<VM IP>:~/app/deploy

# 5. On the VM: configure secrets and start SQL.
ssh azureuser@<VM IP>
cd ~/app/deploy/vm
cp .env.example .env && nano .env     # set APP_DOMAIN, ACME_EMAIL, MSSQL_SA_PASSWORD,
                                      # MEDIATR_LICENSE_KEY (recommended)
docker compose up -d mssql            # wait until healthy: docker compose ps
exit

# 6. From your DEV MACHINE: first deploy — sync source, publish schema, build, start.
MSSQL_SA_PASSWORD='<same as VM .env>' ./deploy.sh <VM IP> --schema --logs
```

Visit `https://mentoory-dev.<your-domain>` — Caddy serves a valid Let's Encrypt cert automatically.
Log in as the seeded GlobalAdmin (credentials provisioned by PostDeployment `002.SeedGlobalAdmin.sql`).

## Smoke test (acceptance — SC-001, SC-009)

1. The site loads over HTTPS with a valid certificate (no warning).
2. The login page renders in Spanish.
3. Logging in as GlobalAdmin reaches the dashboard; navigation works.
4. `docker compose ps` on the VM shows `caddy`, `webapp`, `mssql` up; `mssql` healthy.
5. `curl -sko /dev/null -w '%{http_code}\n' https://mentoory-dev.<domain>/` returns 200/302.

## Day-to-day — deploy updates

```bash
./deploy.sh <VM IP>                                  # code change
MSSQL_SA_PASSWORD='…' ./deploy.sh <VM IP> --schema   # code + schema change
./deploy.sh <VM IP> --no-build                       # recreate without rebuild
./deploy.sh <VM IP> --logs                           # tail logs after
```

`deploy.sh` is idempotent and never touches the VM's `.env`.

## Logs (no cloud log cost)

```bash
docker compose logs -f webapp            # live
docker compose logs --since 30m webapp   # recent
docker compose logs --tail 200 webapp    # last N
```
Disk cap is set by cloud-init (`/etc/docker/daemon.json`: 10m × 3 per container).

## Telemetry on demand (optional, $0)

```bash
# On the VM: point the app at the dashboard and start it.
nano .env        # set OTEL_ENDPOINT=http://aspire-dashboard:18889
docker compose up -d webapp
docker compose --profile debug up -d aspire-dashboard
# From dev machine: tunnel the loopback UI.
ssh -L 18888:localhost:18888 azureuser@<VM IP>    # then open http://localhost:18888
# When done:
docker compose stop aspire-dashboard
nano .env        # clear OTEL_ENDPOINT
docker compose up -d webapp
```

## Backups

```bash
chmod +x ~/app/deploy/vm/backup.sh
( crontab -l 2>/dev/null; echo "30 3 * * * ~/app/deploy/vm/backup.sh >> ~/backup.log 2>&1" ) | crontab -
```
Restore: copy a `.bak` into the `mssql` container and `RESTORE DATABASE`.

## Power schedule (cut cost ~half)

```bash
./provision-schedule.sh provision   # auto-start weekday 06:45, auto-stop 19:00 (Costa Rica)
./provision-schedule.sh start|stop|status
./provision-schedule.sh disable|enable
```
Static IP persists across stop/start — DNS + TLS unaffected.

## Cost kill-switch (optional)

Create a budget on `rg-Mentoory-D` → alert → Action Group → Automation runbook running
`az vm deallocate -g rg-Mentoory-D -n vm-mentoory-dev`. Deallocated VM = $0 compute.

## Decommission the old Aspire/Azure-SQL path

Once the VM serves traffic, tear down the prior serverless/Azure-SQL stack to stop double-billing. Keep a
final dacpac + data export first. (The Aspire AppHost remains in the repo for local dev; it is not used by
this deployment.)

## Cost (centralus, ~fixed/month)

| Item | Choice | ~Cost |
|---|---|---|
| VM | `Standard_B2s` (2 vCPU, 4 GB) | $30–38 |
| OS disk | 64 GB StandardSSD | $5 |
| Static public IP | Standard | $3–4 |
| **Total** | | **~$40, predictable** |

B2s (4 GB) is the floor for SQL + webapp; if you hit OOM (e.g. dashboard running), set
`VM_SIZE=Standard_B2ms` (8 GB, ~$60) before provisioning, or
`az vm resize -g rg-Mentoory-D -n vm-mentoory-dev --size Standard_B2ms`.
