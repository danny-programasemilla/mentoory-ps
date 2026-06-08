#!/usr/bin/env bash
# Provision an Azure Storage account for attachments and grant the VM's managed identity
# access — so a FUTURE attachments feature can authenticate with NO secret on disk (the
# Blob endpoint URI is all that goes in .env).
#
# ⚠️ FUTURE USE: No Mentoory code consumes blob storage today. The deployment runs fully
# without this script. Run it only when an attachments feature lands.
#
# Run AFTER provision-vm.sh (the VM must exist to receive the identity grant).
#
# Storage cost is trivial (Standard_LRS hot ~$0.018/GB-mo + tiny per-transaction) and Blob
# survives VM loss, which a LocalFilesystem provider does not.
set -euo pipefail

RG="${RESOURCE_GROUP:-rg-Mentoory-D}"
LOC="${LOCATION:-centralus}"
VM="${VM_NAME:-vm-mentoory-dev}"
# Storage account names are GLOBALLY unique, 3-24 chars, lowercase alphanumeric.
# Override if this one is taken.
ACCT="${STORAGE_ACCOUNT:-stmentoorydev}"

command -v az >/dev/null || { echo "az CLI missing." >&2; exit 1; }
az account show >/dev/null 2>&1 || { echo "Not logged in. Run: az login" >&2; exit 1; }

echo "== Creating storage account $ACCT in $RG / $LOC =="
az storage account create \
  -n "$ACCT" -g "$RG" -l "$LOC" \
  --sku Standard_LRS \
  --kind StorageV2 \
  --min-tls-version TLS1_2 \
  --allow-blob-public-access false \
  -o none

echo "== Ensuring the VM has a system-assigned managed identity =="
az vm identity assign -g "$RG" -n "$VM" -o none
PID="$(az vm show -g "$RG" -n "$VM" --query identity.principalId -o tsv)"
[[ -n "$PID" ]] || { echo "Could not resolve VM identity principalId." >&2; exit 1; }

SCOPE="$(az storage account show -n "$ACCT" -g "$RG" --query id -o tsv)"
# Data Contributor is sufficient for read/write blob access. (The bds-ps reference needed
# Data Owner because its app asserts container ACLs via GetAccessPolicy; Mentoory has no
# such consumer yet — revisit the role when an attachments feature defines its needs.)
echo "== Granting 'Storage Blob Data Contributor' to the VM identity =="
az role assignment create \
  --assignee-object-id "$PID" \
  --assignee-principal-type ServicePrincipal \
  --role "Storage Blob Data Contributor" \
  --scope "$SCOPE" \
  -o none
echo "   (RBAC can take a few minutes to propagate before blob auth succeeds.)"

ENDPOINT="$(az storage account show -n "$ACCT" -g "$RG" --query primaryEndpoints.blob -o tsv)"
cat <<EOF

== Done ==
Blob endpoint: $ENDPOINT

FUTURE USE — when an attachments feature exists, put these in the VM's deploy/vm/.env
(managed-identity auth, no secret), then recreate the webapp:
    STORAGE_PROVIDER=AzureBlob
    BLOB_CONNECTION=$ENDPOINT
    docker compose up -d webapp

The webapp container reaches the VM's managed identity via Azure IMDS (169.254.169.254),
normally routable from the default docker bridge. If blob auth fails with a credential
error, fall back to a key connection string in BLOB_CONNECTION.
EOF
