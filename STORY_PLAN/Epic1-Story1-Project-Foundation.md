# Epic 1 – Story 1: Project .NET 8 + Blazor Foundation

**Status:** Done  
**Brief:** Project .NET 8 + Blazor foundation (monolithic, clean architecture).

---

## 1. Objective

Create the EUROERP solution in .NET 8 + Blazor Server — monolithic, simplified Clean Architecture — ready for gradual migration from **Eurobus4** (ASP.NET 4.5). Main layout and menu come in Story 1.2.

---

## 2. Scope

- .NET 8 solution: Web, Application, Domain, Infrastructure
- Blazor Server host with default home page
- DI registration, appsettings, logging
- SQL Server connection string (LionEBDev schema — see `docs/database_schema.md`)
- **No** business pages, menu, or auth yet

---

## 3. Stack

| Item | Choice |
|------|--------|
| Runtime | .NET 8 |
| UI | Blazor Server |
| Database | SQL Server (existing Eurobus schema) |
| Data access | **Dapper** + `Microsoft.Data.SqlClient` |

Mirror **ERPCOM3** project layout; naming: `EUROERP.*` instead of `ERPCOM3.*`.

---

## 4. Solution structure

```
EUROERP/
├── src/
│   ├── EUROERP.Web/
│   ├── EUROERP.Application/
│   ├── EUROERP.Domain/
│   └── EUROERP.Infrastructure/
├── docs/
└── STORY_PLAN/
```

**Dependencies:** Web → Application, Infrastructure | Application → Domain | Infrastructure → Application, Domain | Domain → (none)

---

## 5. Projects to create

1. **EUROERP.Domain** — entities, enums (minimal placeholder OK)
2. **EUROERP.Application** — service interfaces, DTOs
3. **EUROERP.Infrastructure** — Dapper, `IDbConnection` factory, SQL access
4. **EUROERP.Web** — Blazor Server, `Program.cs`, DI

---

## 6. Configuration

- `appsettings.json` / `appsettings.Development.json`: connection string `DefaultConnection` → LionEBDev
- Do not commit secrets; use User Secrets or env vars locally
- Reference schema: `docs/database_schema.md`

---

## 7. References (read-only)

| Project | Use |
|---------|-----|
| `Eurobus4/` | Legacy behavior source |
| `ERPCOM3/src/` | Implementation template (copy structure, rename) |
| `LionAquaGitRef/` | Diff only when Eurobus differs |

**Edit only `EUROERP/`.**

---

## 8. Out of scope

- Menu / layout (Story 1.2)
- Login / aspnet membership (Story 1.3)
- Business modules

---

## 9. Acceptance criteria

- [ ] Solution builds on .NET 8
- [ ] Four projects with correct dependency direction
- [ ] Blazor Server runs; default page loads
- [ ] Connection string configured
- [ ] No changes outside `EUROERP/`

---

## 10. Links

- `docs/PROJECT.md`
- `docs/MIGRATION_PLAN.md`
- `docs/epics-stories.md` — Epic 1, Story 1.1
