#!/usr/bin/env bash
# Run first-run setup if the database needs it, otherwise sign in. Writes a
# cookie jar for curl and prints who you are.
#
#   scripts/session.sh [--port N]
#
# Credentials: admin@raytha.local / Verify123$ (override with VERIFY_EMAIL,
# VERIFY_PASSWORD). Jar: runs/cookies-PORT.txt
set -euo pipefail

SKILL="$(cd "$(dirname "$0")/.." && pwd)"
PORT=15200
[ "${1:-}" = "--port" ] && PORT="$2"
BASE="http://127.0.0.1:$PORT"
JAR="$SKILL/runs/cookies-$PORT.txt"
EMAIL="${VERIFY_EMAIL:-admin@raytha.local}"
PASSWORD="${VERIFY_PASSWORD:-Verify123\$}"

required="$(curl -sf "$BASE/raytha/api/auth/setup/status" | python3 -c 'import sys,json;print(json.load(sys.stdin)["required"])')"
body="$(python3 -c 'import json,sys; print(json.dumps(dict(zip(sys.argv[1::2], sys.argv[2::2]))))' \
  email "$EMAIL" password "$PASSWORD")"

if [ "$required" = True ]; then
  setup="$(python3 -c 'import json,sys; b=json.loads(sys.argv[1]); b.update(firstName="Verify",lastName="Admin",organizationName="Verify Org",smtpHost="localhost",smtpPort=1025,websiteUrl=sys.argv[2]); print(json.dumps(b))' "$body" "$BASE")"
  code="$(curl -s -o "$SKILL/runs/setup-$PORT.json" -w '%{http_code}' -c "$JAR" -X POST "$BASE/raytha/api/auth/setup" \
    -H 'Content-Type: application/json' -d "$setup")"
  echo "setup -> $code"
  [ "${code:0:1}" = 2 ] || { cat "$SKILL/runs/setup-$PORT.json" >&2; exit 1; }
else
  code="$(curl -s -o /dev/null -w '%{http_code}' -c "$JAR" -X POST "$BASE/raytha/api/auth/login" \
    -H 'Content-Type: application/json' -d "$body")"
  echo "login -> $code"
  [ "$code" = 200 ] || [ "$code" = 204 ] || { echo "sign-in failed; if this database was seeded by tools/seed.py, set VERIFY_EMAIL/VERIFY_PASSWORD" >&2; exit 1; }
fi

curl -sf -b "$JAR" "$BASE/raytha/api/auth/me" \
  | python3 -c 'import sys,json; m=json.load(sys.stdin); print("me", m.get("emailAddress") or m.get("email"), "isAdmin", m.get("isAdmin"), "permissions", len(m.get("permissions") or []))'
echo "jar $JAR"
