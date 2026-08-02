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

    # Keyed by (package, class, line number) rather than summed per file: the .NET coverage
    # collector emits one cobertura report per test assembly, and a production assembly merely
    # *loaded* (via a transitive reference) by more than one test project shows up in more than
    # one report -- often with near-zero hits in the report belonging to the project that never
    # actually calls into it. Summing those reports independently would count the same lines
    # multiple times and could dilute or inflate the aggregate. Deduping by line and treating a
    # line as covered if any report saw a hit gives one honest count per line.
    seen = set()
    line_hit = {}

    for f in files:
        root = ET.parse(f).getroot()
        for pkg in root.iter("package"):
            name = pkg.get("name")
            if name not in wanted:
                continue
            seen.add(name)
            for cls in pkg.iter("class"):
                cls_name = cls.get("name")
                if exclude_re and exclude_re.search(cls_name):
                    continue
                for line in cls.iter("line"):
                    key = (name, cls_name, line.get("number"))
                    hit = int(line.get("hits")) > 0
                    line_hit[key] = line_hit.get(key, False) or hit

    missing = wanted - seen
    if missing:
        # Two different causes, and the distinction is not visible from the report -- an absent
        # assembly looks identical either way:
        #   1. no test project loads it, so its (probably near-zero) coverage never counts;
        #   2. it has no instrumentable code at all, so the collector emits no <package> for it
        #      even though tests do load it. Assemblies of pure auto-property DTO/message types
        #      hit this -- an auto-property compiles to no sequence points, so a project of
        #      nothing but request/response shapes is invisible to coverage by construction.
        # Case 1 is the bug this check exists to catch. Case 2 means the assembly should not be
        # in --packages: it can never carry a coverage signal, so listing it only ever fails.
        print(f"FAIL: these packages have no coverage data: {sorted(missing)}", file=sys.stderr)
        print("      Either no test project loads them (write tests), or they contain no", file=sys.stderr)
        print("      instrumentable code (pure DTO/message assemblies -- drop from --packages).", file=sys.stderr)
        sys.exit(1)

    per_package = {}
    for (pkg_name, _cls_name, _line_number), hit in line_hit.items():
        c, v = per_package.get(pkg_name, (0, 0))
        per_package[pkg_name] = (c + (1 if hit else 0), v + 1)

    covered = sum(c for c, _v in per_package.values())
    valid = sum(v for _c, v in per_package.values())

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
