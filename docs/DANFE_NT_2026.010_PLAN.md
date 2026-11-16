# Plan: DANFE NT 2026.010 + IBS/CBS product params (deadline 01/12/2026)

## Goal

1. Update EUROERP DANFE PDF (`NfePdfGenerator.cs` / QuestPDF) so the printed/PDF document shows IBS, CBS and IS per [NT 2026.010 v1.00](https://www.fiscoscan.com.br/norma/nota-tecnica-2026-010-2026).
2. Keep **company-wide defaults** (`/diretoria/ibs-cbs` / `SYS_CONTROL`) and add **per-product** IBS/CBS parameters (CST + `cClassTrib`, with defaults as fallback).

XML already calculates IBS/CBS from company defaults; DANFE work is **print layout**. Product-level params are an **emission/cadastro** follow-up so CST/`cClassTrib` can vary by item.

Approved by Ato Técnico Conjunto RFB/CGIBS nº 8/2026. Production mandatory from **01/12/2026** (no separate homologation window for the print layout).

## Current state

### DANFE
- DANFE is A4 portrait QuestPDF in `src/EUROERP.Infrastructure/NFe/NfePdfGenerator.cs`
- Totals show ICMS/IPI/PIS/COFINS style blocks — **no IBS/CBS/IS block**
- Product table is classic ICMS columns; item rows even hardcode some ICMS cells as `0,00`
- XML parser (`LoadDanfeDataFromXml`) does **not** read `IBSCBS` / `IBSCBSTot` / `CRT`
- No landscape variant (optional)

### IBS/CBS config today
- Company defaults: `/diretoria/ibs-cbs` → `SYS_CONTROL` (`IBS_CST`, `IBS_CCLASSTRIB`, `IBS_ALIQ_UF`, `IBS_ALIQ_MUN`, `CBS_ALIQ`)
- Emission (`NfeXmlBuilder` / `NfeIndividualService`) uses those defaults for every item

### What already exists on PRODUCT (legacy — not IBS/CBS)
| Field | Meaning today | Reuse for IBS/CBS? |
|-------|---------------|--------------------|
| `CST_ID` → table `CST` | Legacy ICMS CST | **No** — different tax, different codes |
| `CSTB_ID` → table `CSTB` | ICMS/CSOSN mapping used for Simples (`MapCstbToCsosn`) + CFOP via `CST_CFOP` | **No** — keep for ICMS; do not overload |
| `FISCAL_CLASS_ID` → `FISCAL_CLASS` | NCM / fiscal class (also IPI/ICMS ST flags) | **No** — NCM stays; not `cClassTrib` |

**Conclusion:** keep `CST` / `CSTB` / `FISCAL_CLASS` as they are (still needed for ICMS/NCM). They are **not** the Reforma IBS/CBS fields and should **not** be deprecated for that reason. Add **new** product fields for IBS/CBS.

---

## Phase 1 — Data model & XML parse

**File:** `NfePdfGenerator.cs` (`DanfeData`, `DanfeProduto`, `LoadDanfeDataFromXml`)

1. Extend `DanfeData` with totals:
   - `VCbs`, `VIbsUf`, `VIbsMun`, `VIs`
   - `VIbsMono`, `VCbsMono`, `VIbsMonoReten`, `VCbsMonoReten` (empty if absent)
   - `Crt` (+ display label)
   - Optional: `TipoRegimeApuracaoIbsCbs` reserved blank
2. Extend `DanfeProduto` with item RTC fields:
   - `CClassTrib`, `VBcIbsCbs`
   - `PIbsUf` / `VIbsUf`, `PIbsMun` / `VIbsMun`, `PCbs` / `VCbs`
   - `VBcIs`, `PIs`, `VIs`
3. Parse from XML:
   - `emit/CRT`
   - `det/imposto/IBSCBS` (+ `gIBSCBS`, `gIBSUF`, `gIBSMun`, `gCBS`, and `IS` if present)
   - Aliquota rule: if `gRed` → use `pAliqEfet`; else `pIBSUF` / `pIBSMun` / `pCBS`
   - `total/IBSCBSTot` (+ `ISTot` if present)
4. Fix existing bug while touching items: bind `VBcIcms` / `VIcms` / `VIpi` instead of hardcoded `0,00`

---

## Phase 2 — Emitente block

In the emitente identification area, add:

- **Código do Regime Tributário** (`CRT` → e.g. `3 - REGIME NORMAL`)
- Empty reserved label **Tipo de Regime de Apuração do IBS e da CBS** (no value until future NT)

---

## Phase 3 — Totals block “TOTAL DO IBS / CBS / IS”

Insert **immediately after** the ICMS/IPI totals section (before transportador), matching NT fields:

| Printed | XML |
|---------|-----|
| Valor da CBS | `vCBS` |
| Valor do IBS UF | `vIBSUF` |
| Valor do IBS Município | `vIBSMun` |
| Valor do Imposto Seletivo | `vIS` |
| IBS / CBS monofásicos + retenções | only if present in XML |

Keep current product/freight/discount totals; don’t invent missing mono/IS values.

---

## Phase 4 — Product grid redesign

NT item layout is denser than today’s 14-column ICMS table. Recommended approach for A4 portrait:

### Option A (preferred for compliance)

Multi-line item row like the NT sample:

- Line 1: code, description, NCM, `cClassTrib`, CST/CFOP, qty, unit, values
- Line 2: bases (ICMS / IBS-CBS / IS / IPI)
- Line 3: aliquots + tax amounts (ICMS, IBS UF, IBS Mun, CBS, IS, IPI)

### Option B

Keep flat table and add many columns (likely unreadable on A4).

**Decision:** implement **Option A**, still QuestPDF table/column, font ~5–6pt, preserve page flow with many items.

---

## Phase 5 — Conditional / optional fields

Only print when XML has data:

- FCP / FCP ST / DIFAL / FCP UF dest
- ICMS mono / mono retenção
- IBS/CBS mono fields
- ISSQN block (not used today for goods NFe — leave suppressible)
- Transport / volumes (already present)
- QR Code: leave as-is for now; NT says more detail comes in a later NT

Rule: do **not** invent or infer values that are not in the XML.

---

## Phase 6 — Verify & harden

1. Regenerate PDF from an authorized note that already contains IBS/CBS and check:
   - CRT `3`
   - Totals: CBS, IBS UF, IBS Mun, IS blank/0 when absent
   - Item: `cClassTrib`, BC IBS/CBS, aliquots and values match XML
2. Visual check vs NT vertical reference (canhoto, totals order, item readability)
3. Turn `ShowSectionMarkers` off for production if still on
4. Optional later: landscape layout (`tpImp=2`) — not required for first delivery

---

## Phase 7 — Per-product IBS/CBS parameters (keep company defaults)

**Intent:** company defaults remain; each product can override CST / `cClassTrib`. Aliquotas padrão stay company-wide for 2026 transition unless a later need appears for product-level effective rates.

### 7.1 Data model
1. Add columns on `PRODUCT` (names TBD, e.g.):
   - `IBS_CST` `varchar(3)` NULL — empty = use company default
   - `IBS_CCLASSTRIB` `varchar(6)` NULL — empty = use company default
2. Optional later (not required for first cut):
   - product-level IS flags / `cClassTribIS` if Imposto Seletivo applies to specific goods
3. Do **not** repurpose `CST_ID` / `CSTB_ID` / `FISCAL_CLASS_ID`

### 7.2 Cadastro UI
1. Product create/edit (`CadastroNovo` / `CadastroEditar`) and mass info if applicable:
   - fields **CST IBS/CBS** and **cClassTrib**
   - placeholder/hint: “vazio = usa padrão Diretoria → IBS/CBS”
2. Keep existing CST / CSTB / Classe fiscal sections labeled as **ICMS / NCM** so users don’t confuse them with Reforma fields

### 7.3 Emission wiring
1. `GetOrderDetailsForNfeAsync`: select product `IBS_CST` / `IBS_CCLASSTRIB`
2. `NfeDetInput`: carry per-line CST / `cClassTrib`
3. `NfeXmlBuilder` `AppendDet`:
   - use item override when filled
   - else fall back to `NfeBuildInput` company defaults
4. Aliquotas (`pIBSUF` / `pIBSMun` / `pCBS`): keep from company defaults for now
5. DANFE already prints per-item values from XML once Phases 1–4 are done — no extra DANFE work beyond parsing

### 7.4 Migration / defaults strategy
1. New columns nullable; existing products keep using company defaults until filled
2. Seed/mass-update optional for companies that only use `000` / `000001`
3. Document that legacy CST/CSTB remain for ICMS and are **not** deprecated by this work

---

## Out of scope

- Replacing or removing legacy `CST` / `CSTB` / `FISCAL_CLASS` (still required for ICMS/NCM)
- Per-product aliquotas (unless needed later for `pAliqEfet` / reductions)
- `vUnComLiq` / `vProdLiq` / `vProdLiqTot` (NT 2026.008 — emission, not DANFE 2026.010)
- Pagamento antecipado / `vICMSPrevisto`
- NFS-e DANFSe product-level IBS/CBS (separate if needed)
- QR Code full NT follow-up

---

## Suggested delivery order

1. Parse + model (Phase 1)
2. Totals + CRT (Phases 2–3) — biggest DANFE compliance win
3. Item grid (Phase 4)
4. Conditionals + PDF smoke test (Phases 5–6)
5. Per-product IBS CST / `cClassTrib` + emission fallback (Phase 7)

---

## Main risks

- **DANFE:** product grid density on A4. Mitigate with multi-line items (Option A) and the NT sample as visual reference.
- **Product params:** user confusion between legacy ICMS CST/CSTB and new IBS/CBS CST/`cClassTrib`. Mitigate with clear UI labels and leaving legacy fields unchanged.

---

## Reference

- NT 2026.010 v1.00 — DANFE Reforma Tributária (Outubro/2026)
- Production: **01/12/2026**
- Layouts of reference: DANFE Modelo 55 Vertical (main) and Horizontal (alt)
