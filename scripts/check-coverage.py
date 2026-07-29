#!/usr/bin/env python3
"""Coverage gate: fails if line coverage over a set of business-logic assemblies
falls below a threshold. Reads cobertura XML produced by Microsoft.CodeCoverage
(`dotnet test --collect:"Code Coverage;Format=cobertura"`). No third-party deps.

Assemblies not in --packages are ignored entirely (e.g. Api/Infrastructure/Demo
hosts that current test projects don't load) -- see each variant's CLAUDE.md for
which layers are in scope for its coverage gate.

Some variants (Vertical.Slice.Architecture, Modular.Monolith) have no separate
project boundary between business logic and composition-root code (Program.cs,
Controllers, EF Core Persistence/DbContextFactory, FluentValidation validators,
SignalR hub) -- everything lives in one assembly. --class-exclude filters those
out by class name (regex, matched against cobertura's <class name=...>) so the
gate still measures business logic only, consistent with the variants that get
this for free via project boundaries.
"""
import argparse
import glob
import re
import sys
import xml.etree.ElementTree as ET


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("cobertura_glob", help="glob pattern for *.cobertura.xml files")
    parser.add_argument("--packages", required=True, help="comma-separated list of package/assembly names in scope")
    parser.add_argument("--class-exclude", default=None, help="regex; classes whose cobertura name matches are excluded from the count")
    parser.add_argument("--threshold", type=float, default=80.0)
    args = parser.parse_args()

    wanted = set(args.packages.split(","))
    exclude_re = re.compile(args.class_exclude) if args.class_exclude else None
    files = glob.glob(args.cobertura_glob, recursive=True)
    if not files:
        print(f"No cobertura files matched: {args.cobertura_glob}", file=sys.stderr)
        sys.exit(1)

    covered = 0
    valid = 0
    seen = set()
    per_package = {}

    for f in files:
        root = ET.parse(f).getroot()
        for pkg in root.iter("package"):
            name = pkg.get("name")
            if name not in wanted:
                continue
            seen.add(name)
            pcov = psum = 0
            for cls in pkg.iter("class"):
                cls_name = cls.get("name")
                if exclude_re and exclude_re.search(cls_name):
                    continue
                for line in cls.iter("line"):
                    psum += 1
                    if int(line.get("hits")) > 0:
                        pcov += 1
            c, v = per_package.get(name, (0, 0))
            per_package[name] = (c + pcov, v + psum)
            covered += pcov
            valid += psum

    missing = wanted - seen
    if missing:
        print(f"WARNING: these packages never appeared in any cobertura report (never loaded by tests): {sorted(missing)}", file=sys.stderr)

    print(f"{'Assembly':40} {'covered/valid':>15} {'line %':>8}")
    print("-" * 66)
    for name in sorted(per_package):
        c, v = per_package[name]
        pct = (c / v * 100) if v else 0.0
        print(f"{name:40} {c:>6}/{v:<7} {pct:7.1f}%")

    pct = (covered / valid * 100) if valid else 0.0
    print("-" * 66)
    print(f"{'TOTAL':40} {covered:>6}/{valid:<7} {pct:7.1f}%   (gate: {args.threshold:.0f}%)")

    if pct < args.threshold:
        print(f"\nFAIL: {pct:.1f}% < {args.threshold:.0f}% threshold", file=sys.stderr)
        sys.exit(1)

    print(f"\nPASS: {pct:.1f}% >= {args.threshold:.0f}% threshold")


if __name__ == "__main__":
    main()
