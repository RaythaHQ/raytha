#!/usr/bin/env bash
# Headless Chromium against the verification host. See browse.cjs for options.
#
#   scripts/browse.sh --out evidence/<change> /raytha/users /raytha/webhooks
#   scripts/browse.sh --out evidence/<change> --steps evidence/<change>/steps.cjs
set -euo pipefail

if [ -z "${NODE_PATH:-}" ]; then
  pw="$(command -v playwright || true)"
  [ -n "$pw" ] || { echo "playwright CLI not on PATH (mise use -g npm:playwright, then playwright install chromium)" >&2; exit 1; }
  NODE_PATH="$(cd "$(dirname "$(readlink -f "$pw")")/.." && pwd)"
  export NODE_PATH
fi
exec node "$(dirname "$0")/browse.cjs" "$@"
