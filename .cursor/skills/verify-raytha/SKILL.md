---
name: verify-raytha
description: >-
  Prove a Raytha change against a running instance on isolated ports and an
  isolated Postgres database. Use when verifying the public site, the admin
  API under /raytha/api, the admin shell at /raytha, sign-in, or first-run
  setup, without touching the developer's own dev instance or database.
---

# Verify Raytha

Raytha is one ASP.NET Core host with three surfaces:

- **Public site** — server-rendered controllers plus Liquid templates at `/`
- **Admin** — React SPA bundle at `/raytha`, with Razor admin pages still
  claiming many `/raytha` routes (RFC-0006 §6)
- **APIs** — `/raytha/api/auth` and `/raytha/api/admin` (cookie session, what the
  SPA calls) and `/raytha/api/v1` (headless, `X-API-KEY`)

`./tools/dev.sh` is the human loop: host on **http://localhost:5200**, Vite on
**5203**, Postgres database **raytha**. **Never drive that instance.** This skill
runs its own host on **http://127.0.0.1:15200** against Postgres database
**raytha_verify**. Compose services (Postgres on 5433, MailHog, MinIO, Azurite)
are shared; the database name is the isolation.

There is no helper CLI in this repo — the commands below are the skill. Run them
from the repo root.

## Why not Vite

Do not start Vite for verification. `src/admin/apps/shell/vite.config.ts`
hardcodes its API proxy to `http://localhost:5200`, so a Vite-served SPA would
send every mutation to the developer's own instance and database. Verification
runs against the committed bundle with `AdminSpa__AutoStart=false`.

The tradeoff is real and you must account for it. Measured on this instance with
the committed bundle and a signed-in super admin:

| Request | Answer |
|---|---|
| `GET /raytha` | Razor, `<title>Dashboard - Raytha</title>` |
| `GET /raytha/users/groups` | Razor, `<title>User groups - Raytha</title>` |
| `GET /raytha/webhooks` | 302 to `/raytha/error/404` (Razor's `{contentTypeDeveloperName}/{viewId?}` route claims it) |
| `GET /raytha/settings/authentication` | 302 to `/raytha/error/404`, same reason |
| `GET /raytha/spa/shell/probe` | SPA shell, `<title>Raytha Admin</title>` |

Every real SPA route is shadowed by a Razor page or by that content-items
parameter route, which outranks the SPA's `{**path}` catch-all. Only a deep path
nothing else claims reaches the shell. So headless proof of admin behavior comes
from `/raytha/api/*`, and the shell probe proves only that the bundle is being
served.

## Launch

Prereqs: .NET 10 SDK, Docker, and the committed bundle in
`src/Raytha.Web/wwwroot/raytha` (run `pnpm build` in `src/admin` if you changed
admin code).

```bash
RUNS="$PWD/.cursor/skills/verify-raytha/runs"
mkdir -p "$RUNS"
./tools/compose-up.sh     # skips any service whose port is already taken
dotnet build src/Raytha.Web/Raytha.Web.csproj

ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://127.0.0.1:15200 \
nohup src/Raytha.Web/bin/Debug/net10.0/Raytha.Web \
  --ConnectionStrings:DefaultConnection="Host=localhost;Port=5433;Username=postgres;Password=changeme;Database=raytha_verify" \
  --APPLY_PENDING_MIGRATIONS=true \
  --AdminSpa:AutoStart=false \
  --SMTP_HOST=localhost \
  --SMTP_PORT=1025 \
  --FILE_STORAGE_LOCAL_DIRECTORY="$RUNS/uploads" \
  > "$RUNS/host.log" 2>&1 &
echo $! > "$RUNS/host.pid"
```

Why it is shaped this way:

- Run the **built binary**, not `dotnet run`. `dotnet run` forks a child that owns
  the port, so killing the pid you recorded leaves a host running on 15200 and the
  next launch fails in a confusing way.
- `FILE_STORAGE_LOCAL_DIRECTORY` must be **absolute**: a relative path resolves
  against the content root (`src/Raytha.Web/`), which scatters upload
  directories into the source tree.

- Settings go in as **command-line arguments with colons**, because `Program`
  calls `DotNetEnv`, so a repo-root `.env` can overwrite environment variables —
  command-line configuration outranks both. `--ConnectionStrings__DefaultConnection`
  (double underscore) is ignored on the command line and would silently attach you
  to the developer's `raytha` database.
- The bind address is the one exception: `Program` calls
  `UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? …)`, so the
  port comes from that **environment variable** and `--urls` does nothing. If a
  repo-root `.env` sets `ASPNETCORE_URLS`, the host will land somewhere else —
  check that before launching, and always confirm the port afterwards.
- `Development` keeps HTTPS redirection and HSTS off. `AdminSpa:AutoStart=false`
  keeps Vite out of it.
- The first launch creates `raytha_verify` and applies every migration. Startup
  logs a Data Protection failure against the not-yet-existing database on the way
  there; if `/healthz/ready` comes back Healthy afterwards, that noise is
  expected and not the bug you are chasing.

## Doctor

Run these before drawing any conclusion. All must pass.

```bash
BASE=http://127.0.0.1:15200

# 1. It is our process on the port, and it is not :5200
ss -ltnp | grep ':15200'
cat .cursor/skills/verify-raytha/runs/host.pid

# 2. Liveness, readiness, and the version the binary reports
curl -s $BASE/healthz | python3 -m json.tool
curl -s $BASE/healthz/ready | python3 -m json.tool   # "status": "Healthy"
cat VERSION                                          # must match healthz "version"

# 3. The isolated database exists and the shared one was not touched
docker compose -f tools/compose.yaml exec -T postgres \
  psql -U postgres -lqt | cut -d'|' -f1 | grep -w raytha_verify

# 4. Public site answers
curl -s -o /dev/null -w '%{http_code}\n' $BASE/

# 5. The admin bundle is served (deep unclaimed path, see the table above)
curl -s $BASE/raytha/spa/shell/probe | grep '<title>Raytha Admin</title>'
```

On step 5: the path is deliberately not a real SPA route, because every real one
is shadowed. `GET /raytha` returning the Razor dashboard is expected here —
finding `Raytha Admin` at `/raytha` instead would mean Vite is proxying and you
are not testing the bundle.

Readiness covers Postgres and the file storage provider. If `/healthz/ready` is
degraded, read `runs/host.log`; do not drive a half-up host, and never fall back
to :5200.

## First-run setup

A fresh `raytha_verify` has no organization and no admin. Create one through the
same endpoint the SPA uses:

```bash
BASE=http://127.0.0.1:15200
JAR=.cursor/skills/verify-raytha/runs/cookies.txt

curl -s $BASE/raytha/api/auth/setup/status | python3 -m json.tool   # "required": true

curl -s -c $JAR -X POST $BASE/raytha/api/auth/setup \
  -H 'Content-Type: application/json' \
  -d '{"firstName":"Verify","lastName":"Admin","email":"admin@raytha.local",
       "password":"Verify123$","organizationName":"Verify Org",
       "smtpHost":"localhost","smtpPort":1025}'
```

Setup signs the new super admin in, so `$JAR` is authenticated from here. It is
idempotent only in the sense that a second call returns 409 — that is the
expected answer on an already-initialized database, not a failure.

## Drive

```bash
BASE=http://127.0.0.1:15200
JAR=.cursor/skills/verify-raytha/runs/cookies.txt

# Session
curl -s -c $JAR -X POST $BASE/raytha/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@raytha.local","password":"Verify123$","rememberMe":true}'
curl -s -b $JAR $BASE/raytha/api/auth/me | python3 -m json.tool

# Admin API reads (RFC-0010 list contract)
curl -s -b $JAR "$BASE/raytha/api/admin/users?pageNumber=1&pageSize=10&search=" | python3 -m json.tool
curl -s -b $JAR "$BASE/raytha/api/admin/content-types" | python3 -m json.tool

# Admin API mutation — JSON content type is mandatory (RFC-0008 §5)
curl -s -b $JAR -X POST $BASE/raytha/api/admin/users \
  -H 'Content-Type: application/json' \
  -d '{"firstName":"Ada","lastName":"Lovelace","emailAddress":"ada@raytha.local","sendEmail":false}'

# Public site HTML
curl -s $BASE/ > .cursor/skills/verify-raytha/runs/home.html
```

Notes that will save you a wrong conclusion:

- `/raytha/api/*` answers with status codes and
  `application/problem+json`, never an HTML login redirect. A 401 means the
  cookie jar is empty; a 415 means you forgot `Content-Type: application/json`;
  a 429 means you hit the auth rate limiter (30/min per IP by default) and should
  wait, not retry harder.
- Ids in requests and responses are `ShortGuid` strings.
- `POST /raytha/api/auth/login` is the production endpoint the SPA uses, not a
  test back door. It is a legitimate way to establish a session.
- Mail goes to MailHog; read it at http://localhost:8025 when a flow sends email.
- Browser automation is only meaningful for the React app, which needs Vite,
  which this skill does not run. If a change genuinely requires clicking through
  the SPA, say so and let the developer drive their own instance.

## Evidence

Keep artifacts in `.cursor/skills/verify-raytha/evidence/<change>/`, named after
what they show (`home.html`, `users-list.json`, `me.json`).

Proof bar:

- Walk the real path: the HTTP endpoint the SPA or a browser would call. A
  handler unit test is not verification.
- A mutation needs a second read that shows the new state — re-`GET` the list or
  the item. A 200 on the write alone is not proof.
- Capture before and after, not just the final state.
- No mocks and no in-memory database. This instance talks to real Postgres.
- If you could not verify something (anything needing the React UI), say which
  part is unverified rather than implying coverage.

## Cleanup

```bash
kill "$(cat .cursor/skills/verify-raytha/runs/host.pid)"
sleep 2
ss -ltn | grep ':15200' || echo "port 15200 free"
rm -f .cursor/skills/verify-raytha/runs/host.pid
```

Confirm the port is free. If something is still listening, find it with
`ss -ltnp | grep 15200` and kill that pid specifically — never by process name.

- Leave compose running; Postgres, MailHog, MinIO, and Azurite are shared.
- Leave `raytha_verify` in place; the next launch reuses it and setup is already
  done. Drop it only when you need a true first-run test:
  `docker compose -f tools/compose.yaml exec -T postgres dropdb -U postgres raytha_verify`.
- Leave `evidence/`. `runs/` holds logs, cookies, and uploads — it is scratch,
  and nothing in it should be committed.
- Never `pkill dotnet` or `pkill node`. That kills the developer's dev instance,
  which is the one thing this skill exists to protect.
