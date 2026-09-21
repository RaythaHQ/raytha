#!/usr/bin/env python3
"""Require VERSION to describe the code in the same Git revision."""

from __future__ import annotations

import argparse
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION_FILE = "VERSION"
MIGRATION_RE = re.compile(r"(^|/)Persistence/Migrations/[^/]+\.cs$")
EXCLUDE_RE = re.compile(r"(Designer|ModelSnapshot)\.cs$")
NON_RELEASE_FILES = {VERSION_FILE, "docs/supply-chain/sbom.cdx.json"}


def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def parse_version(text: str) -> tuple[int, int, int]:
    parts = text.strip().split(".")
    if len(parts) != 3 or not all(part.isdigit() for part in parts):
        raise ValueError(f"VERSION must be MAJOR.MINOR.PATCH, got {text!r}")
    return tuple(map(int, parts))  # type: ignore[return-value]


def version_at(revision: str) -> tuple[int, int, int]:
    return parse_version(git("show", f"{revision}:{VERSION_FILE}"))


def changed_files(before: str, after: str) -> list[str]:
    return git("diff", "--name-only", before, after).splitlines()


def expected_version(
    current: tuple[int, int, int], migration_added: bool
) -> tuple[int, int, int]:
    major, minor, patch = current
    return (major, minor + 1, 0) if migration_added else (major, minor, patch + 1)


def format_version(version: tuple[int, int, int]) -> str:
    return ".".join(map(str, version))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--before", required=True, help="base Git revision")
    parser.add_argument("--after", required=True, help="code Git revision")
    args = parser.parse_args()

    files = changed_files(args.before, args.after)
    releasable = [path for path in files if path not in NON_RELEASE_FILES]
    if not releasable:
        print("No releasable changes; VERSION validation skipped.")
        return 0

    old = version_at(args.before)
    new = version_at(args.after)
    migration_added = any(
        MIGRATION_RE.search(path)
        and not EXCLUDE_RE.search(path)
        and git("diff", "--diff-filter=A", "--name-only", args.before, args.after, "--", path)
        for path in releasable
    )
    expected = expected_version(old, migration_added)
    if new != expected:
        kind = "MINOR" if migration_added else "PATCH"
        raise SystemExit(
            f"VERSION must be {format_version(expected)} ({kind} bump from "
            f"{format_version(old)}) in the same change; found {format_version(new)}."
        )

    print(
        f"VERSION {format_version(old)} -> {format_version(new)} is valid "
        f"({'migration/MINOR' if migration_added else 'PATCH'})."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
