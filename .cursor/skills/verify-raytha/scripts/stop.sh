#!/usr/bin/env bash
# Stop the host this skill started on PORT. Leaves runs/ scratch and evidence/ alone.
#
#   scripts/stop.sh [--port N]
set -euo pipefail

SKILL="$(cd "$(dirname "$0")/.." && pwd)"
PORT=15200
[ "${1:-}" = "--port" ] && PORT="$2"
PIDFILE="$SKILL/runs/host-$PORT.pid"

if [ -f "$PIDFILE" ]; then
  pid="$(cat "$PIDFILE")"
  if kill -0 "$pid" 2>/dev/null; then
    kill "$pid"
    for _ in $(seq 1 10); do kill -0 "$pid" 2>/dev/null || break; sleep 1; done
  fi
  rm -f "$PIDFILE"
fi

holder="$(ss -Hltnp "sport = :$PORT" | sed -n 's/.*pid=\([0-9]*\).*/\1/p' | head -1)"
if [ -n "$holder" ]; then
  echo "port $PORT still held by pid $holder, which this skill did not record; not touching it" >&2
  exit 1
fi
echo "port $PORT free"
