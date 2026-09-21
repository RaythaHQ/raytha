#!/usr/bin/env bash
# Bring up tools/compose.yaml without failing when a published port is already
# taken (a shared SQL Server, MailHog, Redis, etc.). Docker Compose exits 1 on
# the first bind conflict, which is why F5 used to fail once then succeed.
set -euo pipefail
cd "$(dirname "$0")/.."

COMPOSE=(docker compose -f tools/compose.yaml)

port_listening() {
  ss -Hltn "sport = :$1" | grep -q .
}

ours_on_port() {
  local port="$1" id
  for id in $("${COMPOSE[@]}" ps -q 2>/dev/null); do
    docker inspect "$id" --format '{{json .NetworkSettings.Ports}}' \
      | grep -q "\"HostPort\":\"${port}\"" && return 0
  done
  return 1
}

mapfile -t to_start < <("${COMPOSE[@]}" config --format json | python3 -c '
import json, sys
cfg = json.load(sys.stdin)
for name, svc in cfg.get("services", {}).items():
    published = []
    for p in svc.get("ports") or []:
        if isinstance(p, dict) and p.get("published") is not None:
            published.append(str(p["published"]))
    print(name + " " + " ".join(published))
')

start=()
for entry in "${to_start[@]}"; do
  # shellcheck disable=SC2086
  set -- $entry
  service="$1"
  shift
  if [ "$#" -eq 0 ]; then
    start+=("$service")
    continue
  fi
  skip=0
  for port in "$@"; do
    if port_listening "$port" && ! ours_on_port "$port"; then
      echo "Skipping ${service}: host port ${port} is already in use."
      skip=1
      break
    fi
  done
  if [ "$skip" -eq 0 ]; then
    start+=("$service")
  fi
done

if [ "${#start[@]}" -eq 0 ]; then
  echo "All compose host ports are already in use; nothing to start."
  exit 0
fi

if ! "${COMPOSE[@]}" up -d "${start[@]}"; then
  # Docker can report "port is already allocated" while an endpoint is still
  # tearing down; a second up then succeeds.
  "${COMPOSE[@]}" up -d "${start[@]}"
fi
