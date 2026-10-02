#!/usr/bin/env python3
"""Bump the repo-root VERSION file after a merge to main.

A newly added EF migration under **/Persistence/Migrations/*.cs (excluding
*.Designer.cs and *ModelSnapshot.cs) bumps MINOR and resets PATCH.
Anything else bumps PATCH. MAJOR is never changed here.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VERSION_FILE = ROOT / "VERSION"
MIGRATION_RE = re.compile(r"(^|/)Persistence/Migrations/[^/]+\.cs$")
EXCLUDE_RE = re.compile(r"(Designer|ModelSnapshot)\.cs$")


def parse_version(text: str) -> tuple[int, int, int]:
    parts = text.strip().split(".")
    if len(parts) != 3 or not all(p.isdigit() for p in parts):
        raise ValueError(f"VERSION must be MAJOR.MINOR.PATCH, got {text!r}")
    return int(parts[0]), int(parts[1]), int(parts[2])


def format_version(major: int, minor: int, patch: int) -> str:
    return f"{major}.{minor}.{patch}"


def next_version(current: str, migration_added: bool) -> str:
    major, minor, patch = parse_version(current)
    if migration_added:
        return format_version(major, minor + 1, 0)
    return format_version(major, minor, patch + 1)


def added_migration(before: str, after: str) -> bool:
    if not before or re.fullmatch(r"0+", before):
        return False
    result = subprocess.run(
        ["git", "diff", "--name-only", "--diff-filter=A", before, after],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    for line in result.stdout.splitlines():
        normalized = line.replace("\\", "/")
        if MIGRATION_RE.search(normalized) and not EXCLUDE_RE.search(normalized):
            return True
    return False


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--before", help="git SHA before the merge (github.event.before)")
    parser.add_argument("--after", help="git SHA after the merge (github.sha)")
    parser.add_argument(
        "--kind",
        choices=("minor", "patch"),
        help="bump kind; when omitted, inferred from --before/--after",
    )
    parser.add_argument("--print-kind", action="store_true", help="print minor|patch and exit")
    parser.add_argument("--write", action="store_true", help="write the next version to VERSION")
    args = parser.parse_args()

    if args.kind:
        kind = args.kind
    elif args.before and args.after:
        kind = "minor" if added_migration(args.before, args.after) else "patch"
    else:
        parser.error("provide --kind, or both --before and --after")

    if args.print_kind:
        print(kind)
        return 0

    current = VERSION_FILE.read_text(encoding="utf-8").strip()
    bumped = next_version(current, kind == "minor")
    if args.write:
        VERSION_FILE.write_text(bumped + "\n", encoding="utf-8")
    print(bumped)
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--self-test":
        assert next_version("1.0.0", False) == "1.0.1"
        assert next_version("1.2.3", False) == "1.2.4"
        assert next_version("1.2.3", True) == "1.3.0"
        assert next_version("1.0.9", True) == "1.1.0"
        print("ok")
        raise SystemExit(0)
    raise SystemExit(main())
