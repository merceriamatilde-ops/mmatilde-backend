#!/usr/bin/env bash
# Post-deploy idempotente: seed de colores + recálculo de títulos Makor cortados.
set -euo pipefail

CFG=/etc/mmatilde/appsettings.json
SCRIPTS=/var/www/mmatilde-backend/scripts
API_URL="${MMATILDE_API_URL:-https://api.merceriamatilde.com}"

if [[ ! -f "$CFG" ]]; then
  echo "No se encontró $CFG — omitiendo mantenimiento."
  exit 0
fi

CONN=$(python3 -c "import json; print(json.load(open('$CFG'))['ConnectionStrings']['DefaultConnection'])")

echo "→ Seed colores…"
psql "$CONN" -v ON_ERROR_STOP=1 -f "$SCRIPTS/seed_colores.sql"
echo "✓ Colores OK"

echo "→ Esperando API…"
sleep 5

python3 <<PY
import json, urllib.request, urllib.error, os

cfg_path = "/etc/mmatilde/appsettings.json"
api_url = os.environ.get("MMATILDE_API_URL", "https://api.merceriamatilde.com")

with open(cfg_path) as f:
    cfg = json.load(f)

password = (cfg.get("AdminPassword") or "").strip()
if not password:
    print("AdminPassword vacío — omitiendo recalcular títulos.")
    raise SystemExit(0)

email = (cfg.get("AdminEmail") or "admin@mmatilde.com").strip()
login_body = json.dumps({"email": email, "password": password}).encode()

req = urllib.request.Request(
    f"{api_url}/api/auth/login",
    data=login_body,
    headers={"Content-Type": "application/json"},
    method="POST",
)
with urllib.request.urlopen(req, timeout=30) as r:
    token = json.load(r).get("token", "")

if not token:
    print("No se pudo obtener token admin — omitiendo recalcular títulos.")
    raise SystemExit(0)

print("→ Recalcular títulos Makor cortados…")
req2 = urllib.request.Request(
    f"{api_url}/api/productos/mantenimiento/recalcular-titulos",
    data=b"",
    headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"},
    method="POST",
)
with urllib.request.urlopen(req2, timeout=120) as r:
    result = json.load(r)
    print(f"✓ Recalcular OK — corregidos: {result.get('corregidos', '?')}")
PY

echo "✓ Mantenimiento post-deploy completo"
