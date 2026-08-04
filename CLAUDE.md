# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repo shape

This is an **architecture portfolio**: the same ride-sharing domain (Identity/Driver/Ride/Hub services) implemented multiple times, once per architectural style, so each variant can be inspected in isolation. See [README.md](README.md) for the full rationale and a comparison table.

```
Plugin.Microservices.CQRS/    → plugin-composed microservices + CQRS/Hikyaku (original implementation)
Clean.Architecture/           → Domain/Application/Infrastructure/Presentation layering
Hexagonal.Architecture/       → ports & adapters
Vertical.Slice.Architecture/  → feature-folder slices, no horizontal layers
Modular.Monolith/             → same bounded contexts, single deployable process
Commons/                      → infra-agnostic shared code only (no domain/CQRS logic)
```

Each architecture folder is a **self-contained .NET solution** with its own `CLAUDE.md` documenting its commands, layout, and the rationale for that style. Read that file before working inside a specific folder — do not assume commands or paths from one variant apply to another.

## Rules for this repo specifically

- Never move domain/business logic into `Commons/`. Each architecture folder must stand on its own — that isolation is the entire point of the portfolio. `Commons/` is for things that are genuinely infra-agnostic across all variants (e.g. shared docker infra, test container fixtures), not a shortcut to avoid re-implementing a handler.
- When adding a new architecture variant, mirror the same domain (Identity/Driver/Ride/Hub) and give it its own `CLAUDE.md` + `README.md`, and add a row to the comparison table in the root `README.md`.
- Root `README.md` is the portfolio index; keep architecture-specific detail in each folder's own README instead of growing the root one.
