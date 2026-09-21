#!/usr/bin/env bash
# One-shot dev environment: containers + dotnet watch (which auto-starts admin Vite).
# Browse http://localhost:5200, or http://<tailscale-ip>:5200 from another device.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ "${SKIP_COMPOSE:-0}" != "1" ]; then
  ./tools/compose-up.sh
fi

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:5200}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=localhost;Port=5433;Username=postgres;Password=changeme;Database=raytha}"
export APPLY_PENDING_MIGRATIONS="${APPLY_PENDING_MIGRATIONS:-true}"
export SMTP_HOST="${SMTP_HOST:-localhost}"
export SMTP_PORT="${SMTP_PORT:-1025}"
export AdminSpa__DevServerUrl="${AdminSpa__DevServerUrl:-http://localhost:5203}"
export AdminSpa__AutoStart="${AdminSpa__AutoStart:-true}"

echo "Local:     http://localhost:5200  (admin at /raytha)"
if command -v tailscale >/dev/null 2>&1; then
  ts_ip="$(tailscale ip -4 2>/dev/null || true)"
  if [ -n "${ts_ip}" ]; then
    echo "Tailscale: http://${ts_ip}:5200  (admin at /raytha)"
  fi
fi

dotnet watch --project src/Raytha.Web
