#!/usr/bin/env bash
# Read-only: is the verification host worth driving?
#
#   scripts/doctor.sh [--port N]
#
# Exits non-zero on the first failed check. Changes nothing.
set -uo pipefail

SKILL="$(cd "$(dirname "$0")/.." && pwd)"
REPO="$(cd "$SKILL/../../.." && pwd)"
RUNS="$SKILL/runs"
PORT=15200
[ "${1:-}" = "--port" ] && PORT="$2"
BASE="http://127.0.0.1:$PORT"
PIDFILE="$RUNS/host-$PORT.pid"

fail() { echo "FAIL $*"; exit 1; }
ok() { echo "ok   $*"; }

[ "$PORT" != 5200 ] || fail "5200 is the developer's instance"
[ -f "$PIDFILE" ] || fail "no $PIDFILE; launch with scripts/host.sh"
pid="$(cat "$PIDFILE")"
kill -0 "$pid" 2>/dev/null || fail "recorded pid $pid is not running"
holder="$(ss -Hltnp "sport = :$PORT" | sed -n 's/.*pid=\([0-9]*\).*/\1/p' | head -1)"
[ "$holder" = "$pid" ] || fail "port $PORT is held by pid '${holder:-nobody}', not our pid $pid"
ok "pid $pid owns 127.0.0.1:$PORT"

db="$(tr '\0' '\n' < "/proc/$pid/cmdline" | sed -n 's/.*Database=\([^;]*\).*/\1/p' | head -1)"
[ -n "$db" ] && [ "$db" != raytha ] || fail "host is attached to database '${db:-unknown}'"
ok "database $db"

ready="$(curl -s "$BASE/healthz/ready")"
echo "$ready" | python3 -c 'import sys,json; sys.exit(json.load(sys.stdin)["status"]!="Healthy")' \
  || fail "/healthz/ready: $ready"
ok "/healthz/ready Healthy"

running="$(curl -s "$BASE/healthz" | python3 -c 'import sys,json;print(json.load(sys.stdin).get("version"))')"
want="$(cat "$REPO/VERSION")"
[ "$running" = "$want" ] || fail "host reports $running, VERSION is $want (stale build: rerun host.sh)"
ok "version $running"

if rg -q "Admin Vite already running" "$RUNS/host-$PORT.log" 2>/dev/null; then
  fail "host is proxying /raytha to a Vite dev server, not serving the committed bundle"
fi
title="$(curl -s "$BASE/raytha/users" | rg -o '<title>[^<]*</title>')"
[ "$title" = "<title>Raytha Admin</title>" ] || fail "/raytha/users is not the SPA bundle: '$title'"
ok "/raytha/users serves the committed SPA bundle"

code="$(curl -s -o /dev/null -w '%{http_code}' "$BASE/raytha/api/auth/setup/status")"
[ "$code" = 200 ] || fail "/raytha/api/auth/setup/status answered $code"
required="$(curl -s "$BASE/raytha/api/auth/setup/status" | python3 -c 'import sys,json;print(json.load(sys.stdin).get("required"))')"
ok "setup required=$required (run scripts/session.sh to set up or sign in)"
