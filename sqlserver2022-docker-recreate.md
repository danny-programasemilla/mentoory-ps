# SQL Server 2022 in Docker – Quick Recreate Guide

This guide captures the exact steps to (re)create your SQL Server 2022 container on Ubuntu with data **persisted** across restarts, and **manual start** (i.e., it won’t auto‑start on login unless you start it).

---

## 1) Prerequisites

- Docker Engine **and** Docker Desktop installed.
- Expose port **1433** (default SQL Server port).
- Choose a strong SA password (below uses the one you tested).

> Replace the password everywhere if you change it later.

```bash
# (Optional) Verify Docker is working
docker version
```

---

## 2) Create a persistent volume for SQL data

Using a named volume keeps your databases even when you remove/recreate the container.

```bash
docker volume create mssql_data
```

(You can see it later with `docker volume ls`.)

---

## 3) Run SQL Server 2022 container

```bash
docker run -d   --name sql2022   -e "ACCEPT_EULA=Y"   -e "MSSQL_SA_PASSWORD=UrStrongPa55w0rd"   -p 1433:1433   -v mssql_data:/var/opt/mssql   mcr.microsoft.com/mssql/server:2022-latest
```

**What this does**

- `--name sql2022` → the container name you’ll use to start/stop.
- `-p 1433:1433` → maps host TCP 1433 → container 1433.
- `-v mssql_data:/var/opt/mssql` → keeps data files on a Docker volume.
- Image: `mcr.microsoft.com/mssql/server:2022-latest` (SQL Server 2022).

> If you prefer *not* to auto‑start with Docker Desktop login, leave Docker Desktop’s “Start Docker Desktop when you log in” disabled and start the container manually (see below).

---

## 4) Start/Stop/Remove

```bash
# Stop
docker stop sql2022

# Start (manual, when you need it)
docker start sql2022

# See logs
docker logs -f sql2022

# Remove container (data stays in mssql_data volume)
docker rm -f sql2022
```

To completely wipe data too:
```bash
docker rm -f sql2022
docker volume rm mssql_data
```

---

## 5) Optional: Host folder for backups

If you want easy .bak import/export, mount a host folder as the SQL backup dir:

```bash
mkdir -p ~/sqlbackups

docker run -d   --name sql2022   -e "ACCEPT_EULA=Y"   -e "MSSQL_SA_PASSWORD=UrStrongPa55w0rd"   -p 1433:1433   -v mssql_data:/var/opt/mssql   -v ~/sqlbackups:/var/opt/mssql/backup   mcr.microsoft.com/mssql/server:2022-latest
```

Then copy backups to `~/sqlbackups` on the host. Inside the container that path is `/var/opt/mssql/backup`.

---

## 6) Test connectivity from the container

```bash
docker exec -it sql2022 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'UrStrongPa55w0rd' -C -Q "SELECT @@VERSION;"
```

- `-C` trusts the self‑signed cert (matches client setting `TrustServerCertificate=true`).

---

## 7) .NET connection string

Use **SQL authentication**, do **not** use `Trusted_Connection=True` on Linux (that triggers SSPI/Kerberos errors).

```text
Server=localhost,1433;Database=mentooryDb;User Id=sa;Password=UrStrongPa55w0rd;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

If you prefer explicit address:
```text
Server=127.0.0.1,1433;Database=mentooryDb;User Id=sa;Password=UrStrongPa55w0rd;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

---

## 8) DBeaver connection (SQL Server)

- **Server Host**: `localhost`
- **Port**: `1433`
- **Database**: `mentooryDb` (or master first time)
- **Authentication**: SQL Server
- **User**: `sa`
- **Password**: `UrStrongPa55w0rd`
- **SSL**: Enable TLS and set **Trust server certificate** (or in Driver properties set `encrypt=true` & `trustServerCertificate=true`).

---

## 9) docker‑compose (optional)

Create `docker-compose.yml`:

```yaml
services:
  sql2022:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sql2022
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=UrStrongPa55w0rd
    ports:
      - "1433:1433"
    volumes:
      - mssql_data:/var/opt/mssql
      # - ./sqlbackups:/var/opt/mssql/backup   # optional host folder
    restart: "no"   # do not auto-restart; start manually

volumes:
  mssql_data:
```

Run:
```bash
docker compose up -d
docker compose stop
docker compose start
```

---

## 10) Common issues

- **“The target principal name is incorrect. Cannot generate SSPI context.”**  
  Remove `Trusted_Connection=True`; use SQL login with `Encrypt=True;TrustServerCertificate=True` (as above).

- **Port in use**: ensure nothing else is bound to 1433 (`sudo lsof -i :1433`).

- **Container running but can’t connect**: check logs  
  ```bash
  docker logs -f sql2022
  ```

---

**That’s it!** This file is safe to keep in your repo/notes for quick re‑creation.
