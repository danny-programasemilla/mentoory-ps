#!/usr/bin/env bash
#
# sqlserver2022-docker-recreate.sh
#
# Performs steps 2 and 3 of sqlserver2022-docker-recreate.md:
#   2) create a persistent named volume for SQL data
#   3) run the SQL Server 2022 container
#
# On a FRESH container (first run or --reset) it waits for SQL Server to come
# up, then applies the database schema and seed data by running
# Mentoory.Db/publish-mentoorydb.sh -p.
#
# Safe to run repeatedly: respects an existing volume and container.
#   - Volume missing      -> created
#   - Container missing    -> created, started, schema+seed published
#   - Container stopped    -> started (no publish; data preserved)
#   - Container running    -> left as-is (no publish)
#
# Pass --reset to remove the container AND the volume first, then recreate
# everything from scratch (DESTROYS ALL DATABASE DATA).
#
# Usage:
#   ./sqlserver2022-docker-recreate.sh              # idempotent (re)create + publish if fresh
#   ./sqlserver2022-docker-recreate.sh --reset      # wipe data + recreate + publish
#   ./sqlserver2022-docker-recreate.sh --no-publish # skip the schema/seed publish step
#   ./sqlserver2022-docker-recreate.sh -h           # help
#
# Override defaults via environment variables, e.g.:
#   MSSQL_SA_PASSWORD='MySecret123!' ./sqlserver2022-docker-recreate.sh

set -euo pipefail

# ---- Configuration (override via environment) -------------------------------
CONTAINER_NAME="${CONTAINER_NAME:-sql2022}"
VOLUME_NAME="${VOLUME_NAME:-mssql_data}"
HOST_PORT="${HOST_PORT:-1433}"
IMAGE="${IMAGE:-mcr.microsoft.com/mssql/server:2022-latest}"
MSSQL_SA_PASSWORD="${MSSQL_SA_PASSWORD:-UrStrongPa55w0rd}"
DB_NAME="${DB_NAME:-mentooryDb}"

RESET=0
NO_PUBLISH=0
CREATED=0
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_SCRIPT="${SCRIPT_DIR}/Mentoory.Db/publish-mentoorydb.sh"

# ---- Helpers ----------------------------------------------------------------
log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m!  \033[0m %s\n' "$*" >&2; }
die()  { printf '\033[1;31mxx \033[0m %s\n' "$*" >&2; exit 1; }

usage() {
  cat <<'EOF'
sqlserver2022-docker-recreate.sh — (re)create the SQL Server 2022 container.

Performs steps 2 and 3 of sqlserver2022-docker-recreate.md (volume + container).
On a FRESH container (first run or --reset) it waits for SQL Server, then applies
the database schema and seed data via Mentoory.Db/publish-mentoorydb.sh -p.

Usage:
  ./sqlserver2022-docker-recreate.sh              # idempotent (re)create + publish if fresh
  ./sqlserver2022-docker-recreate.sh --reset      # wipe data + recreate + publish
  ./sqlserver2022-docker-recreate.sh --no-publish # skip the schema/seed publish step
  ./sqlserver2022-docker-recreate.sh -h           # help

Behavior (idempotent):
  Volume missing    -> created
  Container missing  -> created, started, schema+seed published
  Container stopped  -> started (no publish; data preserved)
  Container running  -> left as-is (no publish)

Override defaults via env vars: CONTAINER_NAME, VOLUME_NAME, HOST_PORT, IMAGE,
MSSQL_SA_PASSWORD, DB_NAME.
EOF
  exit 0
}

container_exists() { docker ps -a --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"; }
container_running() { docker ps    --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"; }
volume_exists()    { docker volume inspect "$VOLUME_NAME" >/dev/null 2>&1; }

# Block until SQL Server accepts connections (or fail after ~120s).
wait_for_sql() {
  local tries=60 i
  log "Waiting for SQL Server to accept connections..."
  for ((i = 1; i <= tries; i++)); do
    if docker exec "$CONTAINER_NAME" /opt/mssql-tools18/bin/sqlcmd \
         -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
      log "SQL Server is ready."
      return 0
    fi
    sleep 2
  done
  die "SQL Server not ready after $((tries * 2))s. Check: docker logs $CONTAINER_NAME"
}

# ---- Argument parsing -------------------------------------------------------
while [[ $# -gt 0 ]]; do
  case "$1" in
    --reset)      RESET=1; shift ;;
    --no-publish) NO_PUBLISH=1; shift ;;
    -h|--help)    usage ;;
    *)            die "Unknown argument: $1 (use -h for help)" ;;
  esac
done

# ---- Preflight --------------------------------------------------------------
command -v docker >/dev/null 2>&1 || die "docker not found on PATH."
docker info >/dev/null 2>&1 || die "Docker daemon not reachable. Is Docker running?"

# ---- Reset (destructive) ----------------------------------------------------
if [[ "$RESET" -eq 1 ]]; then
  warn "RESET requested: removing container '$CONTAINER_NAME' and volume '$VOLUME_NAME' (all data lost)."
  if container_exists; then
    log "Removing container '$CONTAINER_NAME'."
    docker rm -f "$CONTAINER_NAME" >/dev/null
  fi
  if volume_exists; then
    log "Removing volume '$VOLUME_NAME'."
    docker volume rm "$VOLUME_NAME" >/dev/null
  fi
fi

# ---- Step 2: persistent volume ----------------------------------------------
if volume_exists; then
  log "Volume '$VOLUME_NAME' already exists. Keeping it."
else
  log "Creating volume '$VOLUME_NAME'."
  docker volume create "$VOLUME_NAME" >/dev/null
fi

# ---- Step 3: container ------------------------------------------------------
if container_running; then
  log "Container '$CONTAINER_NAME' already running. Nothing to do."
elif container_exists; then
  log "Container '$CONTAINER_NAME' exists but is stopped. Starting it."
  docker start "$CONTAINER_NAME" >/dev/null
else
  log "Creating and starting container '$CONTAINER_NAME' from '$IMAGE'."
  docker run -d \
    --name "$CONTAINER_NAME" \
    -e "ACCEPT_EULA=Y" \
    -e "MSSQL_SA_PASSWORD=${MSSQL_SA_PASSWORD}" \
    -p "${HOST_PORT}:1433" \
    -v "${VOLUME_NAME}:/var/opt/mssql" \
    "$IMAGE" >/dev/null
  CREATED=1
fi

log "Container status:"
docker ps --filter "name=^/${CONTAINER_NAME}$" \
  --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'

# ---- Schema + seed on a fresh DB --------------------------------------------
if [[ "$CREATED" -eq 1 && "$NO_PUBLISH" -eq 1 ]]; then
  warn "Fresh container created, but --no-publish set: skipping schema/seed."
elif [[ "$CREATED" -eq 1 ]]; then
  [[ -f "$PUBLISH_SCRIPT" ]] || die "Publish script not found: $PUBLISH_SCRIPT"
  wait_for_sql
  log "Applying schema + seed: $PUBLISH_SCRIPT -p"
  bash "$PUBLISH_SCRIPT" -p
else
  log "Existing container reused — schema/seed left untouched."
fi

# ---- Connection info --------------------------------------------------------
cat <<EOF

Connection info
---------------
.NET connection string:
  Server=localhost,${HOST_PORT};Database=${DB_NAME};User Id=sa;Password=${MSSQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;

DBeaver (JDBC) — switch "Connect by" from Host to URL, then paste:
  jdbc:sqlserver://localhost:${HOST_PORT};databaseName=${DB_NAME};encrypt=true;trustServerCertificate=true
  (Username/Password stay separate fields: sa / ${MSSQL_SA_PASSWORD})
EOF
