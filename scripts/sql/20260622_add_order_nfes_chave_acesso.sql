-- Story 12.1-NFES: NFS-e (serviços) must not use ORDER.NFE_KEY / NFE_RECEIPT (product NFe / SEFAZ).
-- Legacy (Eurobus4): updateOrderNfeServiceReceipt sets RPS_NO, NFES_NO, NFES_CHECK_CODE only.
-- Simpliss layout nacional: store full chave in NFES_CHAVE_ACESSO.

IF COL_LENGTH('[ORDER]', 'NFES_CHAVE_ACESSO') IS NULL
BEGIN
    ALTER TABLE [ORDER] ADD NFES_CHAVE_ACESSO varchar(100) NULL;
END
GO
