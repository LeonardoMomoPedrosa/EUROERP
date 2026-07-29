**This file is modified by Leonardo only.**  
It contains epics and story progress for the Eurobus4 → EUROERP migration.

Status values: `Pending` | `Ready` | `Done`  
Only **Ready** stories may be planned and implemented when Leonardo requests.

Legacy source: **`Eurobus4/`** (read-only). Patterns: **`ERPCOM3/`** (read-only). Schema: **`docs/database_schema.md`**.

---

## EPIC 1 — Foundation

**Story 1.1 — Project .NET 8 + Blazor foundation** — **STATUS: Done**  
Monolithic Clean Architecture (Web, Application, Domain, Infrastructure). Same stack as ERPCOM3. Dapper + SQL Server. No business pages yet.  
Plan: `STORY_PLAN/Epic1-Story1-Project-Foundation.md`

**Story 1.2 — Main layout (Eurobus menu)** — **STATUS: Done**  
Configurable menu from file. Top menus: Principal, Vendas, Financeiro, Referência, Diretoria, Cadastro (+ Sair). Left submenu tree. Role filtering later.  
Reference menu: `Eurobus4/resource_files/menu.xml`  
Pattern: ERPCOM3 Epic 1 Story 1.2

**Story 1.3 — Login page** — **STATUS: Done**  
Cookie auth reusing `aspnet_*` tables. Redirect to dashboard after login. Authorization deferred.  
Pattern: ERPCOM3 Epic 1 Story 1.3

---

## EPIC 2 — Products (Produtos)

**Story 2.1 — Produtos → Cadastro** — **STATUS: Done**  
Single page: list, search, create, edit.  
Legacy: `Eurobus4/principal/products/new_product.aspx`, `search_product.aspx`, `edit_product.aspx`

**Story 2.2 — Produtos → Histórico** — **STATUS: Done**  
Product timeline (stock in/out, orders).  
Legacy: `Eurobus4/principal/products/history.aspx`

**Story 2.3 — Produtos → Alterar em massa (Descrição)** — **STATUS: Done**  
Row auto-save on change (ajax), fadeout feedback.  
Legacy: `Eurobus4/principal/products/update_mass_info.aspx`  
Pattern: ERPCOM3 Story 2.3

**Story 2.4 — Produtos → Alterar em massa (Custos)** — **STATUS: Done**  
Same UX as 2.3; cost rules from 2.1.  
Legacy: `Eurobus4/principal/products/update_mass_cost.aspx`

---

## EPIC 3 — Suppliers (Fornecedores)

**Story 3.1 — Fornecedores → Cadastro** — **STATUS: Done**  
Legacy: `Eurobus4/principal/suppliers/new_supplier.aspx`, `search_supplier.aspx`

**Story 3.2 — CEP → Estado/Cidade (ViaCEP)** — **STATUS: Done**  
Reusable component (clients reuse). City match/insert in `CITY`.  
Pattern: ERPCOM3 Story 3.2

**Story 3.3 — Supplier search autocomplete** — **STATUS: Done**  
Legacy: supplier search page behavior

**Story 3.4 — Fornecedores → Alterar em massa** — **STATUS: Done**  
Legacy: `Eurobus4/principal/suppliers/update_mass_pre.aspx`, `update_mass.aspx`

---

## EPIC 4 — Clients (Clientes)

**Story 4.1 — Clientes → Cadastro** — **STATUS: Done**  
Legacy: `Eurobus4/principal/clients/new_client.aspx`, `search_client.aspx`, `edit_client.aspx`  
Use CEP component from Story 3.2.

**Story 4.2 — Client search autocomplete** — **STATUS: Done**

**Story 4.3 — Clientes → Descontos** — **STATUS: Done**  
Legacy: `Eurobus4/principal/clients/discounts.aspx`

**Story 4.4 — Clientes → Créditos** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/clients/credit/credit_insert.aspx`

**Story 4.5 — Clientes → Frota (CAR)** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/clients/car/car_insert.aspx`  
Tables: `CAR`, `CLIENT`

**Story 4.6 — Clientes → Higienização** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/clients/higienic_list.aspx` (6–12 meses, marcar processado), `higienic_list_2.aspx` (por cliente, &gt;180 dias)

**Story 4.7 — Lista de vendedores** — **STATUS: Done**  
Legacy: `Eurobus4/principal/clients/sales_agent_list.aspx`

**Story 4.8 — Clientes → Alterar em massa** — **STATUS: Done**  
Legacy: `Eurobus4/principal/clients/update_mass.aspx`

---

## EPIC 5 — Warranty (Garantia) *(Eurobus)*

**Story 5.1 — Nova garantia** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/warranty/new_warranty.aspx`, `warranty_details.aspx`

**Story 5.2 — Consulta garantia** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/warranty/search_warranty.aspx`

---

## EPIC 6 — Stock (Estoque)

**Story 6.1 — Entrada manual (wizard)** — **STATUS: Done**  
Legacy: `Eurobus4/principal/stock/stock_in.aspx`  
Pattern: ERPCOM3 Epic 5 Story 5.1

**Story 6.2 — Consulta entradas** — **STATUS: Done**  
Legacy: `Eurobus4/principal/stock/stock_in_search.aspx`

**Story 6.3 — Entrada em lote** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/stock/stock_in2.aspx`

**Story 6.4 — Pedido de compra → Entrar estoque** — **STATUS: Done**  
Legacy: `Eurobus4/principal/list/gnrl_ordering.aspx`, `stock/purchase_stock_list.aspx`, `purchase_stock_in.aspx`

**Story 6.5 — Relatório de ativos** — **STATUS: Done**  
Legacy: `Eurobus4/principal/stock/stock_value_search.aspx`, `stock_supplier_value_search.aspx`

---

## EPIC 7 — Product lists (Listas)

**Story 7.1 — Lista geral (PDF/Excel)** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/list/product_list_search.aspx`  
Note: Eurobus uses single “Geral” list (no separate Animais page).

---

## EPIC 8 — Service orders / OS (Vendas) — **DONE**

All stories complete (8.1–8.7). Full open/closed OS flow in EUROERP; billing/NFe remain Epics 11–12.

**Story 8.1 — Nova OS — first screen** — **STATUS: Done** *(Eurobus)*  
Client + sales agent selection; recent OS list.  
Legacy: `Eurobus4/principal/sales/new_sale_create_order_number.aspx`

**Story 8.2 — OS editor (open status)** — **STATUS: Done** *(Eurobus)*  
Cart, discounts, F2 product search, credit/shipment amounts, fleet (truck) assignment.  
Legacy: `Eurobus4/principal/sales/new_sale.aspx` and related components

**Story 8.3 — OS payment (BTR)** — **STATUS: Done** *(Eurobus)*  
Payment screen (`OsPagamento.razor` → `OsFinalizado.razor`). Creates `FINANCE_BTR` + `FINANCE_BTR_DETAIL`; auto baixa (`FINANCE_RECEIVE`) **only for dinheiro (payment method 4)**. Cheque / `FINANCE_CHECK` **not** implemented.  
Legacy: `Eurobus4/principal/sales/sale_btr.aspx`  
Tables: `FINANCE_BTR`, `FINANCE_BTR_DETAIL`, `FINANCE_RECEIVE`

**Story 8.4 — OS activities (read-only + actions)** — **STATUS: Done** *(Eurobus)*  
Reopen, print (types 1–3), packing slip, labels on closed OS (`Os.razor`).  
Legacy: order detail/print pages under `Eurobus4/principal/sales/`

**Story 8.5 — Consultar OS** — **STATUS: Done** *(Eurobus)*  
Search by client, OS #, NF (`RECEIPT` / `NFES_NO`), fleet description, plate. Opens `Os.razor` (read-only when closed).  
Legacy: `Eurobus4/principal/sales/search_sale.aspx` and related search pages

**Story 8.6 — Desconto por produto na OS** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/sales/discountProduct.aspx`  
Per-line discount % and optional new list price on open OS (`Os.razor` modal).

**Story 8.7 — Efetuar venda (orçamento → OS)** — **STATUS: Done** *(Eurobus)*  
Convert closed orçamento (`MODE=Q`, `STATUS=F`) to venda (`MODE=S`): reserve/deduct stock, align line quantities.  
Legacy: `Eurobus4/components/sales/OrderDetailsEngine.ascx` → **Efetuar Venda** → `SalesController.performSale`

---

## EPIC 9 — Sales reports (Relatórios Vendas) — **DONE**

All stories complete (9.1–9.3). Routes under `/vendas/relatorios/*`; Eurobus SQL + XSL via `ISalesGroupReportService` / `IClientSalesRankService`.

**Story 9.1 — ABC** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/finance/invoicing/search_by_group.aspx` (+ result pages)  
**EUROERP:** `Abc.razor` → `/vendas/relatorios/abc`; resultados with tabs Grupos/Produtos/Clientes/Vendedores/Técnicos/Cort-Garant.; drill-downs under `/vendas/relatorios/abc/...`.

**Story 9.2 — Minhas vendas** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/sales/mySalesInput.aspx`  
**EUROERP:** `MinhasVendas.razor` → `/vendas/relatorios/minhas-vendas` (filter by logged-in `SALES_AGENT`, `COMMISSION=1`).

**Story 9.3 — Cliente / Vendedor** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/finance/rank/client_per_saler.aspx`  
**EUROERP:** `ClienteVendedor.razor` → `/vendas/relatorios/cliente-vendedor` (role Vendas picker; fixed ~6-month period; month cell → ABC client month).

---

## EPIC 10 — Customer service (Atendimento) *(optional — to be implemented)*

Eurobus-only (`CALL`, `CALL_STEP`, `ZONE`). Not in the current migration wave; menu routes exist with placeholder pages until stories are **Ready**.

**Story 10.1 — Criar atendimento** — **STATUS: Pending** *(to be implemented)*  
Legacy: `Eurobus4/principal/call/call_create.aspx`  
Tables: `CALL`, `CALL_STEP`, `ZONE`

**Story 10.2 — Listar atendimentos** — **STATUS: Pending** *(to be implemented)*  
Legacy: `Eurobus4/principal/call/call_search.aspx`

**Story 10.3 — Relatório atendimentos** — **STATUS: Pending** *(to be implemented)*  
Legacy: `Eurobus4/principal/call/call_report.aspx`

---

## EPIC 11 — OS billing flow — **DONE**

All stories complete (11.1–11.2). Lists pending OS (`STATUS` not `E`/`C`); faturar sets `STATUS=E` (legacy send order).

**Story 11.1 — OS não faturada** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/sales/last_orders.aspx`  
`OsNaoFaturada.razor` — list with optional product filter.

**Story 11.2 — Faturar OS** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/sales/send/send_order.aspx`  
`FaturarOs.razor` — enter OS # (status `F`) → `SendOrderAsync`.

---

## EPIC 12 — NFe

**Interim (Eurobus bridge):** Epics 8–11 are **done** in EUROERP. Product NFe (SEFAZ) Story 12.1 is **Done**; NFES bridge (12.1-NFES) is Done.

**Story 12.1 — Enviar NFe (individual)** — **STATUS: Done**  
Legacy: `Eurobus4/principal/sales/nfe/receipt.aspx` *(Eurobus flow — not receiptSync)*  
SEFAZ, certificate, validation, PDF — pattern ERPCOM3 Epic 10 Story 10.1  
**EUROERP:** `NfeEmitir.razor` at `/vendas/nfe/enviar`; CRT 3 (ICMS00 + PIS/COFINS alíquota); `infCpl` com OS/frota; XML/PDF em `NFE_files` (mesma pasta do legado).

**Story 12.1-NFES — Enviar NFES (NFS-e serviços)** — **STATUS: Done** *(Eurobus bridge)*  
Legacy: `Eurobus4/principal/sales/nfe/receipt.aspx` → `printNFEServices`.  
**EUROERP:** provider por município — **Simpliss / layout nacional DPS** para Santana de Parnaíba (`Nfes:Provider=Simpliss`, IBGE `3547304`); **Prefeitura SP RPS** opcional (`PrefeituraSp`).  
Not in ERPCOM3/Aquanimal. Updates `ORDER.NFES_NO`, `NFES_CHECK_CODE`, `RPS_NO`.  
Requires order **F** or **E** with service total &gt; 0 (legacy closes OS / BTR). No EUROERP Epic 8.3 dependency if OS is finished in legacy.

**Story 12.2 — NFe Outras / Outras (novo)** — **STATUS: Done**  
Legacy: `Eurobus4/principal/receiptin_nfe/dataInput.aspx`, `dataInput2.aspx`, `detailsInput.aspx`, `detailsInput2.aspx`  
EUROERP: `/vendas/nfe/outras`, `/vendas/nfe/outras-novo` + detalhes. Eurobus-only (`RECEIPT_IN_DATA`, `RECEIPT_IN_DETAILS`). Not in ERPCOM3.

**Story 12.3 — Imprimir / listar NFe** — **STATUS: Done**  
Legacy: `Eurobus4/principal/sales/nfe/status.aspx`  
**EUROERP:** `NfeImprimir.razor` at `/vendas/nfe/imprimir` — search by OS, Últimas Saídas (NFe + NFES) + Últimas Entradas, detail with PDF/XML via `/NFE_FILES/` (ERPCOM3 pattern). No SEFAZ poll / email (same as ERPCOM3 10.2).

**Story 12.4 — Cancelar NFe** — **STATUS: Done**  
Legacy: `Eurobus4/principal/sales/nfe/cancel.aspx` *(radio “Cancelar NF”)*  
**EUROERP:** `NfeCancelar.razor` at `/vendas/nfe/cancelar` — SEFAZ evento 110111 via `CancelNfeAsync`; grid of today's `RECEIPT_CANCEL`. Inutilização de número is a separate story (**12.4-INUT**).

**Story 12.4-INUT — Inutilizar NFe** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/sales/nfe/cancel.aspx` *(radio “Inutilizar número da NF” → `voidNFE`)* + `NfeController.inutilizaNfe` / `NFeInutilizacao4`  
Same screen shape as cancel (date, NF number, motivo ≥15 chars; today’s cancels/inuts grid), but SEFAZ **inutilização** (`TInutNFe` / `retInutNFe`, model 55, serie 0, `nNFIni`=`nNFFin`) instead of event 110111.  
On success: persist via `cancelReceipt(..., memo + " - Prot.Inut:" + nProt)` (legacy) into `RECEIPT_CANCEL`; save signed ped-inut XML under `NFE_files/INUT/`.  
**EUROERP:** `NfeInutilizar.razor` at `/vendas/nfe/inutilizar` — `InutilizarNfeAsync` + `NfeInutilizacaoAsync` (SOAP NFeInutilizacao4); accepts cStat **102** (official) and **101** (legacy); grid via `GetTodayCanceledReceiptsAsync`. Not in ERPCOM3.

**Story 12.5 — Cancelar NFES (batch)** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/sales/nfe/cancel_nfes.aspx`  
**EUROERP:** `NfesCancelarLote.razor` at `/vendas/nfe/cancelar-lote` — Simpliss cancel via `INfesCancellationService`; today's cancels grid. Manual admin path: `/diretoria/admin/nfes-cancel-manual`.

**Story 12.6 — Carta de correção** — **STATUS: Done**  
Legacy: `Eurobus4/principal/sales/nfe/cc_nfe.aspx`  
**EUROERP:** `NfeCartaCorrecao.razor` at `/vendas/nfe/carta-correcao` + print `/vendas/nfe/carta-correcao/imprimir?RID=`; SEFAZ 110110 via `SendCceAsync` (sales only). Email deferred (same as ERPCOM3).

**Story 12.7 — Relatório NFe** — **STATUS: Done**  
Legacy: `Eurobus4/principal/sales/receipt_report.aspx`  
**EUROERP:** `NfeRelatorio.razor` at `/vendas/nfe/relatorio` — period filter; saídas + entradas/outras; PDF/XML via `/NFE_FILES/`.

**Story 12.8 — Download ZIP + Status serviço** — **STATUS: Done**  
Legacy: `Eurobus4/principal/sales/nfe/nfe_download.aspx`, `status_servico.aspx`  
**EUROERP:** `NfeDownload.razor` at `/vendas/nfe/download` (zip → `/NFE_download/`); `NfeStatusServico.razor` at `/vendas/nfe/status-servico` (SEFAZ consStatServ).

---

## EPIC 13 — Accounts payable (Contas a Pagar)

**Story 13.1 — Consultar AP** — **STATUS: Done**  
Legacy: `Eurobus4/principal/finance/btp2/search.aspx`  
EUROERP: `/financeiro/contas-a-pagar/consultar` — port from ERPCOM3; supplier autocomplete via `ISupplierService`; groups/payment methods via `ISupplierReferenceService`; no activity codes.

**Story 13.2 — Criar AP** — **STATUS: Done**  
Legacy: `Eurobus4/principal/finance/btp/new.aspx`  
EUROERP: `/financeiro/contas-a-pagar/criar` — creates detail rows with STATUS=`U` (pending approval, Eurobus).

**Story 13.3 — AP actions (search list)** — **STATUS: Done**  
Pattern: ERPCOM3 Epic 13 Story 13.3  
EUROERP: alterar-vencimento / alterar-valor / alterar-memo / alterar-pedido / alterar-metodo-pagamento. Due-date, amount, and memo updates write `FINANCE_BTP_CHG_HST` (Eurobus `insertBtpHistory`).

**Story 13.4 — Baixa AP** — **STATUS: Done**  
Legacy: `Eurobus4/principal/finance/btp2/down.aspx`  
EUROERP: `/financeiro/contas-a-pagar/baixa` + `/financeiro/contas-a-pagar/pagamentos`.

**Story 13.5 — AP reports (semanal, pagto por grupo)** — **STATUS: Done**  
Legacy: `Eurobus4/principal/finance/btp2/reports/*`  
EUROERP: `/financeiro/contas-a-pagar/relatorios/semanal` (alias `/diario`); `/relatorios/pagto-grupo` + `/detalhe`.

**Story 13.6 — Pendentes (approve)** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/principal/finance/btp2/approve_ajax.aspx`  
EUROERP: `/financeiro/contas-a-pagar/pendentes` — list STATUS=`U`, approve → `A` via `IBillsToPayApproveService`.

---

## EPIC 14 — Accounts receivable (Contas a Receber) — **DONE**

All stories complete (14.1–14.3). Port from ERPCOM3 Contas a Receber + Eurobus BTR (`FINANCE_BTR_CHG_HST`, NFES, TYPE=`M`). Cheque split not implemented.

**Story 14.1 — Consultar AR** — **STATUS: Done**  
Legacy: `Eurobus4/principal/finance/btr/search.aspx`  
EUROERP: `/financeiro/contas-a-receber/consultar` — client autocomplete, due-date range, payment method, status, usuário/vendedor, order id, ABC view; NFE/NFES columns.

**Story 14.2 — AR actions** — **STATUS: Done**  
Change due date/amount (writes `FINANCE_BTR_CHG_HST`), receive/baixa, list/cancel receives, change payment method (blocked if any baixa).  
Routes: `alterar-vencimento`, `alterar-valor`, `receber`, `recebimentos`, `alterar-metodo-pagamento`.

**Story 14.3 — Relatório de baixas** — **STATUS: Done**  
Legacy: `Eurobus4/principal/finance/btr/reports/receive_request.aspx`  
EUROERP: `/financeiro/contas-a-receber/relatorios/baixas` — period ≤31 days; optional payment-method filter.

---

## EPIC 15 — Revenue & finance reports — **DONE**

All stories complete (15.1–15.5). Port from ERPCOM3 Epic 16 + Eurobus-only fornecedor monthly and cash flow.

**Story 15.1 — Faturamento diário** — **STATUS: Done**  
Legacy: `Eurobus4/principal/finance/invoicing/search.aspx`  
EUROERP: `/financeiro/faturamento/diario` — SENT_DATE range; payment method / usuário / vendedor; NF + NFES; `?date=` / `?supId=` drill-downs.

**Story 15.2 — Faturamento mensal (geral + fornecedor)** — **STATUS: Done**  
EUROERP: `/financeiro/faturamento/mensal/geral` + `/financeiro/faturamento/mensal/fornecedor` (line totals via `ORDER_DETAILS` + `PRODUCT_SUPPLIER_LINK`).

**Story 15.3 — Faturamento anual** — **STATUS: Done**  
EUROERP: `/financeiro/faturamento/anual` — month range; link to mensal.

**Story 15.4 — Fluxo de caixa** — **STATUS: Done**  
Legacy: `cashflow_day.aspx`  
EUROERP: `/financeiro/fluxo-caixa` — composes AR + AP due-date searches; running balance from caixa inicial.

**Story 15.5 — Liberar cliente (inadimplência)** — **STATUS: Done**  
Legacy: `delinq.aspx`  
EUROERP: `/financeiro/liberar-cliente` — `ALLOW_DELINQ` / `IGNORE_DELINQ`.

---

## EPIC 16 — Reference data (Referência) — **DONE**

All stories complete (16.1–16.3).

**Story 16.1 — Grupo de produtos** — **STATUS: Done**  
Legacy: `Eurobus4/operation/reference/product_group.aspx`  
EUROERP: `/referencia/grupos-produtos` — list/create/edit/delete; `IGNORE_ORDER_DISC`; class from `PRODUCT_CLASS`.

**Story 16.2 — Classificação fiscal** — **STATUS: Done**  
Legacy: `Eurobus4/operation/reference/fiscal_class.aspx`  
EUROERP: `/referencia/classe-fiscal` — user-supplied PK; VALUE/IPI/**ICMSST**/NAME (Eurobus ICMSST kept).

**Story 16.3 — Conversão de moedas** — **STATUS: Done**  
Legacy: `Eurobus4/operation/currency/currency.aspx`  
EUROERP: `/referencia/moedas` — edit `CURRENCY_CONVERSION` rates only (decimal 4,3).

---

## EPIC 17 — Admin & security (Diretoria)

**Story 17.1 — User management** — **STATUS: Done**  
Legacy: `Eurobus4/manager/members/manageMembers.aspx`, `delMember.aspx`  
EUROERP: `/diretoria/usuarios/novo`, `/diretoria/usuarios/remover` — create (PasswordFormat=1, auto MARKET_USER market 1); delete cleans related rows; cannot delete self.

**Story 17.2 — Roles** — **STATUS: Done**  
EUROERP: `/diretoria/usuarios/funcoes` — CRUD `aspnet_Roles` + user↔role assignment (`aspnet_UsersInRoles`). Protected: Admin, Master. (Live tables — not aspnet_roles2.)

**Story 17.3 — Activities & role-activity mapping** — **STATUS: Done**  
Legacy: `manager/activity/*` — `SEC_ACTIVITY`, `ACTIVITY_ROLE`  
EUROERP: `/diretoria/atividades/gerenciar`, `/diretoria/atividades/associar`; login loads ActivityCodes claim; `RequireActivity` + `/acesso-negado`.

**Story 17.4 — Mercados (user markets)** — **STATUS: Done** *(Eurobus)*  
Legacy: `Eurobus4/manager/members/userMarket.aspx`  
EUROERP: `/diretoria/usuarios/mercados` — dual-list `MARKET` / `MARKET_USER`.

**Story 17.5 — Alíq. ICMS** — **STATUS: Done**  
Legacy: `Eurobus4/manager/creditIcms.aspx`  
EUROERP: `/diretoria/icms` — edits `SYS_CONTROL` `ICMS_ALIQ`; NFe emit + DANFE read from SYS_CONTROL (appsettings fallback).

**Story 17.6 — Master functions** — **STATUS: Done**  
Legacy: `Eurobus4/manager/master/master.aspx`, `sql.aspx`  
EUROERP: `/diretoria/master/sql` — gated by activity `SQL`; SELECT capped; non-SELECT logged.

---

## EPIC 18 — Cadastro & dashboard

**Story 18.1 — Alterar senha / e-mail** — **STATUS: Done**  
Legacy: `Eurobus4/security/changePassword.aspx`, `changeEmail.aspx`  
EUROERP: `/cadastro/alterar-senha`, `/cadastro/alterar-email` — `IAccountService` valida a senha atual como o login (PasswordFormat 0/1), regrava com salt novo e PasswordFormat 1 (7–10 caracteres, letras + números) e atualiza `LastPasswordChangedDate`; e-mail exige `.*@.*\..*` e é único por ApplicationId (`Email` + `LoweredEmail`).

**Story 18.2 — Dashboard + widgets** — **STATUS: Done**  
Pattern: ERPCOM3 Epic 19 (daily sales, NFes, shortcuts)  
EUROERP: Home (`/`) mostra o widget fixo de certificado NFe (validade piscante a ≤45 dias) mais os widgets escolhidos pelo usuário: faturamento diário e acumulado (Chart.js, cache 2h sobre `IRevenueReportMonthlyService`), últimas 7 NF-e (PDF/XML, "Ver todas" → `/vendas/nfe/imprimir`) e atalhos de menu. Preferências em `/cadastro/widgets` (`USER_WIDGET`, `USER_SHORTCUT` — scripts em `docs/sql/`); as leituras toleram tabelas ausentes.

---

## EPIC 19 — External APIs

**Story 19.1 — REST APIs (legacy WS replacement)** — **STATUS: Pending**  
Legacy SOAP: `Eurobus4/WS/*.asmx`, `App_Code/lion/ws/*`  
Services: Client, Product, Supplier, Sales, Ordering, Returning — implement as needed for Eurobus integrations  
Auth: `X-Api-Token` pattern from ERPCOM3

---

## EPIC 20 — Mobile layout — **DONE**

**Story 20.1 — Responsive shell** — **STATUS: Done**  
Collapsible menu on small screens; do not change desktop layout.  
Pattern: ERPCOM3 Epic 21  
**EUROERP:** hamburger + overlay drawer ≤768px; top sections in drawer; desktop unchanged. Plan: `STORY_PLAN/Epic20-Story1-Responsive-Shell.md`

---

## Summary

| Phase | Epics | Stories (approx.) |
|-------|-------|-------------------|
| Foundation | 1 | 3 |
| Master data | 2–5 | 18 |
| Stock & lists | 6–7 | 6 |
| OS / Vendas | 8–9, 11 | 11 |
| NFe | 12 | 9 |
| Finance | 13–15 | 14 |
| Platform | 16–19 | 12 |
| Optional (to be implemented) | 10 | 3 |
| Optional (mobile) | 20 | 1 (Done) |
| **Total** | **20** | **~77** |

See also: [`MIGRATION_PLAN.md`](MIGRATION_PLAN.md)
