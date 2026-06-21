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

## EPIC 8 — Service orders / OS (Vendas)

**Story 8.1 — Nova OS — first screen** — **STATUS: Done** *(Eurobus)*  
Client + sales agent selection; recent OS list.  
Legacy: `Eurobus4/principal/sales/new_sale_create_order_number.aspx`

**Story 8.2 — OS editor (open status)** — **STATUS: Done** *(Eurobus)*  
Cart, discounts, F2 product search, credit/shipment amounts.  
Legacy: `Eurobus4/principal/sales/new_sale.aspx` and related components

**Story 8.3 — OS payment (BTR)** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/sale_btr.aspx`  
Tables: `FINANCE_BTR`, `FINANCE_BTR_DETAIL`, `FINANCE_RECEIVE`

**Story 8.4 — OS activities (read-only + actions)** — **STATUS: Pending**  
Reopen, print, packing slip, labels.  
Legacy: order detail/print pages under `Eurobus4/principal/sales/`

**Story 8.5 — Consultar OS** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/search_sale.aspx` and related search pages

**Story 8.6 — Desconto por produto na OS** — **STATUS: Pending** *(if required)*  
Legacy: `Eurobus4/principal/sales/discountProduct.aspx`

---

## EPIC 9 — Sales reports (Relatórios Vendas)

**Story 9.1 — ABC** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/invoicing/search_by_group.aspx` (+ result pages)

**Story 9.2 — Minhas vendas** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/mySalesInput.aspx`

**Story 9.3 — Cliente / Vendedor** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/rank/client_per_saler.aspx`

---

## EPIC 10 — Customer service (Atendimento) *(Eurobus)*

**Story 10.1 — Criar atendimento** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/call/call_create.aspx`  
Tables: `CALL`, `CALL_STEP`, `ZONE`

**Story 10.2 — Listar atendimentos** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/call/call_search.aspx`

**Story 10.3 — Relatório atendimentos** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/call/call_report.aspx`

---

## EPIC 11 — OS billing flow

**Story 11.1 — OS não faturada** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/last_orders.aspx`

**Story 11.2 — Faturar OS** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/send/send_order.aspx`

---

## EPIC 12 — NFe

**Interim (Eurobus bridge):** OS lifecycle stays in **legacy Eurobus** until Epics 8–11 are done. EUROERP may ship **NFES only** first — same DB (`LionEBDev`), user enters order # closed in legacy (`F`/`E`), emits NFS-e via Prefeitura SP. Product NFe (SEFAZ) remains in legacy until full Story 12.1.

**Story 12.1 — Enviar NFe (individual)** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/nfe/receipt.aspx` *(Eurobus flow — not receiptSync)*  
SEFAZ, certificate, validation, PDF — pattern ERPCOM3 Epic 10 Story 10.1  
*Deferred while legacy handles product NFe; see 12.1-NFES for interim scope.*

**Story 12.1-NFES — Enviar NFES (NFS-e serviços)** — **STATUS: Done** *(Eurobus bridge)*  
Legacy: `Eurobus4/principal/sales/nfe/receipt.aspx` → `printNFEServices`.  
**EUROERP:** provider por município — **Simpliss / layout nacional DPS** para Santana de Parnaíba (`Nfes:Provider=Simpliss`, IBGE `3547304`); **Prefeitura SP RPS** opcional (`PrefeituraSp`).  
Not in ERPCOM3/Aquanimal. Updates `ORDER.NFES_NO`, `NFES_CHECK_CODE`, `RPS_NO`.  
Requires order **F** or **E** with service total &gt; 0 (legacy closes OS / BTR). No EUROERP Epic 8.3 dependency if OS is finished in legacy.

**Story 12.2 — NFe Outras / Outras (novo)** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/receiptin_nfe/dataInput.aspx`, `dataInput2.aspx`

**Story 12.3 — Imprimir / listar NFe** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/nfe/status.aspx`

**Story 12.4 — Cancelar NFe** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/nfe/cancel.aspx`

**Story 12.5 — Cancelar NFES (batch)** — **STATUS: Pending** *(Eurobus)*  
Legacy: `Eurobus4/principal/sales/nfe/cancel_nfes.aspx`

**Story 12.6 — Carta de correção** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/nfe/cc_nfe.aspx`

**Story 12.7 — Relatório NFe** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/receipt_report.aspx`

**Story 12.8 — Download ZIP + Status serviço** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/sales/nfe/nfe_download.aspx`, `status_servico.aspx`

---

## EPIC 13 — Accounts payable (Contas a Pagar)

**Story 13.1 — Consultar AP** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/btp2/search.aspx`

**Story 13.2 — Criar AP** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/btp/new.aspx`

**Story 13.3 — AP actions (search list)** — **STATUS: Pending**  
Pattern: ERPCOM3 Epic 13 Story 13.3

**Story 13.4 — Baixa AP** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/btp2/down.aspx`

**Story 13.5 — AP reports (semanal, pagto por grupo)** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/btp2/reports/*`

**Story 13.6 — Pendentes (approve)** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/btp2/approve_ajax.aspx`

---

## EPIC 14 — Accounts receivable (Contas a Receber)

**Story 14.1 — Consultar AR** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/btr/search.aspx`

**Story 14.2 — AR actions** — **STATUS: Pending**  
Change due date/amount, receive, change payment method.

**Story 14.3 — Relatório de baixas** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/btr/reports/receive_request.aspx`

---

## EPIC 15 — Revenue & finance reports

**Story 15.1 — Faturamento diário** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/invoicing/search.aspx`

**Story 15.2 — Faturamento mensal (geral + fornecedor)** — **STATUS: Pending**  
Legacy: `monthInvoicingSearch.aspx`, `monthInvoicingSupplierSearch.aspx`

**Story 15.3 — Faturamento anual** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/invoicing/yearInvoicingSearch.aspx`

**Story 15.4 — Fluxo de caixa** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/cashflow/cashflow_day.aspx`, `cashflow_day_results.aspx`

**Story 15.5 — Liberar cliente (inadimplência)** — **STATUS: Pending**  
Legacy: `Eurobus4/principal/finance/delinq.aspx`

---

## EPIC 16 — Reference data (Referência)

**Story 16.1 — Grupo de produtos** — **STATUS: Pending**  
Legacy: `Eurobus4/operation/reference/product_group.aspx`

**Story 16.2 — Classificação fiscal** — **STATUS: Pending**  
Legacy: `Eurobus4/operation/reference/fiscal_class.aspx`

**Story 16.3 — Conversão de moedas** — **STATUS: Pending**  
Legacy: `Eurobus4/operation/currency/currency.aspx`

---

## EPIC 17 — Admin & security (Diretoria)

**Story 17.1 — User management** — **STATUS: Pending**  
Legacy: `Eurobus4/manager/members/manageMembers.aspx`, `delMember.aspx`

**Story 17.2 — Roles (aspnet_roles2)** — **STATUS: Pending**  
Pattern: ERPCOM3 Epic 18 Stories 18.2–18.3

**Story 17.3 — Activities & role-activity mapping** — **STATUS: Pending**  
Legacy: `manager/activity/*` — use `SEC_ACTIVITY`, `ACTIVITY_ROLE`  
Pattern: ERPCOM3 Epic 18 Stories 18.4–18.7

**Story 17.4 — Mercados (user markets)** — **STATUS: Pending** *(Eurobus)*  
Legacy: `Eurobus4/manager/members/userMarket.aspx`  
Tables: `MARKET`, `MARKET_USER`, `MARKET_PRODUCT`

**Story 17.5 — Alíq. ICMS** — **STATUS: Pending**  
Legacy: `Eurobus4/manager/creditIcms.aspx`

**Story 17.6 — Master functions** — **STATUS: Pending**  
Legacy: `Eurobus4/manager/master/master.aspx`, `sql.aspx` (Master role only)

---

## EPIC 18 — Cadastro & dashboard

**Story 18.1 — Alterar senha / e-mail** — **STATUS: Pending**  
Legacy: `Eurobus4/security/changePassword.aspx`, `changeEmail.aspx`

**Story 18.2 — Dashboard + widgets** — **STATUS: Pending**  
Pattern: ERPCOM3 Epic 19 (daily sales, NFes, shortcuts) — adapt to Eurobus needs

---

## EPIC 19 — External APIs

**Story 19.1 — REST APIs (legacy WS replacement)** — **STATUS: Pending**  
Legacy SOAP: `Eurobus4/WS/*.asmx`, `App_Code/lion/ws/*`  
Services: Client, Product, Supplier, Sales, Ordering, Returning — implement as needed for Eurobus integrations  
Auth: `X-Api-Token` pattern from ERPCOM3

---

## EPIC 20 — Mobile layout *(optional, later)*

**Story 20.1 — Responsive shell** — **STATUS: Pending**  
Collapsible menu on small screens; do not change desktop layout.  
Pattern: ERPCOM3 Epic 21

---

## Summary

| Phase | Epics | Stories (approx.) |
|-------|-------|-------------------|
| Foundation | 1 | 3 |
| Master data | 2–5 | 18 |
| Stock & lists | 6–7 | 6 |
| OS / Vendas | 8–11 | 14 |
| NFe | 12 | 8 |
| Finance | 13–15 | 14 |
| Platform | 16–19 | 12 |
| Optional | 20 | 1 |
| **Total** | **20** | **~76** |

See also: [`MIGRATION_PLAN.md`](MIGRATION_PLAN.md)
