# [Raytha](https://raytha.com) 2.0

[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](https://choosealicense.com/licenses/mit/)
[![CI](https://github.com/RaythaHQ/raytha/actions/workflows/ci.yml/badge.svg?branch=dev)](https://github.com/RaythaHQ/raytha/actions/workflows/ci.yml)

![Raytha Logo](https://user-images.githubusercontent.com/777005/210120197-61101dee-91c7-4628-8fb4-c0d701843704.png)

Raytha is a content management system built with .NET. You define content types
in the admin, edit Liquid templates in the browser, and get a public site plus a
REST API for whatever you modelled.

**[Website](https://raytha.com) · [Documentation](https://docs.raytha.com) · [User Guide](https://raytha.com/user-guide) · [YouTube](https://www.youtube.com/channel/UCuQtF2WwODs2DfZ4pV-2SfA)**

## What changed in 2.0

- **PostgreSQL only.** SQL Server support is gone. There is one provider, one set
  of migrations, and Postgres-specific features (`jsonb`, `ILIKE`) are used
  directly.
- **The admin is a React SPA.** It lives in `src/admin` (pnpm workspace, Vite,
  TanStack Router and Query) and is served at `/raytha` from a committed bundle in
  `src/Raytha.Web/wwwroot/raytha`. It talks to `/raytha/api/admin` and
  `/raytha/api/auth` over the same cookie session as the rest of the host. Only
  error pages, logout, the SAML/JWT sign-in handoffs, theme export, and the
  function test runner are still Razor pages.
- **.NET 10**, with `VERSION` at the repo root as the single source of the
  product version (currently `2.0.0`).
- New in the platform: outbound webhooks with HMAC-signed deliveries, an email
  log, and `/healthz` plus `/healthz/ready`.

The public site is unchanged in shape: controllers rendering Liquid templates
stored in the database.

## Features

- **Site pages and page builder** — landing pages from a drag-and-drop widget system
- **Custom content types** — content structures without code changes
- **Liquid templates** — edited in the platform, versioned with revision history
- **Role-based access control** — granular, per content type
- **Headless REST API** — auto-generated for your content, API-key authenticated
- **Audit logs** — every change, with the admin who made it
- **Single sign-on** — SAML and JWT for admins and users
- **Flexible storage** — local disk, Azure Blob, or anything S3-compatible
- **Command-line interface** — [`raytha`](https://github.com/RaythaHQ/raytha-cli) manages a whole site from a shell or an LLM agent

## Run it with Docker

```bash
git clone https://github.com/RaythaHQ/raytha.git
cd raytha
cp .env.example .env
docker compose --env-file .env up
```

Open [http://localhost:5001](http://localhost:5001) and complete the setup
wizard. Migrations are applied on first run.

One-click hosting is also available:
[![Deploy on Railway](https://railway.com/button.svg)](https://railway.com/deploy/raytha-cms?referralCode=RU52It&utm_medium=integration&utm_source=template&utm_campaign=generic)

HTTPS is enforced by default. `.env.example` ships with
`ASPNETCORE_ENVIRONMENT=Development` so you can use plain HTTP locally; set
`Production` once you have TLS in front of it. Every other setting — SMTP, cloud
storage, function limits, upload limits — is documented in `.env.example`.

Behind a proxy, `TRUSTED_PROXIES` decides who may set the client IP and scheme:

- Railway, Azure App Service, one nginx or Caddy: leave it unset (`all`).
- Cloudflare in front of Railway or App Service: `all` with `TRUSTED_PROXY_HOPS=2`.
- A sidecar, a tunnel, or a Docker network where Kestrel is also reachable directly: `private`.
- Known proxy addresses: a comma-separated list of IPs and CIDR ranges.
- Kestrel exposed straight to the internet: `none`.

> **Warning:** unset means any peer is trusted. If clients can reach Raytha
> without going through your proxy, set `none` or list your proxies, or they can
> forge their IP address and get around per-IP rate limits.

Emailed links and API media URLs are built from the Website URL in organization
settings, never from the request's host.

## Raytha CLI

[`raytha`](https://github.com/RaythaHQ/raytha-cli) is a command-line interface
for the 2.0 admin and REST API. It manages themes, templates, widgets, site
pages, content types, content, media, and menus with an administrator's API key,
so a site can be built from a script, a theme pulled and pushed as plain files,
or the whole thing driven by an LLM agent. Every command prints JSON with stable
exit codes, and the guides ship inside the binary.

```sh
# Linux and macOS
curl -fsSL https://github.com/RaythaHQ/raytha-cli/releases/latest/download/install.sh | sh
# Windows (PowerShell)
irm https://github.com/RaythaHQ/raytha-cli/releases/latest/download/install.ps1 | iex
```

```sh
export RAYTHA_URL=https://your-site.example.com
export RAYTHA_API_KEY=...        # Settings > Administrators > (admin) > API Keys
raytha doctor                    # connectivity, key, permissions
raytha guide build-a-site        # the agent playbook (markdown)
```

The CLI targets Raytha 2.0 and later. Command reference, output contract, and
the full agent walkthrough live in the
[raytha-cli README](https://github.com/RaythaHQ/raytha-cli#readme).

## Develop

Requirements: .NET 10 SDK, Node 24, pnpm 11.9, Docker.

```bash
./tools/dev.sh
```

That brings up the containers and runs `dotnet watch`, which also starts the
admin Vite dev server. Then:

- Public site: <http://localhost:5200>
- Admin: <http://localhost:5200/raytha>
- API reference (Scalar): <http://localhost:5200/raytha/api>
- Caught email (MailHog): <http://localhost:8025>

The dev server listens on every interface (`0.0.0.0:5200`). `./tools/dev.sh` prints the Tailscale URL when `tailscale` is on PATH, so another device on the tailnet can open `http://<tailscale-ip>:5200`.

`tools/compose.yaml` provides the local dependencies:

| Service | Port | Notes |
|---|---|---|
| `postgres` | 5433 | Postgres 17, database `raytha` |
| `mailhog` | 1025 / 8025 | SMTP sink, web UI on 8025 |
| `minio` | 9000 / 9001 | S3-compatible storage, console on 9001 |
| `azurite` | 10000 | Azure Blob emulator |

Ports already in use on the host are skipped rather than failing the whole
bring-up, so a shared Postgres is fine.

### Admin app

```bash
cd src/admin
pnpm install
pnpm dev          # Vite on 5203; dev.sh already does this for you
pnpm lint && pnpm test && pnpm typecheck
pnpm build        # writes src/Raytha.Web/wwwroot/raytha
```

The built bundle is committed. If you change anything under `src/admin`, run
`pnpm build` and include `src/Raytha.Web/wwwroot/raytha` in the same commit — CI
fails on a stale bundle.

### Checks and versioning

```bash
./tools/ci-local.sh     # what CI runs: version, backend, admin, NuGet audit
```

`VERSION` is the public release. It changes once, in the pull request into
`main`, for everything since the previous release: a new EF migration means a
MINOR bump, anything else a PATCH. Pull requests into `dev` do not bump it.
`tools/check-version.py` enforces the release pull request and
`tools/bump-version.py` does the arithmetic.

### Migrations

EF migrations live in `src/Raytha.Infrastructure/Persistence/Migrations` and are
named after the release that carries them.

```bash
dotnet ef migrations add v2_1_0 \
  --project src/Raytha.Infrastructure \
  --startup-project src/Raytha.Web
```

Keep `db/Postgres/FreshCreateOnLatestVersion.sql` and the upgrade script in the
same commit — operators who cannot migrate at startup apply those by hand.

## Upgrading from 1.5.0

Back up your database first. Then either:

- start Raytha with `APPLY_PENDING_MIGRATIONS=true` and let it migrate, or
- apply `db/Postgres/v1_5_0_to_v2_0_0.sql` yourself and start with
  `APPLY_PENDING_MIGRATIONS=false`.

Both land on the same schema. If you were running Raytha on SQL Server, 2.0 has
no upgrade path — migrate your data to Postgres on 1.5.0 first.

The upgrade rewrites some existing data, not just the schema:

- **Date fields become ISO dates.** 1.x stored them as `m/d/yyyy` or in the
  server's culture. Each date field's day/month order is inferred from its own
  values (a first number above 12 means day-first), and values are rewritten as
  `YYYY-MM-DD`, or `YYYY-MM-DDTHH:MM:SS` when they carried a time. This covers
  published content, drafts, revisions, and trash. In a field whose values
  disagree on the order (written under two server cultures), each value is read
  by its own numbers. A value that could be either order, or isn't a
  recognizable date, is left unchanged and reported as a `NOTICE`, which only
  `psql` shows. 2.0 reads a leftover value in the server's culture, as 1.x did.
  One it can't parse reads as an empty date, and the admin editor shows the
  stored text and asks for the date it means. To find what was left behind:

  ```sql
  SELECT t."DeveloperName" AS content_type, f."DeveloperName" AS field, ci."Id",
         ci."_PublishedContent" ->> f."DeveloperName" AS value
  FROM "ContentItems" ci
  JOIN "ContentTypes" t ON t."Id" = ci."ContentTypeId"
  JOIN "ContentTypeFields" f ON f."ContentTypeId" = ci."ContentTypeId" AND f."FieldType" = 'date'
  WHERE coalesce(ci."_PublishedContent" ->> f."DeveloperName", '') !~ '^([0-9]{4}-[0-9]{2}-[0-9]{2}.*)?$';
  ```

- **Media links become root-relative.** Rich text and page-builder widgets saved
  absolute URLs such as `http://localhost:5200/raytha/media-items/objectkey/…`,
  which broke when a site moved hosts. Links to media items in this database
  lose the scheme and host (any path base is kept) in content, drafts,
  revisions, trash, and site pages. Links to other sites and all templates are
  left alone.
- **Manage Media is its own permission.** Roles that could reach media before —
  Manage Content Types, Manage System Settings, or Edit on any content type —
  are granted it, so no one loses access.
- **Magic-link sign-in uses a one-time code.** The magic-link email template
  and the "magic link sent" page are replaced if they don't already use the code,
  because the 1.x versions render a link that no longer works. Your previous
  content is kept as a revision you can restore from or merge by hand. Links
  emailed before the upgrade stop working.
- **Built-in widget templates gain field definitions** for the 2.0 page builder.
  Templates that already have fields are not touched.

These rewrites are not undone by rolling back the migration, and 1.5 cannot read
the converted dates. To go back to 1.5, restore the backup. Check the Website URL
in organization settings before upgrading too: 2.0 builds media and email links
from it, so a stale value breaks images.

## Architecture

Clean Architecture, dependencies pointing inward:

| Project | Holds |
|---|---|
| `src/Raytha.Domain` | Entities, value objects, domain events |
| `src/Raytha.Application` | Commands, queries, handlers, validators (Mediator) |
| `src/Raytha.Infrastructure` | EF Core, migrations, storage, background tasks |
| `src/Raytha.Web` | Public site, admin API, SPA hosting, REST API v1 |
| `src/admin` | The React admin workspace |

`tests/Raytha.Architecture.Tests` enforces the boundaries, so a misplaced
reference fails the test run rather than review. The conventions are written up
as RFCs in `.cursor/rules/`, starting with `rfc-0000-index.mdc`.

## Community

- [Documentation](https://docs.raytha.com)
- [GitHub Discussions](https://github.com/RaythaHQ/raytha/discussions)
- [YouTube](https://www.youtube.com/channel/UCuQtF2WwODs2DfZ4pV-2SfA)
- [Twitter](https://twitter.com/raythahq)

## Contributing

MIT licensed. Issues, discussions, and pull requests are welcome — see
[CONTRIBUTING.md](CONTRIBUTING.md), and run `./tools/ci-local.sh` before you
push.

---

Created by [Zack Schwartz](https://twitter.com/apexdodge)
