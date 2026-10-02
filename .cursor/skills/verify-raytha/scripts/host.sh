#!/usr/bin/env bash
# Launch an isolated Raytha host for verification.
#
#   scripts/host.sh [--db NAME] [--port N] [--no-build] [--fresh] [-- --Key=value ...]
#
#   --db NAME    Postgres database (default raytha_verify). "raytha" is refused.
#   --port N     Loopback port (default 15200). 5200 is refused.
#   --no-build   Reuse the last snapshot in runs/bin instead of building.
#   --fresh      Drop the database and the uploads dir first (true first run).
#   -- ...       Extra host settings, e.g. -- --ALLOW_INTERNAL_URL_IMPORTS=true.
#                The connection string and URLs cannot be overridden.
#
# Builds src/Raytha.Web, snapshots bin/Debug/net10.0 into runs/bin so a
# concurrent `dotnet watch` cannot swap binaries under us, starts the snapshot
# from src/Raytha.Web, waits for /healthz/ready, and records the pid.
set -euo pipefail

SKILL="$(cd "$(dirname "$0")/.." && pwd)"
REPO="$(cd "$SKILL/../../.." && pwd)"
RUNS="$SKILL/runs"
DB=raytha_verify
PORT=15200
BUILD=1
FRESH=0
EXTRA=()

while [ $# -gt 0 ]; do
  case "$1" in
    --db) DB="$2"; shift 2 ;;
    --port) PORT="$2"; shift 2 ;;
    --no-build) BUILD=0; shift ;;
    --fresh) FRESH=1; shift ;;
    --) shift; EXTRA=("$@"); break ;;
    *) echo "unknown argument: $1" >&2; exit 2 ;;
  esac
done
for arg in ${EXTRA[@]+"${EXTRA[@]}"}; do
  case "$arg" in
    *ConnectionStrings*|--urls*|*ASPNETCORE_URLS*) echo "refusing extra setting: $arg" >&2; exit 2 ;;
  esac
done

[ "$DB" = "raytha" ] && { echo "refusing: raytha is the developer's database" >&2; exit 2; }
[ "$PORT" = "5200" ] && { echo "refusing: 5200 is the developer's instance" >&2; exit 2; }
case "$RUNS" in *[A-Z]*) echo "refusing: $RUNS has uppercase; local file storage lowercases paths" >&2; exit 2 ;; esac

mkdir -p "$RUNS"
PIDFILE="$RUNS/host-$PORT.pid"
LOG="$RUNS/host-$PORT.log"

if [ -f "$PIDFILE" ]; then
  old="$(cat "$PIDFILE")"
  if kill -0 "$old" 2>/dev/null; then kill "$old"; sleep 2; fi
  rm -f "$PIDFILE"
fi
holder="$(ss -Hltnp "sport = :$PORT" | sed -n 's/.*pid=\([0-9]*\).*/\1/p' | head -1)"
if [ -n "$holder" ]; then
  echo "refusing: port $PORT is held by pid $holder, which this skill did not start" >&2
  ps -o pid,cmd -p "$holder" >&2 || true
  exit 1
fi

"$REPO/tools/compose-up.sh" >/dev/null

if [ "$FRESH" = 1 ]; then
  docker compose -f "$REPO/tools/compose.yaml" exec -T postgres \
    dropdb -U postgres --if-exists --force "$DB"
  rm -rf "$RUNS/uploads"
fi
mkdir -p "$RUNS/uploads"

if [ "$BUILD" = 1 ]; then
  dotnet build "$REPO/src/Raytha.Web/Raytha.Web.csproj" -nologo -v q -clp:ErrorsOnly
  rm -rf "$RUNS/bin"
  cp -r "$REPO/src/Raytha.Web/bin/Debug/net10.0" "$RUNS/bin"
fi
[ -x "$RUNS/bin/Raytha.Web" ] || { echo "no snapshot in $RUNS/bin; run without --no-build" >&2; exit 1; }

cd "$REPO/src/Raytha.Web"
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS="http://127.0.0.1:$PORT" \
nohup "$RUNS/bin/Raytha.Web" \
  --urls "http://127.0.0.1:$PORT" \
  --ConnectionStrings:DefaultConnection="Host=localhost;Port=5433;Username=postgres;Password=changeme;Database=$DB" \
  --APPLY_PENDING_MIGRATIONS=true \
  --AdminSpa:AutoStart=false \
  --AdminSpa:DevServerUrl=http://127.0.0.1:1 \
  --SMTP_HOST=localhost \
  --SMTP_PORT=1025 \
  --FILE_STORAGE_PROVIDER=local \
  --FILE_STORAGE_LOCAL_DIRECTORY="$RUNS/uploads" \
  ${EXTRA[@]+"${EXTRA[@]}"} \
  > "$LOG" 2>&1 &
echo $! > "$PIDFILE"

code=000
for _ in $(seq 1 60); do
  code="$(curl -s -o /dev/null -w '%{http_code}' "http://127.0.0.1:$PORT/healthz/ready" || true)"
  [ "$code" = 200 ] && break
  kill -0 "$(cat "$PIDFILE")" 2>/dev/null || { echo "host exited; tail of $LOG:" >&2; tail -30 "$LOG" >&2; exit 1; }
  sleep 1
done
[ "$code" = 200 ] || { echo "not ready after 60s (last $code); see $LOG" >&2; exit 1; }

version="$(curl -s "http://127.0.0.1:$PORT/healthz" | python3 -c 'import sys,json;print(json.load(sys.stdin).get("version"))')"
echo "ready base=http://127.0.0.1:$PORT db=$DB pid=$(cat "$PIDFILE") version=$version log=$LOG"
