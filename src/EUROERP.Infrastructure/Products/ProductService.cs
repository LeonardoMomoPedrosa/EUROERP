using System.Data;
using Dapper;
using EUROERP.Application.Products;

namespace EUROERP.Infrastructure.Products;

public class ProductService : IProductService
{
    private readonly IDbConnection _connection;

    public ProductService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<ProductSummaryDto>> GetListAsync(ProductFilterDto filter, CancellationToken cancellationToken = default)
    {
        var marketId = filter.MarketId == 0 ? (byte)1 : filter.MarketId;
        var hasParam = filter.ClassId > 0 || filter.GroupId > 0 ||
                       (!string.IsNullOrWhiteSpace(filter.Name) && filter.Name.Length >= 2) ||
                       (!string.IsNullOrWhiteSpace(filter.SciName) && filter.SciName.Length >= 2) ||
                       filter.Code > 0 || filter.SupplierId > 0;
        if (!hasParam)
            return new List<ProductSummaryDto>();

        var sql = @"
            SELECT DISTINCT
                p.PKId,
                p.EXTERNAL_PKID AS ExternalPkId,
                p.NAME AS Name,
                p.SCI_NAME AS SciName,
                az.NAME AS Size,
                ROUND(mp.PRICE * ISNULL(cv.CONVERSION, 1), 2) AS Price,
                curMkt.SYMBOL AS CurrencySymbol,
                p.STOCK AS Stock,
                p.STOCK_MIN AS StockMin,
                p.STOCK_LAST_IN_DATE AS StockLastInDate,
                ISNULL(p.QUARANTINE, 0) AS Quarantine,
                CAST(p.PKId AS varchar) AS LionCode
            FROM PRODUCT p
            JOIN PRODUCT_GROUP pg ON p.GROUP_ID = pg.PKId
            JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId AND mp.MARKET_ID = @MarketId
            JOIN MARKET mkt ON mkt.PKId = mp.MARKET_ID
            JOIN CURRENCY curMkt ON mkt.CURRENCY_ID = curMkt.PKId
            LEFT JOIN ANIMAL_SIZE az ON az.PKId = p.SIZE_ID
            LEFT JOIN CURRENCY_CONVERSION cv ON cv.TARGET_CURRENCY_ID = p.CURRENCY_ID AND cv.SOURCE_CURRENCY_ID = mkt.CURRENCY_ID
            LEFT JOIN PRODUCT_SUPPLIER_LINK psl ON psl.PRODUCT_ID = p.PKId
            WHERE 1=1";

        if (!filter.IncludeInactive)
            sql += " AND p.ACTIVE = 'Y'";
        else
            sql += " AND p.ACTIVE IN ('Y','N')";
        if (filter.ClassId > 0)
            sql += " AND pg.PRODUCT_CLASS_ID = @ClassId";
        if (filter.GroupId > 0)
            sql += " AND p.GROUP_ID = @GroupId";
        if (!string.IsNullOrWhiteSpace(filter.Name) && filter.Name.Length >= 2)
            sql += " AND p.NAME LIKE @NameLike";
        if (!string.IsNullOrWhiteSpace(filter.SciName) && filter.SciName.Length >= 2)
            sql += " AND p.SCI_NAME LIKE @SciNameLike";
        if (filter.Code > 0)
            sql += " AND p.PKId = @Code";
        if (filter.SupplierId > 0)
            sql += " AND psl.SUPPLIER_ID = @SupplierId";

        sql += " ORDER BY p.NAME";

        var nameLike = string.IsNullOrWhiteSpace(filter.Name) || filter.Name.Length < 2 ? null : $"%{filter.Name.Trim()}%";
        var sciNameLike = string.IsNullOrWhiteSpace(filter.SciName) || filter.SciName.Length < 2 ? null : $"%{filter.SciName.Trim()}%";

        var list = await _connection.QueryAsync<ProductSummaryDto>(
            new CommandDefinition(sql, new
            {
                MarketId = marketId,
                filter.ClassId,
                filter.GroupId,
                NameLike = nameLike,
                SciNameLike = sciNameLike,
                filter.Code,
                filter.SupplierId
            }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<ProductEditDto?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        const string sqlProduct = @"
            SELECT p.PKId, pg.PRODUCT_CLASS_ID AS ClassId, p.GROUP_ID AS GroupId, p.NAME, p.SCI_NAME AS SciName, p.EXTERNAL_PKID AS ExternalPkId,
                p.COST_GROSS AS CostGross, p.COST_TRANSPORT AS CostTransport, p.DISCOUNT AS Discount,
                p.COST_NET AS CostNet, p.COST_FINAL AS CostFinal, p.WEIGHT AS Weight,
                p.FISCAL_CLASS_ID AS FiscalClassId, p.CURRENCY_ID AS CurrencyId, p.CST_ID AS CstId, p.CSTB_ID AS CstbId,
                p.pH AS Ph, p.BAR_CODE AS BarCode, p.STOCK_MIN AS StockMin, p.PACK AS Pack,
                p.SIZE_ID AS SizeId, p.STOCK AS Stock, p.STOCK_LAST_IN_DATE AS StockLastInDate,
                CASE WHEN p.ACTIVE = 'Y' THEN 1 ELSE 0 END AS Active, ISNULL(p.QUARANTINE, 0) AS Quarantine
            FROM PRODUCT p
            JOIN PRODUCT_GROUP pg ON p.GROUP_ID = pg.PKId
            WHERE p.PKId = @ProductId";
        var product = await _connection.QuerySingleOrDefaultAsync<ProductEditDto>(
            new CommandDefinition(sqlProduct, new { ProductId = productId }, cancellationToken: cancellationToken));
        if (product == null)
            return null;

        const string sqlSuppliers = "SELECT SUPPLIER_ID AS Id FROM PRODUCT_SUPPLIER_LINK WHERE PRODUCT_ID = @ProductId";
        var supplierIds = await _connection.QueryAsync<int>(
            new CommandDefinition(sqlSuppliers, new { ProductId = productId }, cancellationToken: cancellationToken));
        product.SupplierIds = supplierIds.ToList();

        const string sqlMarketProduct = @"
            SELECT m.PKId AS MarketId, m.NAME AS MarketName, ISNULL(mp.PROFIT, 0) AS ProfitPercent, ISNULL(mp.PRICE, 0) AS Price
            FROM MARKET m
            LEFT JOIN MARKET_PRODUCT mp ON mp.MARKET_ID = m.PKId AND mp.PRODUCT_ID = @ProductId
            ORDER BY m.NAME";
        var marketProducts = await _connection.QueryAsync<MarketProductItemDto>(
            new CommandDefinition(sqlMarketProduct, new { ProductId = productId }, cancellationToken: cancellationToken));
        product.MarketProducts = marketProducts.ToList();

        return product;
    }

    public async Task<int> CreateAsync(ProductCreateDto dto, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var costGross = dto.CostGross ?? 0;
        var discount = dto.Discount ?? 0;
        var costTransport = dto.CostTransport ?? 0;
        var costNet = costGross * (1 - discount / 100m);
        var costFinal = costNet * costTransport;
        var price = costFinal * (1 + dto.ProfitPercent / 100m);

        // Truncate to column lengths to avoid "String or binary data would be truncated"
        var appId = (applicationId ?? "").Length > 8 ? (applicationId ?? "").Substring(0, 8) : (applicationId ?? "");
        var usrId = (userId ?? "").Length > 20 ? (userId ?? "").Substring(0, 20) : (userId ?? "SYS");
        var name = (dto.Name ?? "").Length > 200 ? dto.Name.Substring(0, 200) : (dto.Name ?? "");
        var sciName = dto.SciName != null && dto.SciName.Length > 150 ? dto.SciName.Substring(0, 150) : dto.SciName;
        var externalPkId = dto.ExternalPkId != null && dto.ExternalPkId.Length > 20 ? dto.ExternalPkId.Substring(0, 20) : dto.ExternalPkId;
        var barCode = dto.BarCode != null && dto.BarCode.Length > 15 ? dto.BarCode.Substring(0, 15) : dto.BarCode;
        var cstbId = string.IsNullOrEmpty(dto.CstbId) ? "" : dto.CstbId;

        const string sqlInsert = @"
            INSERT INTO PRODUCT (GROUP_ID, NAME, SCI_NAME, COST_GROSS, COST_TRANSPORT, COST_FINAL, WEIGHT,
                FISCAL_CLASS_ID, CURRENCY_ID, CST_ID, CSTB_ID, pH, BAR_CODE, STOCK, STOCK_MIN, PACK,
                SYS_CREATION_DATE, APPLICATION_ID, USER_ID, SIZE_ID, DISCOUNT, EXTERNAL_PKID, COST_NET, ACTIVE)
            VALUES (@GroupId, @Name, @SciName, @CostGross, @CostTransport, @CostFinal, @Weight,
                @FiscalClassId, @CurrencyId, @CstId, @CstbId, @Ph, @BarCode, 0, @StockMin, @Pack,
                GETDATE(), @ApplicationId, @UserId, @SizeId, @Discount, @ExternalPkId, @CostNet, 'Y');
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        var newId = await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sqlInsert, new
            {
                dto.GroupId,
                Name = name,
                SciName = sciName ?? (object)DBNull.Value,
                CostGross = costGross,
                CostTransport = costTransport,
                CostFinal = costFinal,
                dto.Weight,
                dto.FiscalClassId,
                dto.CurrencyId,
                dto.CstId,
                CstbId = cstbId,
                Ph = dto.Ph ?? (object)DBNull.Value,
                BarCode = barCode ?? (object)DBNull.Value,
                dto.StockMin,
                dto.Pack,
                ApplicationId = appId,
                UserId = usrId,
                SizeId = dto.SizeId ?? (object)DBNull.Value,
                Discount = discount,
                ExternalPkId = externalPkId ?? (object)DBNull.Value,
                CostNet = costNet
            }, cancellationToken: cancellationToken));

        const string sqlMarketProduct = "INSERT INTO MARKET_PRODUCT (MARKET_ID, PRODUCT_ID, PROFIT, PRICE) VALUES (@MarketId, @ProductId, @Profit, @Price)";
        await _connection.ExecuteAsync(
            new CommandDefinition(sqlMarketProduct, new { MarketId = dto.MarketId, ProductId = newId, Profit = dto.ProfitPercent, Price = price }, cancellationToken: cancellationToken));

        const string sqlSupplierLink = "INSERT INTO PRODUCT_SUPPLIER_LINK (PRODUCT_ID, SUPPLIER_ID) VALUES (@ProductId, @SupplierId)";
        foreach (var supId in dto.SupplierIds)
        {
            await _connection.ExecuteAsync(
                new CommandDefinition(sqlSupplierLink, new { ProductId = newId, SupplierId = supId }, cancellationToken: cancellationToken));
        }

        return newId;
    }

    public async Task<bool> UpdateAsync(ProductEditDto dto, CancellationToken cancellationToken = default)
    {
        var costGross = dto.CostGross ?? 0;
        var discount = dto.Discount ?? 0;
        var costTransport = dto.CostTransport ?? 0;
        var costNet = costGross * (1 - discount / 100m);
        var costFinal = costNet * costTransport;

        const string sqlUpdate = @"
            UPDATE PRODUCT SET
                GROUP_ID = @GroupId, NAME = @Name, SCI_NAME = @SciName, EXTERNAL_PKID = @ExternalPkId,
                COST_GROSS = @CostGross, COST_TRANSPORT = @CostTransport, DISCOUNT = @Discount,
                COST_NET = @CostNet, COST_FINAL = @CostFinal, WEIGHT = @Weight,
                FISCAL_CLASS_ID = @FiscalClassId, CURRENCY_ID = @CurrencyId, CST_ID = @CstId, CSTB_ID = @CstbId,
                pH = @Ph, BAR_CODE = @BarCode, STOCK_MIN = @StockMin, PACK = @Pack, SIZE_ID = @SizeId,
                SYS_UPDATE_DATE = GETDATE(), ACTIVE = @Active, QUARANTINE = @Quarantine
            WHERE PKId = @PKId";

        var rows = await _connection.ExecuteAsync(
            new CommandDefinition(sqlUpdate, new
            {
                dto.PKId,
                dto.GroupId,
                dto.Name,
                SciName = dto.SciName ?? (object)DBNull.Value,
                ExternalPkId = dto.ExternalPkId ?? (object)DBNull.Value,
                CostGross = costGross,
                CostTransport = costTransport,
                Discount = discount,
                CostNet = costNet,
                CostFinal = costFinal,
                dto.Weight,
                dto.FiscalClassId,
                dto.CurrencyId,
                dto.CstId,
                CstbId = string.IsNullOrEmpty(dto.CstbId) ? "" : dto.CstbId,
                Ph = dto.Ph ?? (object)DBNull.Value,
                BarCode = dto.BarCode ?? (object)DBNull.Value,
                dto.StockMin,
                dto.Pack,
                SizeId = dto.SizeId ?? (object)DBNull.Value,
                Active = dto.Active ? "Y" : "N",
                Quarantine = dto.Quarantine
            }, cancellationToken: cancellationToken));

        if (rows == 0)
            return false;

        await _connection.ExecuteAsync(
            new CommandDefinition("DELETE FROM PRODUCT_SUPPLIER_LINK WHERE PRODUCT_ID = @ProductId", new { ProductId = dto.PKId }, cancellationToken: cancellationToken));
        const string sqlLink = "INSERT INTO PRODUCT_SUPPLIER_LINK (PRODUCT_ID, SUPPLIER_ID) VALUES (@ProductId, @SupplierId)";
        foreach (var supId in dto.SupplierIds)
        {
            await _connection.ExecuteAsync(
                new CommandDefinition(sqlLink, new { ProductId = dto.PKId, SupplierId = supId }, cancellationToken: cancellationToken));
        }

        foreach (var mp in dto.MarketProducts)
        {
            const string sqlUpsert = @"
                IF EXISTS (SELECT 1 FROM MARKET_PRODUCT WHERE PRODUCT_ID = @ProductId AND MARKET_ID = @MarketId)
                    UPDATE MARKET_PRODUCT SET PROFIT = @Profit, PRICE = @Price WHERE PRODUCT_ID = @ProductId AND MARKET_ID = @MarketId;
                ELSE
                    INSERT INTO MARKET_PRODUCT (PRODUCT_ID, MARKET_ID, PROFIT, PRICE) VALUES (@ProductId, @MarketId, @Profit, @Price);";
            await _connection.ExecuteAsync(
                new CommandDefinition(sqlUpsert, new { ProductId = dto.PKId, mp.MarketId, Profit = mp.ProfitPercent, mp.Price }, cancellationToken: cancellationToken));
        }

        return true;
    }
}
