# EUROERP — Project specification

> Project specification and start point for EUROERP.

## Purpose

EUROERP is the new ERP system for Eurobus4.
We did a similar, big project, big migration, from LionAquaGitRef to ERPCOM3.
LionAquaGitRef was a legacy ASP.NET 4.5 ERP system. We migrated everything to a new .NET 8 structure. Now running on Linux and also with CI/CD in Git.
The idea here is to do the same.
Eurobus4 is a version of LionAquaGitRef that was developed for another customer. They are similar but have **considerable differences**, because both companies are different and have specific requirements and specific customizations.

**Eurobus ≠ Aquanimal.** Do not treat ERPCOM3 as a drop-in spec for EUROERP. ERPCOM3 was built for Aquanimal (LionAquaGitRef); EUROERP must match **Eurobus4** behavior. Use ERPCOM3 only for .NET 8 / Blazor / Clean Architecture patterns when the legacy behavior is the same.

So, I believe you need first to compare LionAquaGitRef to Eurobus4, then check what was done to transform LionAquaGitRef to ERPCOM3 and use this check to do the same for Eurobus4, **paying attention to Eurobus4 customizations**.

## Reference projects (read-only)

Use these for context only. **Never edit them.**

| Folder | Role |
|--------|------|
| `LionAquaGitRef/` | Legacy system (ASP.NET 4.5) for **Aquanimal** — diff baseline only |
| `ERPCOM3/` | Reference ERP (.NET 8 + Blazor) migrated from LionAquaGitRef for **Aquanimal** — patterns, not Eurobus behavior |
| `Eurobus4/` | **Legacy source of truth** for this migration — screens, fields, business rules |

All code changes belong in **`EUROERP/`** only. Must be migrated from Eurobus4.

## Stack

| Layer | Technology |
|-------|------------|
| Runtime | Same as ERPCOM3 (.NET 8) |
| UI | Blazor Server — same as ERPCOM3 |
| Database | SQL Server — schema in `docs/database_schema.md` (via `user-mssql_euroerp_dev` MCP) |
| Auth | Same as ERPCOM3 — cookie authentication (legacy `aspnet_*` tables) |

## Key folders

<!-- TODO: Update once solution structure exists -->

| Path | Contents |
|------|----------|
| `docs/` | Project documentation |
| `docs/MIGRATION_PLAN.md` | Phases and migration strategy |
| `docs/epics-stories.md` | Backlog (Leonardo owns status) |
| `STORY_PLAN/` | Per-story implementation plans |
| `src/` | Application source (Web, Application, Domain, Infrastructure) |

## Local run

```powershell
cd C:\OPUSGit\EUROERP
dotnet build
dotnet run --project src\EUROERP.Web
```

Open `https://localhost:7224` (or `http://localhost:5058`). Login uses legacy `aspnet_*` tables (`Authentication:ApplicationName` = `LionSystem`, database LionEBDev).

## Workflow

1. **Facilitator** (Leonardo) defines epics and stories in `docs/epics-stories.md` _(create when ready)_.
2. **Status READY** on a story means planning and development may proceed when requested.
3. **AI** creates an implementation plan in `STORY_PLAN/Epic{N}-Story{M}-<brief-description>.md`.
4. After plan approval, **implement** only in `EUROERP/`.
5. Reference projects may be **read** for legacy behavior and patterns — never modified.

## Related docs

- `docs/MIGRATION_PLAN.md` — phases, exclusions, Eurobus vs ERPCOM3
- `docs/epics-stories.md` — epics and stories backlog
- `docs/database_schema.md` — SQL Server schema (LionEBDev, exported via MCP)
- `docs/ARCHITECTURE.md` — technical design _(to be created)_
