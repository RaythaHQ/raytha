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
  `/raytha/api/auth` over the same cookie session as the rest of the host. The
  cutover is not finished: the Razor admin pages are still there and still win
  most `/raytha` routes, so the SPA is what you see in development, where Vite is
  proxied ahead of them.
- **.NET 10**, with `VERSION` at the repo root as the single source of the
  product version (currently `2.0.0`).
- New in the platform: outbound webhooks with HMAC-signed deliveries, feature
  flags, an email log, and `/healthz` plus `/healthz/ready`.

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

`VERSION` must describe the code in the same commit: a new EF migration means a
MINOR bump, anything else a PATCH. `tools/check-version.py` enforces it and
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
