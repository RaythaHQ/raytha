# Webhooks

An admin registers a URL to receive signed JSON events (content, users, admins,
and more), sees the signing secret once, and Raytha delivers matching events
with retries and records each delivery.

## Sub-features

- `webhooks-list` lists webhooks.
- `webhooks-create` creates one with name, URL, events, attempts, timeout; shows the secret once.
- `webhooks-edit` edits or deactivates (`Active`); delete is in the `Danger zone`.
- `webhooks-deliver` sends a signed POST with `X-Raytha-Event`, `X-Raytha-Delivery`,
  `X-Raytha-Timestamp`, `X-Raytha-Signature: sha256=…`.
- `webhooks-test` fires a `webhook.test` event on demand.
- `webhooks-deliveries` lists delivery attempts with status; redeliver one.

## How to get to it (user POV)

- Sidebar `Automation` → `Webhooks` → `/raytha/webhooks`.
- `/raytha/webhooks/new` (form: `Name`, `URL`, `Description`,
  `Subscribed events`, `Max attempts`, `Timeout (seconds)`, `Active`, button
  `Create`) → secret card with `Copy` and `Continue to webhook`.
- `/raytha/webhooks/<id>` to edit (`Save`).
- Headless: `/raytha/api/admin/webhooks`, `/events`, `/deliveries`,
  `/{id}/test`, `/deliveries/{id}/redeliver`.

## Driving it with browse.sh and curl

Preconditions:

- Baseline from `README.md`.
- A local receiver on a free loopback port that logs headers and body, e.g. a
  `python3 -m http.server`-style handler on `127.0.0.1:15299` that answers 200
  to POST. The webhook URL is the production boundary, so this is the one
  allowed stand-in.

- **Create.** In the UI: fill `Name`, `URL` = `http://127.0.0.1:15299/hook`,
  choose events, click `Create`; the `Webhook created` toast and the secret card
  appear. Or `POST /raytha/api/admin/webhooks` with
  `{"name":"probe","url":"http://127.0.0.1:15299/hook","subscribedEvents":["*"]}`
  → 201 `{ id, secret }`.
- **Fire.** `POST /raytha/api/admin/webhooks/<id>/test` (JSON `{}` body) → 200,
  or trigger a real event (create a user, publish an item).
- **Receive.** Within a few seconds the receiver gets a POST with event
  `webhook.test` and the four `X-Raytha-*` headers. Verify the signature with
  the secret before trusting it.
- **Record.** `GET /raytha/api/admin/webhooks/deliveries` lists the delivery
  with `status: "succeeded"` and the same payload.

## Gotchas

- The SPA has no test button and no delivery history page. `webhooks-test` and
  `webhooks-deliveries` are API-only; say so rather than claiming UI coverage.
- The secret is returned once, at creation (`CreatedWebhookDto`). Do not write
  it to evidence; `GET /webhooks/{id}` never returns it.
- `subscribedEvents` must be known names from `/webhooks/events` or `"*"`; an
  unknown one is a 400 with per-field errors.
- Deliveries are dispatched by a background worker, not inline. Poll the
  receiver or the deliveries list; do not assert immediately after the 200.
- Stop the receiver after the run so a later delivery does not hit a stranger
  on that port.
