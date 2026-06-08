---
description: EUROERP is the target project; reference projects are read-only
alwaysApply: true
---

# Target project

**`EUROERP/`** is the active project. You **may** edit, create, update, and delete files here.

# Reference projects (read-only)

The following projects are **reference only** and **must never be modified**:

- `LionAquaGitRef/`
- `ERPCOM3/`
- `Eurobus4/`

## Rules

- **Never** edit, create, or delete files in the reference projects.
- **Never** suggest changes to files in the reference projects.
- You **may read** files in the reference projects for context and reference.
- **All** code changes must be made only in `EUROERP/`.
- Before coding, read `docs/PROJECT.md` (and related docs it links to).

## Eurobus ≠ Aquanimal (critical)

**Eurobus4 and Aquanimal (LionAquaGitRef / ERPCOM3) are not the same product.** They share a common legacy base but have **considerable customer-specific differences** — screens, fields, formulas, and flows.

| Reference | Role |
|-----------|------|
| **`Eurobus4/`** | **Source of truth for behavior** — what the user sees and what business rules must do |
| **`ERPCOM3/`** | **Implementation template only** — stack, folder layout, Blazor/Dapper patterns when Eurobus matches |
| **`LionAquaGitRef/`** | Use when **Eurobus4 vs ERPCOM3 disagree** — confirms Aquanimal-only vs shared legacy |

**Never assume ERPCOM3 is correct for EUROERP.** If a story touches stock, clients, sales/OS, NFe, or finance, **read the matching Eurobus4 `.aspx` / controller first** (`Eurobus4/resource_files/menu.xml` for menu routes). Copy from ERPCOM3 only after confirming Eurobus4 behavior is the same.

**Examples where Eurobus differs from Aquanimal/ERPCOM3:**

- **Entrada manual de estoque** — Eurobus `stock_in.aspx`: II/ICMS/PIS/COFINS, NFE, IPI, CSTB, rateio; **not** GTA/Caixa (Aquanimal).
- **Clientes** — Créditos, Frota, Higienização por cliente (Eurobus-only).
- **Vendas** — OS terminology and flows, not generic “venda” only.
- **NFe** — `receipt.aspx` (Eurobus) vs `receiptSync.aspx` (Aquanimal).

When in doubt: **Eurobus4 wins for behavior; ERPCOM3 wins for architecture.**
