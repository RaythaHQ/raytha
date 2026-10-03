---
name: pre-push-ci
description: >-
  Run the same GitHub Actions CI checks locally and fix failures before
  pushing Raytha. Use when the user asks to commit and push, push, or create
  a pull request. Do not use for commit-only requests.
---

# Pre-push CI

Mirror `.github/workflows/ci.yml` locally, fix anything that would fail on
GitHub, then commit (if asked) and push. Do not push while a check is red.

## When this applies

- "commit and push", "push", or opening a PR (that workflow pushes)
- Not "commit" alone

## Run

From the repo root, after the tree you will push is ready (fixes included):

```bash
./tools/ci-local.sh
```

That script covers the `version`, `backend`, and `admin` jobs plus a NuGet
vulnerability scan. It skips the `image` job (Docker build + Trivy), which only
runs on push to `main`/`dev`. If `ci.yml` changes, update the script to match.

What it actually does, in order:

1. `python3 tools/check-version.py --self-test`. The comparison against
   `origin/main` runs only when `VERSION_BASE=origin/main` or the open pull
   request targets `main`. A pull request into `dev` does not bump `VERSION`.
2. `dotnet restore` / `dotnet build --configuration Release` / `dotnet test` on
   `Raytha.sln`.
3. `src/admin`: `pnpm install --frozen-lockfile`, `pnpm lint`, `pnpm test`,
   `pnpm typecheck && pnpm build`.
4. `git diff --exit-code -- src/Raytha.Web/wwwroot/raytha` — the committed admin
   bundle must match a fresh build.
5. `dotnet list package --vulnerable --include-transitive` must report nothing.

Requirements: .NET 10 SDK, Node 24, pnpm 11.9, Docker only for the image job.
`origin/main` needs to be fetched when step 1 compares against it.

## Fix loop

If a check fails, fix it and re-run. Typical remediations:

| Failure | Fix |
|---|---|
| `VERSION` | Only on a pull request into `main`. Set `VERSION` to what `check-version.py --before origin/main` says: MINOR + reset PATCH if the release adds an EF migration, otherwise PATCH (RFC-0012). Do not edit the script, and do not bump on a pull request into `dev`. |
| `dotnet build` | Fix the error. Do not silence it with a pragma. |
| `dotnet test` | Fix the code. Architecture-test failures mean a rule in `.cursor/rules/` was broken — fix the code, not the test (RFC-0007). |
| admin `pnpm lint` | Fix the reported rule. Do not disable it unless there is existing precedent in the file. |
| `pnpm test` | Vitest in `@raytha/ui`. Fix the component or the expectation. |
| `pnpm typecheck` | Fix the types. `any` and `@ts-expect-error` need a stated reason. |
| admin bundle stale | Run `pnpm build` in `src/admin`, then stage `src/Raytha.Web/wwwroot/raytha`. |
| NuGet vulnerabilities | Upgrade or replace the package in `Directory.Packages.props`. Do not ignore it. |

Notes:

- A migration added under `src/Raytha.Infrastructure/Persistence/Migrations`
  also needs the `db/Postgres/` SQL scripts refreshed (RFC-0012 §4).
  `tools/check-sql-scripts.py` fails CI when they drift and prints the
  regenerate commands.
- Include the rebuilt bundle in the same commit you are about to push. Include
  the `VERSION` change only when the pull request targets `main`. If the user
  already committed, make a **new** commit (amend only when the user's git rules
  allow it).

Do not push, and do not skip hooks, until `./tools/ci-local.sh` exits 0. Then
follow the user's existing commit/push/PR rules.
