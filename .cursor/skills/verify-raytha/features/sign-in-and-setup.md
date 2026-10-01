# Admin sign-in and first-run setup

A fresh install asks for an organization and a first super admin; after that,
admins sign in with email and password (or another enabled scheme), recover a
forgotten password by email, and sign out.

## Sub-features

- `setup-status` reports whether first-run setup is still required.
- `setup-create` creates the organization and the first super admin and signs them in.
- `signin-password` signs in from `/raytha/login` and lands on the dashboard.
- `signin-schemes` lists enabled sign-in schemes.
- `forgot-password` emails a recovery link and completes a reset.
- `signout` ends the session.
- `session-me` returns the signed-in admin and their permissions.

## How to get to it (user POV)

- Visiting `/raytha` signed out → SPA route guard → sign-in.
- `/raytha/setup` on a database with no admin (SPA page).
- `/raytha/login` — the server-rendered Razor sign-in page (`Your email`,
  `Your password`, `Sign in`).
- `/raytha/login/forgot-password` from the sign-in page.
- `/raytha/logout`, or the user menu (`Verify Admin` at the top right) in the SPA.
- Headless: `/raytha/api/auth/{setup,setup/status,login,logout,me,schemes,forgot-password,magic-link}`.

## Driving it with browse.sh and curl

Preconditions:

- For setup: `scripts/host.sh --fresh`. For the rest: baseline from `README.md`.

- **Setup required.** `curl -s http://127.0.0.1:15200/raytha/api/auth/setup/status`
  returns `"required": true` on a fresh database.
- **Setup.** Run `scripts/session.sh`. It prints `setup -> 201` and
  `me admin@raytha.local isAdmin True permissions 8`. Status now returns
  `"required": false`; a second `POST /raytha/api/auth/setup` answers 409.
- **Browser sign-in.** Run `scripts/browse.sh --out evidence/<change> /raytha`.
  `browse.log` shows `signed in as admin@raytha.local, landed on /raytha` and a
  shot titled `Dashboard · Raytha Admin` with h1 `Good morning, Verify` (or
  afternoon/evening).
- **Schemes.** `curl -s http://127.0.0.1:15200/raytha/api/auth/schemes` lists
  `email_and_password` on a fresh database.
- **Forgot password.** `POST /raytha/api/auth/forgot-password` with
  `{"email":"admin@raytha.local"}` answers 204. MailHog then has a message with
  subject `[Verify Org] Password recovery`:
  `curl -s 'http://localhost:8025/api/v2/search?kind=to&query=admin@raytha.local&limit=1'`.
- **Unauthenticated API.** `curl -s -o /dev/null -w '%{http_code} %{content_type}' http://127.0.0.1:15200/raytha/api/admin/users`
  prints `401 application/problem+json` — never a redirect to HTML.

## Gotchas

- `/raytha/login` is the Razor page, not the SPA's `/login` route; on the
  committed bundle the literal Razor route wins. Its labels are `Your email` /
  `Your password`; `getByLabel(/email/i)` and `/password/i` work for both.
- Auth endpoints share a 30/min per-IP limiter. A loop of sign-ins or recovery
  requests hits 429; wait for `Retry-After`.
- MailHog is shared and keeps old messages for `admin@raytha.local`. Compare the
  `Created` timestamp to when you acted, or use a unique user.
- Setup needs an explicit `websiteUrl` outside Development; `session.sh` sends
  the host's base URL.
- `curl -b` reads the jar but does not update it; use `-c` too when a call
  rotates the cookie.
