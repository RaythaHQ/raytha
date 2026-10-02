# Raytha verification map

The maintained source for verifying Raytha's user-facing behavior. Read this
index, then use the matching feature file as the recipe. Paths below are
relative to `.cursor/skills/verify-raytha/`.

## Baseline preconditions

- A host started by `scripts/host.sh` on `http://127.0.0.1:15200` against
  `raytha_verify` (or another non-`raytha` database you named).
- `scripts/doctor.sh` passes: pid owns the port, Healthy, `VERSION` matches,
  committed bundle served.
- `scripts/session.sh` has run, so super admin `admin@raytha.local` /
  `Verify123$` exists and `runs/cookies-15200.txt` is signed in.
- A fresh database has one content type, `posts` (fields `title`, `content`,
  `featured_image`), templates `raytha_html_content_item_detail` and
  `raytha_html_content_item_list`, and the default theme.
- Never drive :5200 or the `raytha` database.

## Driving conventions

- Browser recipes run through `scripts/browse.sh --steps`. Selectors are
  accessible names and labels, never CSS position.
- API recipes use `curl` with the cookie jar (admin API) or `X-API-KEY` (v1).
  Every POST/PUT sends `Content-Type: application/json`.
- Make created names unique per run (`Probe post $(date +%s)`,
  `ada.$(date +%s)@raytha.local`), so reruns on a reused database and shared
  MailHog stay unambiguous.
- Wait on an element or a response, not a fixed sleep.

## Proof and skip reporting

- Capture the action and the resulting state: before, action, after.
- UI proof: the `browse.sh` screenshot and `.aria.txt` pair, plus an empty
  `console.log`.
- Mutation proof: an independent second read — admin API `GET`, a reload, or a
  read-only `psql` query against the isolated database.
- Side effects are proven where they land: MailHog for email, a local receiver
  for webhooks, the public URL for published content.
- Record the feature file and entry point with each artifact folder.
- Report an unreachable entry point with the command you tried and the unmet
  precondition. Do not report it as verified through another path.

## Feature entry contract

Each feature file has an H1, one paragraph describing the user-visible behavior,
then exactly these H2s in order: `Sub-features`, `How to get to it (user POV)`,
`Driving it with browse.sh and curl`, `Gotchas`.

## Features

- [Admin sign-in and first-run setup](./sign-in-and-setup.md): setup, password
  sign-in, sign-out, forgot password via MailHog, session API.
- [Users](./users.md): list, search, create, edit, suspend, reset password,
  delete.
- [Content items and the public site](./content-and-public-site.md): create,
  publish, draft, and render a content item at its public route.
- [REST API v1](./rest-api-v1.md): mint an API key, read and write content
  headlessly, batch create.
- [Webhooks](./webhooks.md): create, receive a signed delivery, inspect the
  delivery log.

## Last driven

2026-10-01, version 2.0.0, while generating this skill:

- Users: UI create, search, suspend; API and Postgres read-back.
- Content: UI publish and API create; anonymous public render of both.
- Sign-in: setup, 409 on repeat, browser sign-in, forgot-password mail in MailHog, 401 problem details.
- REST API v1: key mint, 401 without key, list with key. Batch not run.
- Webhooks: API create, test, signed receipt, delivery log. UI create form not run.

Update this list when you drive a recipe, so the next agent knows what has been
exercised recently and what is only documented.
