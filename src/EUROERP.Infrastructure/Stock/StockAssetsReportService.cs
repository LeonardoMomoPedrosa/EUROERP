using System.Data;
using Dapper;
using EUROERP.Application.Stock;

namespace EUROERP.Infrastructure.Stock;

public class StockAssetsReportService : IStockAssetsReportService
{
    private readonly IDbConnection _connection;

    public StockAssetsReportService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<StockAssetsResultDto> SearchAsync(StockAssetsFilterDto filter, CancellationToken cancellationToken = default)
    {
        var safeFilter = filter ?? new StockAssetsFilterDto();

        var sql = BuildSql(safeFilter, out var canDrillDown);
        var rows = (await _connection.QueryAsync<StockAssetsRowDto>(
            new CommandDefinition(sql, new
            {
                safeFilter.ClassId,
                safeFilter.GroupId
            }, cancellationToken: cancellationToken))).ToList();

        var result = new StockAssetsResultDto
        {
            Rows = rows,
            CurrentLevel = safeFilter.DrillLevel,
            Breadcrumb = BuildBreadcrumb(safeFilter, rows),
            TotalStock = rows.Sum(x => (decimal)x.Stock),
            TotalCost = rows.Sum(x => x.TotalCost),
            TotalPrice = rows.Sum(x => x.TotalPrice)
        };

        foreach (var row in rows)
            row.CanDrillDown = canDrillDown;

        return result;
    }

    private static string BuildSql(StockAssetsFilterDto filter, out bool canDrillDown)
    {
        var isAnimalView = filter.ViewMode == StockAssetsViewMode.Animals;

        canDrillDown = filter.DrillLevel == 0;

        var cte = @"
WITH base_data AS (
    SELECT
        p.PKId AS ProductId,
        p.NAME AS ProductName,
        p.EXTERNAL_PKID AS ExternalPkid,
        p.STOCK AS Stock,
        p.COST_FINAL AS CostUnit,
        ISNULL(mp.PRICE, p.COST_FINAL) AS PriceUnit,
        pc.PKId AS ClassId,
        pc.NAME AS ClassName,
        pg.PKId AS GroupId,
        pg.NAME AS GroupName,
        p.SIZE_ID AS SizeId
    FROM PRODUCT p
    JOIN PRODUCT_GROUP pg ON pg.PKId = p.GROUP_ID
    JOIN PRODUCT_CLASS pc ON pc.PKId = pg.PRODUCT_CLASS_ID
    LEFT JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId AND mp.MARKET_ID = 1
    WHERE p.ACTIVE = 'Y'
      AND (@ClassId IS NULL OR pc.PKId = @ClassId)
      AND (@GroupId IS NULL OR pg.PKId = @GroupId)";

        // Eurobus: only product classes (not services)
        cte += @"
      AND pc.PROD_SRV_IND = 'P'";

        cte += @"
),
sales_by_product AS (
    SELECT
        od.PRODUCT_ID AS ProductId,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -30, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q1,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -60, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -30, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q2,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -90, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -60, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q3,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -120, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -90, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q4,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -150, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -120, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q5,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -180, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -150, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q6,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -210, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -180, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q7,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -240, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -210, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q8,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -270, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -240, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q9,
        SUM(CASE WHEN o.SYS_CREATION_DATE >= DATEADD(day, -300, GETDATE()) AND o.SYS_CREATION_DATE < DATEADD(day, -270, GETDATE()) THEN od.QUANTITY ELSE 0 END) AS Q10
    FROM ORDER_DETAILS od
    JOIN [ORDER] o ON o.PKId = od.ORDER_ID
    GROUP BY od.PRODUCT_ID
)";

        if (filter.DrillLevel == 0)
        {
            return cte + @"
SELECT
    CAST(ClassId AS varchar(20)) + ':' + CAST(GroupId AS varchar(20)) AS [Key],
    GroupName AS [Description],
    ClassName,
    GroupName,
    CAST(SUM(Stock) AS int) AS Stock,
    CAST(CASE WHEN SUM(Stock) = 0 THEN 0 ELSE SUM(CostUnit * Stock) / SUM(Stock) END AS decimal(18, 2)) AS CostUnit,
    CAST(CASE WHEN SUM(Stock) = 0 THEN 0 ELSE SUM(PriceUnit * Stock) / SUM(Stock) END AS decimal(18, 2)) AS PriceUnit,
    CAST(SUM(CostUnit * Stock) AS decimal(18, 2)) AS TotalCost,
    CAST(SUM(PriceUnit * Stock) AS decimal(18, 2)) AS TotalPrice,
    CAST(CASE WHEN SUM(CostUnit * Stock) = 0 THEN 0 ELSE ((SUM(PriceUnit * Stock) - SUM(CostUnit * Stock)) / SUM(CostUnit * Stock)) * 100 END AS decimal(18, 2)) AS MarginPercent,
    CAST(0 AS bit) AS CanDrillDown,
    CAST(ClassId AS tinyint) AS ClassId,
    CAST(GroupId AS tinyint) AS GroupId,
    CAST(0 AS int) AS Q1,
    CAST(0 AS int) AS Q2,
    CAST(0 AS int) AS Q3,
    CAST(0 AS int) AS Q4,
    CAST(0 AS int) AS Q5,
    CAST(0 AS int) AS Q6,
    CAST(0 AS int) AS Q7,
    CAST(0 AS int) AS Q8,
    CAST(0 AS int) AS Q9,
    CAST(0 AS int) AS Q10,
    CAST(0 AS decimal(18,2)) AS Qm
FROM base_data
GROUP BY ClassId, ClassName, GroupId, GroupName
ORDER BY ClassName, GroupName";
        }

        return cte + @"
SELECT
    CAST(base_data.ProductId AS varchar(20)) AS [Key],
    CASE
        WHEN ExternalPkid IS NULL OR LTRIM(RTRIM(ExternalPkid)) = '' THEN ProductName
        ELSE ProductName + ' (' + ExternalPkid + ')'
    END AS [Description],
    ClassName,
    GroupName,
    Stock,
    CAST(CostUnit AS decimal(18, 2)) AS CostUnit,
    CAST(PriceUnit AS decimal(18, 2)) AS PriceUnit,
    CAST(CostUnit * Stock AS decimal(18, 2)) AS TotalCost,
    CAST(PriceUnit * Stock AS decimal(18, 2)) AS TotalPrice,
    CAST(CASE WHEN CostUnit = 0 THEN 0 ELSE ((PriceUnit - CostUnit) / CostUnit) * 100 END AS decimal(18, 2)) AS MarginPercent,
    CAST(0 AS bit) AS CanDrillDown,
    CAST(ClassId AS tinyint) AS ClassId,
    CAST(GroupId AS tinyint) AS GroupId,
    ISNULL(sb.Q1, 0) AS Q1,
    ISNULL(sb.Q2, 0) AS Q2,
    ISNULL(sb.Q3, 0) AS Q3,
    ISNULL(sb.Q4, 0) AS Q4,
    ISNULL(sb.Q5, 0) AS Q5,
    ISNULL(sb.Q6, 0) AS Q6,
    ISNULL(sb.Q7, 0) AS Q7,
    ISNULL(sb.Q8, 0) AS Q8,
    ISNULL(sb.Q9, 0) AS Q9,
    ISNULL(sb.Q10, 0) AS Q10,
    CAST((ISNULL(sb.Q1, 0) + ISNULL(sb.Q2, 0) + ISNULL(sb.Q3, 0) + ISNULL(sb.Q4, 0) + ISNULL(sb.Q5, 0) + ISNULL(sb.Q6, 0) + ISNULL(sb.Q7, 0) + ISNULL(sb.Q8, 0) + ISNULL(sb.Q9, 0) + ISNULL(sb.Q10, 0)) / 10.0 AS decimal(18,2)) AS Qm
FROM base_data
LEFT JOIN sales_by_product sb ON sb.ProductId = base_data.ProductId
ORDER BY ClassName, GroupName, ProductName";
    }

    private static string? BuildBreadcrumb(StockAssetsFilterDto filter, IReadOnlyList<StockAssetsRowDto> rows)
    {
        const string root = "Grupo";

        if (filter.DrillLevel == 1)
        {
            var groupName = rows.Select(r => r.GroupName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (!string.IsNullOrWhiteSpace(groupName))
                return $"{root} > {groupName}";

            return $"{root} > {filter.GroupId}";
        }

        return root;
    }
}
