# REST API v1

A headless client reads and writes Raytha content with an API key: lists and
gets items, creates, edits, and deletes them, and batch-imports many items
through a background task.

## Sub-features

- `v1-key` mints an API key for an admin (shown once).
- `v1-auth` rejects requests without a valid `X-API-KEY`.
- `v1-content-read` lists and gets content items of a type.
- `v1-content-write` creates, edits, and deletes items.
- `v1-batch` batch-creates items and reports per-item results via a background task.
- `v1-other` content types, menus, site pages, themes, templates, media, users, functions.

## How to get to it (user POV)

- Admin SPA: `Admins` → an admin's editor → API keys card, at
  `/raytha/settings/admins/<id>`.
- `API reference` link at the bottom of the SPA sidebar (OpenAPI docs).
- Headless: `/raytha/api/v1/<Controller>/...`, e.g.
  `/raytha/api/v1/ContentItems/posts`.

## Driving it with browse.sh and curl

Preconditions:

- Baseline from `README.md`.

- **Mint a key.** `ME=$(curl -s -b runs/cookies-15200.txt http://127.0.0.1:15200/raytha/api/auth/me | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')`,
  then `curl -s -b runs/cookies-15200.txt -X POST -H 'Content-Type: application/json' -d '{}' http://127.0.0.1:15200/raytha/api/admin/admins/$ME/api-keys`
  answers 201 `{ "apiKey": "…" }`. Keep it in a shell variable, never in evidence.
- **No key.** `curl -s -o /dev/null -w '%{http_code} %{content_type}' http://127.0.0.1:15200/raytha/api/v1/ContentItems/posts`
  prints `401 application/problem+json`.
- **Read.** `curl -s -H "X-API-KEY: $KEY" http://127.0.0.1:15200/raytha/api/v1/ContentItems/posts`
  answers 200 `{ "result": { "items": [...] } }`.
- **Batch.** `POST /raytha/api/v1/ContentItems/<type>/batch` with
  `{"templateId": "...", "items": [{"content": {...}}]}` answers 202 with a task
  id in `result`; poll `GET /raytha/api/v1/BackgroundTasks/<id>` until status is
  `complete` or `error`, then read per-item outcomes from `statusInfo`.
- **Proof of write.** Read the created item back through v1 and through the
  admin API, and check its public route if published.

## Gotchas

- Controller segments are PascalCase (`ContentItems`, `BackgroundTasks`); the
  content type segment is the developer name (`posts`).
- v1 responses wrap payloads in `result`; the admin API does not.
- The key is a secret (RFC-0008 §7): do not write it to evidence, logs, or URLs.
- v1 policies are the `Api`-prefixed mirrors of admin permissions; a key
  inherits its admin's roles.
- Batch is asynchronous. A 202 proves nothing until the task completes.
