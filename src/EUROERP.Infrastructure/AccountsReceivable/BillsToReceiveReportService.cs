using System.Data;
using Dapper;
using EUROERP.Application.AccountsReceivable;

namespace EUROERP.Infrastructure.AccountsReceivable;

public class BillsToReceiveReportService : IBillsToReceiveReportService
{
    private readonly IDbConnection _connection;

    public BillsToReceiveReportService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<ReceiveReportResultDto> GetReceiveReportAsync(ReceiveReportCriteriaDto criteria, CancellationToken cancellationToken = default)
    {
        EnsureOpen();

        // Eurobus FinancialReceiveController.getReceiveReport
        const string select = @"
SELECT o.PKId AS OrderId, fr.TERM_NO AS TermNo, btr.TERMS AS Terms,
    CONVERT(VARCHAR, btrd.DUE_DATE, 103) AS DueDate,
    CONVERT(VARCHAR, fr.SYS_CREATION_DATE, 103) AS ReceiveDate,
    fr.USER_ID AS UserId,
    ROUND(btrd.AMOUNT, 2) AS OriginalAmount,
    ROUND(fr.AMOUNT, 2) AS Amount,
    fr.MEMO AS Memo,
    ISNULL(fr.COMMISSION_ID, 0) AS ComId,
    fr.RETURN_ID AS ReturnId,
    pm.NAME AS PaymentMethodName,
    c.FANTASY_NAME AS ClientFantasyName
FROM [FINANCE_RECEIVE] fr
JOIN [FINANCE_BTR] btr ON btr.PKId = fr.FINANCE_BTR_ID
JOIN [FINANCE_BTR_DETAIL] btrd ON btr.PKId = btrd.FINANCE_BTR_ID AND btrd.TERM_NO = fr.TERM_NO
JOIN [ORDER] o ON o.BTR_ID = fr.FINANCE_BTR_ID
JOIN [CLIENT] c ON c.PKId = o.CLIENT_ID
JOIN [PAYMENT_METHOD] pm ON pm.PKId = btrd.PAYMENT_METHOD_ID
WHERE CAST(fr.SYS_CREATION_DATE AS DATE) >= @FIRST_DATE
  AND CAST(fr.SYS_CREATION_DATE AS DATE) <= @LAST_DATE";

        var param = new Dictionary<string, object?>
        {
            ["FIRST_DATE"] = criteria.FirstDate.Date,
            ["LAST_DATE"] = criteria.LastDate.Date
        };

        var sql = select;
        if (criteria.PaymentMethodId > 0)
        {
            sql += " AND btrd.PAYMENT_METHOD_ID = @PYM_ID";
            param["PYM_ID"] = criteria.PaymentMethodId;
        }

        sql += " ORDER BY fr.SYS_CREATION_DATE DESC";

        var rows = (await _connection.QueryAsync<ReceiveReportRowDto>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken))).ToList();

        return new ReceiveReportResultDto
        {
            Rows = rows,
            FirstDate = criteria.FirstDate,
            LastDate = criteria.LastDate
        };
    }

    private void EnsureOpen()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }
}
