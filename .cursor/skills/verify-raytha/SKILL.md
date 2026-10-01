---
name: verify-raytha
description: >-
  Prove a Raytha change against a running instance on an isolated port
  (127.0.0.1:15200) and an isolated Postgres database (raytha_verify). Drives
  the React admin at /raytha in headless Chromium, the admin API under
  /raytha/api, the REST API v1, webhooks, email via MailHog, and the public
  Liquid site. Use when verifying any user-visible Raytha behavior without
  touching the developer's own dev instance on :5200 or its database.
---

# Verify Raytha

Raytha is one ASP.NET Core host with four surfaces an agent can drive:

| Surface | Where | Driven with |
|---|---|---|
| Admin SPA (React) | `/raytha/...` | `scripts/browse.sh` (Playwright, headless Chromium) |
| Admin API (cookie session) | `/raytha/api/auth`, `/raytha/api/admin` | `curl` with the jar from `scripts/session.sh` |
| REST API v1 (`X-API-KEY`) | `/raytha/api/v1/...` | `curl` with a key minted through the admin API |
| Public site (Razor + Liquid) | `/`, content routes like `/2026/10/my-post` | `curl` or `browse.sh` |

The developer's loop is `./tools/dev.sh`: host on **:5200**, Vite on **:5203**,
database **raytha**. **Never drive that instance or that database.** This skill
runs its own host on **http://127.0.0.1:15200** against **raytha_verify** in the
same compose Postgres (port 5433); the database name is the isolation. MailHog
(1025/8025) is shared.

Every script lives in `scripts/` and is run from the skill directory or by path
from the repo root. All of them refuse port 5200 and database `raytha`.

## Launch

Prereqs: .NET 10 SDK, Docker, Node 24, the Playwright CLI on `PATH` with
Chromium installed (`playwright install chromium`). If you changed anything
under `src/admin`, run `pnpm build` in `src/admin` first; this host serves the
**committed bundle** in `src/Raytha.Web/wwwroot/raytha`, never Vite.

```bash
S=.cursor/skills/verify-raytha/scripts
$S/host.sh            # build, snapshot, start, wait for /healthz/ready
$S/host.sh --fresh    # same, after dropping raytha_verify and uploads (true first run)
$S/host.sh --no-build # restart the last snapshot without rebuilding
$S/host.sh --db raytha_verify_seed --port 15201   # a second, independent instance
```

Ready looks like:
`ready base=http://127.0.0.1:15200 db=raytha_verify pid=… version=2.0.0 log=…/runs/host-15200.log`

What `host.sh` does, and why each piece is there:

- Builds `src/Raytha.Web`, then copies `bin/Debug/net10.0` to `runs/bin` and runs
  that copy. The developer's `dotnet watch` rebuilds the same output directory;
  the snapshot keeps it from swapping binaries under a running verification.
- Starts the binary (not `dotnet run`, which forks a child that owns the port)
  from `src/Raytha.Web`, because first-run setup reads the default theme from
  `wwwroot` relative to the working directory.
- Passes every setting as a command-line argument with colons. `Program` calls
  `Env.TraversePath().Load()`, so a `.env` in this or any parent directory would
  override environment variables; command-line configuration outranks it.
  `--ConnectionStrings__DefaultConnection` (double underscore) is ignored on the
  command line and would silently attach to `raytha`.
- Pins `AdminSpa:DevServerUrl` to a dead port. With `AutoStart=false` the host
  still proxies `/raytha` to any Vite it finds on 5203 — the developer's live
  source, not the bundle under test.
- Points `FILE_STORAGE_LOCAL_DIRECTORY` at `runs/uploads`, an absolute,
  all-lowercase path: local storage lowercases the path, and a relative one
  lands inside `src/Raytha.Web`.
- Refuses to start if the port is held by a process it did not record.

The first launch on a fresh database logs `3D000: database "raytha_verify" does
not exist` (Data Protection loading keys) before migrations create it. If
`/healthz/ready` is Healthy afterwards, that noise is expected.

## Doctor

Run before drawing any conclusion, and again whenever something looks off. It
is read-only and stops at the first failure.

```bash
$S/doctor.sh            # or --port 15201
```

It checks: the recorded pid owns the port; the process is attached to a
database other than `raytha`; `/healthz/ready` is Healthy; the reported version
equals `VERSION` (a mismatch is a stale build: rerun `host.sh`); the host is not
proxying to Vite; `/raytha/users` serves the committed SPA bundle
(`<title>Raytha Admin</title>`); setup status answers.

Never fall back to :5200 because the verify host is unhealthy. Read
`runs/host-15200.log` and fix the launch.

## Session

```bash
$S/session.sh           # setup on a fresh DB, otherwise sign in
```

On a fresh database it calls `POST /raytha/api/auth/setup` (the endpoint the
SPA's `/raytha/setup` page uses) and creates super admin
`admin@raytha.local` / `Verify123$`, org `Verify Org`, SMTP to MailHog. Later
runs sign in with `POST /raytha/api/auth/login`. Either way it writes
`runs/cookies-15200.txt` and prints `me`. For a database seeded by
`tools/seed.py`, set `VERIFY_EMAIL` and `VERIFY_PASSWORD`; `browse.sh` reads the
same variables.

## Drive

### Admin SPA in a browser

On the committed bundle the SPA owns every admin page. Measured on this host:
`/raytha`, `/raytha/users`, `/raytha/webhooks`, `/raytha/settings/authentication`,
`/raytha/content/posts`, and arbitrary deep paths all return the React shell.
Only `/raytha/login*`, `/raytha/logout`, `/raytha/login-redirect`,
`/raytha/error/*`, `/raytha/themes/export/*`, and `/raytha/functions/execute/*`
are still server-rendered — so sign-in goes through the Razor login page, then
lands in the SPA.

```bash
# Screenshot a list of pages, signed in
$S/browse.sh --out evidence/<change> /raytha /raytha/users /raytha/webhooks

# Run a scripted flow
$S/browse.sh --out evidence/<change> --steps evidence/<change>/steps.cjs
```

A steps module gets a signed-in Playwright `page`:

```js
module.exports = async ({ page, base, shot, log, settle }) => {
  await page.goto(`${base}/raytha/users/new`, { waitUntil: "networkidle" });
  await page.getByLabel(/^first name/i).fill("Ada");
  await page.getByRole("button", { name: "Create" }).click();
  await page.getByText("User created").waitFor();
  await shot("user-created");   // NN-user-created.png + .aria.txt, after skeletons clear
};
```

Handles that hold across the SPA:

- Form fields: `getByLabel(/^<label>/i)`. Required fields render `Label *`, so
  anchor the start and do not match the asterisk.
- Buttons and links by accessible name: `New user`, `Create`, `Save`,
  `Save changes`, `Publish`, `Save draft`, `Sign in`.
- Success toasts are text: `User created`, `Webhook created`, `Saved`.
- Page titles are `<Page> · Raytha Admin`; each page has one `h1`.
- Loading state is `@raytha/ui` `Skeleton` (`.animate-pulse`). `shot()` waits for
  it to clear; when asserting yourself, wait for a heading or cell, not a sleep.

`browse.sh` writes `browse.log` (every shot with URL, title, h1) and
`console.log` (browser console errors, page errors, and every `/raytha/api`
response ≥ 400). An empty `console.log` is part of the proof.

### Admin API

```bash
BASE=http://127.0.0.1:15200
JAR=.cursor/skills/verify-raytha/runs/cookies-15200.txt
curl -s -b $JAR "$BASE/raytha/api/admin/users?pageSize=50&search=ada" | python3 -m json.tool
curl -s -b $JAR -X POST $BASE/raytha/api/admin/users \
  -H 'Content-Type: application/json' \
  -d '{"firstName":"Ada","lastName":"Lovelace","emailAddress":"ada@raytha.local","sendEmail":false}'
```

- Failures are `application/problem+json`, never an HTML redirect. 401 = empty
  jar; 415 = missing `Content-Type: application/json` (required on every
  POST/PUT, even an empty `{}` body); 429 = auth rate limit (30/min/IP) — wait
  for `Retry-After`.
- Ids are `ShortGuid` strings. Lists take `pageNumber`, `pageSize`, `orderBy`,
  `search` and answer `{ items, totalCount, … }`.
- `POST /raytha/api/auth/login` is the production sign-in endpoint, not a back
  door.

### Database

Read-only checks against the isolated database, for side effects the UI does
not show:

```bash
docker compose -f tools/compose.yaml exec -T postgres \
  psql -U postgres -d raytha_verify -Atc 'select "EmailAddress","IsAdmin" from "Users"'
```

Never write to the database to set up a scenario; go through the API.

### Email

Mail goes to the shared MailHog. Read it through its API and filter, because
other runs' messages are there too:

```bash
curl -s 'http://localhost:8025/api/v2/search?kind=to&query=ada@raytha.local&limit=5'
```

Use a unique recipient per run (`ada.$(date +%s)@raytha.local`) or compare the
`Created` timestamp to when you acted.

### Seeded data

For a realistic dataset (every field type, hundreds of items, users, themes,
site pages, webhooks), use a separate database so `raytha_verify` stays small:

```bash
$S/host.sh --fresh --db raytha_verify_seed
python3 tools/seed.py --base-url http://127.0.0.1:15200 \
  --email seed-owner@example.com --password 'Seed-Pass-2026!' --setup
VERIFY_EMAIL=seed-owner@example.com VERIFY_PASSWORD='Seed-Pass-2026!' $S/session.sh
```

## Feature map

`features/README.md` indexes one recipe per user-facing feature: entry points,
exact handles, and the end state that proves it. Read the matching file before
driving a feature. A proof that exercises one entry point is incomplete when the
map lists others; say which ones you skipped.

## Evidence

Put artifacts in `evidence/<change>/`, named for what they show. Steps modules
live there too, so the proof can be rerun. `evidence/` and `runs/` are
gitignored: evidence backs your report, it does not ship.

Proof bar:

- Walk the real path: the page a user clicks, or the HTTP endpoint the SPA or a
  headless client calls. A handler unit test or a direct database write is not
  verification.
- Capture the action and the resulting state: a before shot or read, the
  action, and an after shot or read.
- A mutation needs a second, independent read of the new state — re-`GET` it,
  reload the list, or query `raytha_verify`. A 200 or a toast alone is not proof.
- Check side effects alongside the screen: the row in Postgres, the mail in
  MailHog, the request at the webhook receiver, the rendered public page.
- No mocks and no in-memory database. The only stand-in allowed is a local HTTP
  receiver for outbound webhooks, because the webhook URL is already the
  production boundary.
- If something could not be verified, say which part and why rather than
  implying coverage.

## Cleanup

```bash
$S/stop.sh              # or --port 15201
```

It kills only the pid it recorded and confirms the port is free. If another
process still holds the port, it says so and leaves it alone; find it with
`ss -ltnp | grep 15200` and decide deliberately. Never `pkill dotnet` or
`pkill node` — that kills the developer's instance, the one thing this skill
exists to protect.

What stays:

- `evidence/` — always. Cleanup never touches it.
- `raytha_verify` — reused next launch. Drop it with `host.sh --fresh`, or
  `docker compose -f tools/compose.yaml exec -T postgres dropdb -U postgres --force raytha_verify`.
- Compose services — shared, leave them running.
- `runs/` — logs, jars, uploads, the binary snapshot. Scratch; safe to delete
  when no host is running.

## Helpers

| Script | Does |
|---|---|
| `scripts/host.sh [--db N] [--port N] [--no-build] [--fresh]` | Build, snapshot, launch, wait for ready |
| `scripts/doctor.sh [--port N]` | Read-only health, isolation, version, and bundle checks |
| `scripts/session.sh [--port N]` | First-run setup or sign-in; writes `runs/cookies-PORT.txt` |
| `scripts/browse.sh --out DIR [--port N] (--steps F.cjs \| PATH...)` | Headless Chromium, signed in; screenshots + ARIA snapshots + console log |
| `scripts/stop.sh [--port N]` | Stop the host this skill started |
