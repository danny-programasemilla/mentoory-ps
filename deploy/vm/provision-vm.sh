#!/usr/bin/env bash
# Provision the lowest-cost fixed-price VM for the Mentoory single-VM deployment.
#
# Cost target (centralus, approx/month, FIXED regardless of traffic):
#   Standard_B2s (2 vCPU, 4 GB)   ~$30-38
#   64 GB StandardSSD OS disk     ~$5
#   Standard static public IP     ~$3-4
#   -------------------------------------
#   ~$40/month, predictable. No per-request, no autoscale surprise.
#
# B2s (4 GB) runs webapp + SQL + Caddy but is tight. If you see OOM kills, bump
# VM_SIZE to Standard_B2ms (8 GB) before running, or resize later:
#   az vm resize -g rg-Mentoory-D -n vm-mentoory-dev --size Standard_B2ms
set -euo pipefail

RG="${RESOURCE_GROUP:-rg-Mentoory-D}"
LOC="${LOCATION:-centralus}"
VM="${VM_NAME:-vm-mentoory-dev}"
SIZE="${VM_SIZE:-Standard_B2s}"
ADMIN="${ADMIN_USER:-azureuser}"
DISK_GB="${OS_DISK_GB:-64}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

command -v az >/dev/null || { echo "az CLI missing." >&2; exit 1; }
az account show >/dev/null 2>&1 || { echo "Not logged in. Run: az login" >&2; exit 1; }

# Public IP to lock the SSH NSG rule to. Override with MYIP=... ; otherwise try several
# echo services (some networks block any single one).
MYIP="${MYIP:-}"
for _svc in https://api.ipify.org https://checkip.amazonaws.com https://ifconfig.me; do
  [[ -n "$MYIP" ]] && break
  MYIP="$(curl -fsS --max-time 8 "$_svc" 2>/dev/null | tr -d '[:space:]')"
done
[[ -n "$MYIP" ]] || { echo "Could not determine public IP. Re-run with MYIP=<your.ip>" >&2; exit 1; }
echo "== Provisioning $VM ($SIZE) in $RG / $LOC =="
echo "   SSH will be locked to your current IP: $MYIP"

# az vm create does NOT create the resource group. Make it idempotently so this works
# green-field (e.g. after the RG was deleted to stop billing).
echo "== Ensuring resource group $RG exists in $LOC =="
az group create -n "$RG" -l "$LOC" -o none

az vm create \
  -g "$RG" -n "$VM" -l "$LOC" \
  --image Ubuntu2404 \
  --size "$SIZE" \
  --admin-username "$ADMIN" \
  --generate-ssh-keys \
  --public-ip-sku Standard \
  --os-disk-size-gb "$DISK_GB" \
  --storage-sku StandardSSD_LRS \
  --custom-data "$SCRIPT_DIR/cloud-init.yaml" \
  --nsg-rule NONE

# az vm create makes an NSG named "${VM}NSG". Open web to the world, SSH to you only.
az network nsg rule create -g "$RG" --nsg-name "${VM}NSG" -n allow-web \
  --priority 1000 --direction Inbound --access Allow --protocol Tcp \
  --destination-port-ranges 80 443 >/dev/null
az network nsg rule create -g "$RG" --nsg-name "${VM}NSG" -n allow-ssh \
  --priority 1100 --direction Inbound --access Allow --protocol Tcp \
  --source-address-prefixes "${MYIP}/32" --destination-port-ranges 22 >/dev/null

IP="$(az vm show -d -g "$RG" -n "$VM" --query publicIps -o tsv)"
cat <<EOF

== Done ==
VM public IP: $IP

Next:
  1. DNS: point an A record
        <your APP_DOMAIN>  ->  $IP
     (Caddy needs this resolvable before it can issue the TLS cert.)
  2. Ship the deploy files to the VM (first time):
        scp -r ../../deploy $ADMIN@$IP:~/app/deploy
  3. SSH in, configure secrets, bring up DB:
        ssh $ADMIN@$IP
        cd ~/app/deploy/vm && cp .env.example .env && nano .env
        docker compose up -d mssql
  4. From your dev machine, first deploy (sync + schema + build + start):
        MSSQL_SA_PASSWORD='<same as VM .env>' ./deploy.sh $IP --schema --logs
EOF
