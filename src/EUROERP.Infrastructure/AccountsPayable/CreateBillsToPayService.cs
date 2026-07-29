using System.Data;
using Dapper;
using EUROERP.Application.AccountsPayable;

namespace EUROERP.Infrastructure.AccountsPayable;

public class CreateBillsToPayService : ICreateBillsToPayService
{
    private readonly IDbConnection _connection;

    public CreateBillsToPayService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<SupplierPaytermDto?> GetSupplierPaytermAndPaymentMethodAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT PAYTERM AS Payterm, PAYMENT_METHOD_ID AS PaymentMethodId
            FROM SUPPLIER WHERE PKId = @SupplierId";
        return await _connection.QueryFirstOrDefaultAsync<SupplierPaytermDto>(
            new CommandDefinition(sql, new { SupplierId = supplierId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(CreateBillsToPayDto dto, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        decimal? conversion = null;
        if (dto.CurrencyId != 1)
        {
            const string convSql = @"
                SELECT CONVERSION FROM CURRENCY_CONVERSION
                WHERE SOURCE_CURRENCY_ID = 1 AND TARGET_CURRENCY_ID = @CurrencyId";
            conversion = await _connection.ExecuteScalarAsync<decimal?>(
                new CommandDefinition(convSql, new { dto.CurrencyId }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }

        using var tx = _connection.BeginTransaction();
        try
        {
            const string insertBill = @"
                INSERT INTO FINANCE_BILLS_TO_PAY
                (SUPPLIER_ID, SYS_CREATION_DATE, APPLICATION_ID, USER_ID, PAYMENT_METHOD_ID, TERMS, BILL_TYPE,
                 STOCK_IN_ID, CURRENCY_ID, CONVERSION, ORDER_DATE, MANUAL_BILL_ID, PURCH_ID)
                VALUES
                (@SupplierId, GETDATE(), @ApplicationId, @UserId, @PaymentMethodId, @Terms, 'M',
                 NULL, @CurrencyId, @Conversion, @OrderDate, NULL, NULL);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var billParam = new
            {
                dto.SupplierId,
                ApplicationId = applicationId.Length > 8 ? applicationId[..8] : applicationId,
                UserId = userId.Length > 20 ? userId[..20] : userId,
                dto.PaymentMethodId,
                dto.Terms,
                dto.CurrencyId,
                Conversion = conversion ?? (object)DBNull.Value,
                OrderDate = dto.OrderDate ?? (object)DBNull.Value
            };
            var newId = await _connection.ExecuteScalarAsync<int>(
                new CommandDefinition(insertBill, billParam, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);

            const string insertDetail = @"
                INSERT INTO FINANCE_BILLS_TO_PAY_DETAIL (FINANCE_BILL_ID, TERM_NO, DUE_DATE, AMOUNT, STATUS, MEMO)
                VALUES (@FinanceBillId, @TermNo, @DueDate, @Amount, 'U', @Memo)";

            const int memoMaxLen = 200;
            foreach (var term in dto.Details.OrderBy(t => t.TermNo))
            {
                var memo = term.Memo ?? "";
                if (memo.Length > memoMaxLen) memo = memo[..memoMaxLen];
                await _connection.ExecuteAsync(
                    new CommandDefinition(insertDetail, new
                    {
                        FinanceBillId = newId,
                        term.TermNo,
                        term.DueDate,
                        term.Amount,
                        Memo = memo
                    }, transaction: tx, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            tx.Commit();
            return newId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
