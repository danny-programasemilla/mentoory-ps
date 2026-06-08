#!/usr/bin/env bash
# Publish the MentooryDb dacpac to the VM's SQL Server container.
# Run from your DEV MACHINE. The mssql container binds 1433 to the VM's loopback only,
# so we reach it through an SSH tunnel — SQL is never exposed publicly.
#
# Usage:
#   ./publish-dacpac-vm.sh <vm-ip-or-host> [--skip-build]
#
# Env:
#   ADMIN_USER          SSH user (default: azureuser)
#   MSSQL_SA_PASSWORD   SA password (must match the VM's .env)
#   DACPAC_CONFIG       Build config (default: Release)
set -euo pipefail

VM_HOST="${1:?Usage: publish-dacpac-vm.sh <vm-ip-or-host> [--skip-build]}"
SKIP_BUILD="false"
[[ "${2:-}" == "--skip-build" ]] && SKIP_BUILD="true"

ADMIN="${ADMIN_USER:-azureuser}"
CONFIG="${DACPAC_CONFIG:-Release}"
: "${MSSQL_SA_PASSWORD:?Set MSSQL_SA_PASSWORD (same value as the VM .env)}"

command -v sqlpackage >/dev/null || { echo "sqlpackage missing. Install: dotnet tool install -g microsoft.sqlpackage" >&2; exit 1; }

REPO_ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
cd "$REPO_ROOT"
DB_PROJECT="Mentoory.Db/MentooryDb.sqlproj"
DACPAC="Mentoory.Db/bin/${CONFIG}/MentooryDb.dacpac"
LOCAL_PORT="${LOCAL_PORT:-14333}"

if [[ "$SKIP_BUILD" != "true" ]]; then
  echo "== Building dacpac ($CONFIG) =="
  dotnet build "$DB_PROJECT" -c "$CONFIG"
fi
# The SDK may place the dacpac under a slightly different path; resolve it robustly.
if [[ ! -f "$DACPAC" ]]; then
  DACPAC="$(find "Mentoory.Db/bin/${CONFIG}" -maxdepth 2 -name '*.dacpac' -type f 2>/dev/null | head -1 || true)"
fi
[[ -f "$DACPAC" ]] || { echo "dacpac not found under Mentoory.Db/bin/${CONFIG}" >&2; exit 1; }
echo "== Using dacpac: $DACPAC =="

echo "== Opening SSH tunnel localhost:$LOCAL_PORT -> $VM_HOST:1433 =="
ssh -f -N -L "${LOCAL_PORT}:localhost:1433" "${ADMIN}@${VM_HOST}"
TUNNEL_PID="$(pgrep -f "ssh -f -N -L ${LOCAL_PORT}:localhost:1433" || true)"
cleanup() { [[ -n "${TUNNEL_PID:-}" ]] && kill "$TUNNEL_PID" 2>/dev/null || true; }
trap cleanup EXIT

# Give the tunnel a moment to come up.
for _ in $(seq 1 10); do (echo > "/dev/tcp/127.0.0.1/${LOCAL_PORT}") 2>/dev/null && break; sleep 1; done

echo "== Publishing schema to MentooryDb =="
sqlpackage /Action:Publish \
  /SourceFile:"$DACPAC" \
  /TargetServerName:"127.0.0.1,${LOCAL_PORT}" \
  /TargetDatabaseName:"MentooryDb" \
  /TargetUser:"sa" \
  /TargetPassword:"$MSSQL_SA_PASSWORD" \
  /TargetTrustServerCertificate:True \
  /TargetEncryptConnection:True \
  /p:BlockOnPossibleDataLoss=false

echo "== Schema published =="
