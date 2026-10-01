#!/usr/bin/env python3
"""Require db/Postgres to hold the SQL the EF migrations generate (RFC-0012 §4).

Checks FreshCreateOnLatestVersion.sql and the upgrade script into the latest
release. Older upgrade scripts are frozen and predate generated scripts, so
they are not regenerated.
"""

from __future__ import annotations

import argparse
import difflib
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "src/Raytha.Web"
MIGRATIONS = ROOT / "src/Raytha.Infrastructure/Persistence/Migrations"
SQL_DIR = ROOT / "db/Postgres"
MIGRATION_FILE_RE = re.compile(r"^\d{14}_(v\d+_\d+_\d+)\.cs$")

# Values that change every time a script is generated, not when a migration does.
VOLATILE = [
    # EF stamps its own runtime version on each history row.
    (re.compile(r"^(VALUES \('\d{14}_v\d+_\d+_\d+', )'[^']+'(\);)$", re.M), r"\1'<ef-version>'\2"),
    # v1_4_0 seeds the default theme with Guid.NewGuid() and DateTime.UtcNow.
    (
        re.compile(
            r"^VALUES \('[0-9a-f-]{36}', 'Raytha default theme', 'raytha_default_theme', FALSE, "
            r"'Raytha default theme', TIMESTAMPTZ '[^']+'\);$",
            re.M,
        ),
        "VALUES ('<theme-id>', 'Raytha default theme', 'raytha_default_theme', FALSE, "
        "'Raytha default theme', TIMESTAMPTZ '<created>');",
    ),
]


def releases() -> list[str]:
    names = sorted(p.name for p in MIGRATIONS.iterdir() if MIGRATION_FILE_RE.match(p.name))
    return [MIGRATION_FILE_RE.match(name).group(1) for name in names]  # type: ignore[union-attr]


def normalize(text: str) -> str:
    text = text.lstrip("\ufeff").replace("\r\n", "\n").rstrip() + "\n"
    for pattern, replacement in VOLATILE:
        text = pattern.sub(replacement, text)
    return text


def generate(output: Path, ef_args: list[str], *migrations: str) -> str:
    subprocess.run(
        [
            "dotnet", "ef", "migrations", "script", *migrations,
            "--project", "../Raytha.Infrastructure",
            "--startup-project", ".",
            "--output", str(output),
            *ef_args,
        ],
        cwd=WEB,
        check=True,
        stdout=subprocess.DEVNULL,
    )
    return output.read_text(encoding="utf-8-sig")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--configuration", default="Debug")
    parser.add_argument("--no-build", action="store_true")
    args = parser.parse_args()

    ef_args = ["--configuration", args.configuration]
    if args.no_build:
        ef_args.append("--no-build")

    known = releases()
    if len(known) < 2:
        print("Need at least two migrations to name an upgrade script.", file=sys.stderr)
        return 1
    previous, latest = known[-2], known[-1]
    checks = [
        ("FreshCreateOnLatestVersion.sql", ()),
        (f"{previous}_to_{latest}.sql", (previous, latest)),
    ]

    failed = False
    with tempfile.TemporaryDirectory() as tmp:
        for index, (name, migrations) in enumerate(checks):
            committed_path = SQL_DIR / name
            if not committed_path.exists():
                print(f"MISSING db/Postgres/{name}", file=sys.stderr)
                failed = True
                continue
            # The first run builds when --no-build is not given; later runs reuse it.
            run_args = ef_args if index == 0 or args.no_build else [*ef_args, "--no-build"]
            expected = normalize(generate(Path(tmp) / name, run_args, *migrations))
            committed = normalize(committed_path.read_text(encoding="utf-8-sig"))
            if expected == committed:
                print(f"OK db/Postgres/{name}")
                continue
            failed = True
            print(f"STALE db/Postgres/{name}", file=sys.stderr)
            sys.stderr.writelines(
                difflib.unified_diff(
                    committed.splitlines(keepends=True),
                    expected.splitlines(keepends=True),
                    f"committed/{name}",
                    f"generated/{name}",
                )
            )

    if failed:
        print(
            "\nRegenerate from src/Raytha.Web:\n"
            "  dotnet ef migrations script --project ../Raytha.Infrastructure --startup-project . "
            "--output ../../db/Postgres/FreshCreateOnLatestVersion.sql\n"
            f"  dotnet ef migrations script {previous} {latest} --project ../Raytha.Infrastructure "
            f"--startup-project . --output ../../db/Postgres/{previous}_to_{latest}.sql",
            file=sys.stderr,
        )
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
