#!/usr/bin/env bash
# Local stand-in for .github/workflows/ci.yml: version, backend, admin.
# Skips the Docker/Trivy image job.
set -euo pipefail
cd "$(dirname "$0")/.."

status=0
fail() {
  echo "FAILED: $*" >&2
  status=1
}

if git rev-parse --verify origin/dev >/dev/null 2>&1 && git cat-file -e origin/dev:VERSION 2>/dev/null; then
  if ! git diff --quiet origin/dev...HEAD; then
    echo "==> version: committed VERSION matches this change"
    python3 tools/check-version.py --before origin/dev --after HEAD || fail "VERSION"
  fi
else
  echo "==> version: skipped (no VERSION on origin/dev yet)"
fi

echo "==> backend: restore"
dotnet tool restore --tool-manifest src/Raytha.Web/.config/dotnet-tools.json || true
dotnet restore Raytha.sln

echo "==> backend: build"
dotnet build Raytha.sln --no-restore --configuration Release || fail "dotnet build"

echo "==> backend: test"
dotnet test Raytha.sln --no-build --configuration Release || fail "dotnet test"

if [ -d src/admin ]; then
  echo "==> admin: install"
  (
    cd src/admin
    pnpm install --frozen-lockfile
    echo "==> admin: lint"
    pnpm lint || exit 1
    echo "==> admin: unit tests"
    pnpm test || exit 1
    echo "==> admin: typecheck and build"
    pnpm typecheck && pnpm build || exit 1
  ) || fail "admin"

  echo "==> admin: committed bundle is fresh"
  git diff --exit-code -- src/Raytha.Web/wwwroot/raytha || fail "admin bundle stale (rebuild and commit wwwroot/raytha)"
fi

echo "==> supply-chain: NuGet vulnerabilities"
python3 - <<'PY' || fail "NuGet vulnerabilities"
import json, subprocess, sys
raw = subprocess.check_output(
    ["dotnet", "list", "Raytha.sln", "package", "--vulnerable", "--include-transitive", "--format", "json"],
    text=True,
)
data = json.loads(raw)
found = []
for project in data.get("projects", []):
    for framework in project.get("frameworks", []):
        for top in framework.get("topLevelPackages", []) + framework.get("transitivePackages", []):
            vulns = top.get("vulnerabilities") or []
            if vulns:
                found.append(f"{top.get('id')} {top.get('resolvedVersion')}: {vulns}")
if found:
    print("\n".join(found), file=sys.stderr)
    sys.exit(1)
print("No vulnerable NuGet packages reported.")
PY

if [ "$status" -ne 0 ]; then
  echo "ci-local finished with failures." >&2
  exit "$status"
fi
echo "ci-local OK"
