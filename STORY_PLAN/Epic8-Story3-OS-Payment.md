# Epic 8 – Story 8.3: OS Payment (BTR)

**Status:** Done  
**Brief:** OS payment screen — client payment methods, BTR terms, finalize OS.

---

## Objective

After the OS editor (Story 8.2), the user defines payment and finalizes the order (`STATUS = F`). Creates `FINANCE_BTR` + `FINANCE_BTR_DETAIL`, links `ORDER.BTR_ID`, applies client credit.

Legacy: `Eurobus4/principal/sales/sale_btr.aspx` → `BillsToReceiveControl.ascx`  
Pattern: ERPCOM3 Epic 7 Story 7.3 (adapted to Eurobus)

---

## Eurobus differences vs Aquanimal

| Item | Eurobus | Aquanimal (ERPCOM3) |
|------|---------|---------------------|
| Client payment slots | 5 (`PAYMENT_METHOD_ID` … `ID5`) | 3 |
| “Outros” method | Id **6** — no BTR | “Simples remessa” (permission) |
| Multi-term methods | 2, 3, 5, 7 (by `MAX_TERMS`) | Mainly id 2 (cartão) |
| Payment sub-methods | `PAYMENT_SUB_METHOD` dropdown | Not used |
| Card first due date | Method 7: +30 days | Monthly from today |
| Cheque (id 1) | Skip | Skip |

---

## Routes

| Page | Route |
|------|-------|
| Payment | `/vendas/os/{OrderId}/pagamento` |
| Success | `/vendas/os/{OrderId}/finalizado` |

---

## Acceptance criteria

1. Summary: credit, discount, other expenses, freight, total to pay, client, prazo médio.
2. Payment methods from client (5 slots) + **OUTROS** (6), filtered by `MIN_AMOUNT`.
3. Sub-methods when configured for selected method.
4. Single term for Money (4); parcelas for multi-term methods; OUTROS finishes without BTR.
5. **Finalizar:** validate sum = total; create BTR (except OUTROS); set `STATUS = F`; apply credit.
6. Reopened OS (`R`): replace existing BTR when finishing with a normal method.
7. Voltar → OS editor; Sair → Nova OS.

---

## Files

- `OsPagamento.razor` — payment UI
- `OsFinalizado.razor` — success page
- `OrderService.cs` — Eurobus payment methods, OUTROS, sub-method, BTR detail load
- `BtrDetailDto.cs` — `PaymentSubMethodId`
