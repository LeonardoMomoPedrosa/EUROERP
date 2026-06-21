# EUROERP — Migration plan

> High-level plan for migrating **Eurobus4** (ASP.NET 4.5) to **EUROERP** (.NET 8 + Blazor).  
> Detailed backlog: [`epics-stories.md`](epics-stories.md).

## Goal

Replicate the Aquanimal migration (LionAquaGitRef → ERPCOM3) for Eurobus, preserving **Eurobus4 business behavior** while adopting **ERPCOM3 architecture and patterns**.

## Reference model

| Source | Use for |
|--------|---------|
| **`Eurobus4/`** | **Behavior** — screens, flows, business rules (read-only) |
| **`ERPCOM3/`** | **Target patterns** — solution structure, Blazor pages, services, SQL style (read-only) |
| **`LionAquaGitRef/`** | **Diff only** — when Eurobus4 and Aquanimal diverge, compare both legacies (read-only) |
| **`docs/database_schema.md`** | SQL Server schema (LionEBDev); excludes unused modules |

## Out of scope (not migrated)

These exist in the legacy DB or Aquanimal ERP but **are not used in EUROERP**:

| Module | Reason |
|--------|--------|
| `COMMISSION*` | Not used (Eurobus) |
| `EMAIL` / `EMAIL_BATCH*` | Not used |
| `MORTALITY` / `MORT_*` | Not used |
| E-Com / Mercado Livre | Not in Eurobus4 menu |
| Baterias / `tankparam` | Aquanimal-only |
| Comissão (menu Diretoria) | Excluded with COMMISSION tables |
| DRE | Not needed in initial migration |

## Eurobus-specific (must implement)

Compared to Aquanimal / ERPCOM3 menu:

| Feature | Legacy (Eurobus4) |
|---------|-------------------|
| **OS** (Ordem de Serviço) | Terminology and flows under Vendas — not generic “pedido/venda” only |
| **Garantia** | `principal/warranty/*` |
| **Atendimento (CALL)** | `principal/call/*` |
| **Clientes: Créditos, Frota, Higienização** | `clients/credit`, `clients/car`, `clients/higienic_list*` |
| **Entrada em lote** | `stock/stock_in2.aspx` |
| **NFe: Enviar NFe** | `receipt.aspx` (Eurobus) vs `receiptSync.aspx` (Aquanimal) — follow **Eurobus4** |
| **NFe: NFES (NFS-e serviços)** | `receipt.aspx` → `printNFEServices` — Prefeitura SP, not SEFAZ; **interim EUROERP target** while legacy handles rest |
| **NFe: Cancelar NFES (batch)** | `sales/nfe/cancel_nfes.aspx` |
| **Mercados (users)** | `manager/members/userMarket.aspx` — table `MARKET*` |

## Shared with ERPCOM3 (reuse patterns)

Foundation, auth, products, suppliers, clients (core), stock (manual + purchase), lists, orders/OS editor, finance (AP/AR/BTP/BTR), NFe core, reference data, users/roles/activities, cash flow, revenue reports.

When planning a story: **read Eurobus4 first**, then **copy/adapt ERPCOM3 implementation** only where behavior matches.

### Eurobus ≠ Aquanimal — do not skip this

ERPCOM3 encodes **Aquanimal** customizations. EUROERP must encode **Eurobus4** customizations. Shared table names and similar menus are misleading: field sets, validations, and formulas often differ.

| Area | Aquanimal / ERPCOM3 | Eurobus4 |
|------|---------------------|----------|
| Entrada manual (`stock_in`) | GTA, Caixa/Isopor | II, ICMS, PIS, COFINS, NFE, IPI, CSTB, rateio |
| Clientes | Core CRUD | + Créditos, Frota, Higienização por cliente |
| Vendas | Pedido/venda | OS (ordem de serviço) |
| NFe emissão | `receiptSync.aspx` | `receipt.aspx` |
| Estoque em lote | — | `stock_in2.aspx` |

**Rule:** If Eurobus4 and ERPCOM3 disagree, implement Eurobus4. Use ERPCOM3 for project structure and coding style only.

## Phases

### Phase 1 — Foundation (Epic 1)

Solution bootstrap, configurable menu (Eurobus structure), login (aspnet membership).

**Exit:** App runs, user logs in, menu matches Eurobus top-level areas.

### Phase 2 — Master data (Epics 2–5)

Products, suppliers, clients (+ Eurobus client extras), warranty.

**Exit:** CRUD and mass-update flows for core entities.

### Phase 3 — Stock & lists (Epics 6–7)

Stock in (manual, bulk, purchase), asset reports, product lists.

**Exit:** Inventory operations match Eurobus4.

### Phase 4 — Sales / OS (Epics 8–11)

OS creation, editor, payment, search, reports, atendimento, billing (faturar OS).

**Exit:** Full OS lifecycle except NFe emission.

### Phase 5 — Fiscal (Epic 12)

NFe emission, print, cancel, CC-e, reports, download, service status.

**Exit:** SEFAZ integration parity with Eurobus4.

### Phase 6 — Finance (Epics 13–15)

AP, AR, revenue, cash flow, delinquent clients.

**Exit:** Finance module parity.

### Phase 7 — Platform (Epics 16–19)

Reference data, admin/security, external APIs, dashboard/widgets.

**Exit:** Admin and integrations ready for production cutover.

### Phase 8 — Optional (Epic 20)

Mobile-friendly layout adjustments (desktop unchanged).

## Execution workflow

1. **Leonardo** updates status in `epics-stories.md` (`READY` → proceed).
2. **AI** creates `STORY_PLAN/Epic{N}-Story{M}-<brief>.md` before coding.
3. **Leonardo** approves plan → implement in **`EUROERP/`** only.
4. Compare **Eurobus4** behavior; use **ERPCOM3** as implementation template when equivalent.
5. Schema: `docs/database_schema.md` + MCP `user-mssql_euroerp_dev` for live checks.

## Target menu (from Eurobus4)

Top-level (config file, role-filtered later):

| Top menu | Eurobus4 section |
|----------|------------------|
| Principal | Produtos, Fornecedores, Clientes, Garantia, Estoque, Listas |
| Vendas | Nova OS, Consultar OS, Relatórios, Atendimento, OS não faturada, Faturar OS, NFe |
| Financeiro | Contas a Pagar, Contas a Receber, Faturamento, Fluxo de Caixa, Liberar Cliente |
| Referência | Grupos, Classificação fiscal, Moedas |
| Diretoria | Usuários, Atividades, Master, Alíq. ICMS *(no Comissão)* |
| Cadastro | Alterar senha/e-mail |

## First step

**Epic 1 — Story 1.1** is `READY`: .NET 8 + Blazor foundation.  
Plan: [`STORY_PLAN/Epic1-Story1-Project-Foundation.md`](../STORY_PLAN/Epic1-Story1-Project-Foundation.md).

## Related docs

- [`PROJECT.md`](PROJECT.md) — project specification
- [`database_schema.md`](database_schema.md) — SQL schema
- [`epics-stories.md`](epics-stories.md) — full backlog
