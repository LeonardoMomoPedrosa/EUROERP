using System.Data;
using Dapper;
using EUROERP.Application.Products;

namespace EUROERP.Infrastructure.Products;

public class ProductMassInfoService : IProductMassInfoService
{
    private readonly IDbConnection _connection;

    public ProductMassInfoService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<ProductMassInfoItemDto>> GetListForMassInfoAsync(ProductFilterDto filter, CancellationToken cancellationToken = default)
    {
        var marketId = filter.MarketId == 0 ? (byte)1 : filter.MarketId;
        var hasParam = filter.ClassId > 0 || filter.GroupId > 0 ||
                       (!string.IsNullOrWhiteSpace(filter.Name) && filter.Name.Length >= 2) ||
                       (!string.IsNullOrWhiteSpace(filter.SciName) && filter.SciName.Length >= 2) ||
                       filter.Code > 0 || filter.SupplierId > 0;
        if (!hasParam)
            return new List<ProductMassInfoItemDto>();

        var sql = @"
            SELECT DISTINCT
                p.PKId,
                p.EXTERNAL_PKID AS ExternalPkId,
                p.ACTIVE AS Active,
                ISNULL(p.QUARANTINE, 0) AS Quarantine,
                ISNULL((SELECT TOP 1 SUPPLIER_ID FROM PRODUCT_SUPPLIER_LINK WHERE PRODUCT_ID = p.PKId), 0) AS SupplierId,
                ISNULL(p.CFAT, 0) AS CFat,
                p.NAME AS Name,
                p.SCI_NAME AS SciName,
                mp.NAME AS MktName,
                mp.SCI_NAME AS MktSciName,
                p.SIZE_ID AS SizeId,
                p.pH AS Ph,
                p.CST_ID AS CstId,
                p.CSTB_ID AS CstbId,
                p.STOCK_MIN AS StockMin,
                p.PACK AS Pack,
                p.STOCK AS Stock,
                pg.PRODUCT_CLASS_ID AS ProductClassId
            FROM PRODUCT p
            JOIN PRODUCT_GROUP pg ON p.GROUP_ID = pg.PKId
            JOIN MARKET_PRODUCT mp ON mp.PRODUCT_ID = p.PKId AND mp.MARKET_ID = @MarketId
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

        var list = await _connection.QueryAsync<ProductMassInfoItemDto>(
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

    public async Task<(bool Success, string Message)> UpdateRowAsync(ProductMassInfoUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Active != "Y" && dto.Active != "N" && dto.Active != "X")
            return (false, "Ativo deve ser Y, N ou X.");

        if (dto.StockMin < 0 || dto.StockMin > 32767)
            return (false, "Saldo mínimo deve estar entre 0 e 32767.");
        if (dto.Pack < 1 || dto.Pack > 100000)
            return (false, "Emb/Cx deve estar entre 1 e 100000.");
        if (dto.Ph.HasValue && (dto.Ph < 0 || dto.Ph > 15))
            return (false, "pH deve estar entre 0 e 15.");

        try
        {
            var name = (dto.Name ?? "").Length > 200 ? dto.Name!.Substring(0, 200) : (dto.Name ?? "");
            var sciName = dto.SciName != null && dto.SciName.Length > 150 ? dto.SciName.Substring(0, 150) : dto.SciName;
            var mktName = dto.MktName != null && dto.MktName.Length > 200 ? dto.MktName.Substring(0, 200) : dto.MktName;
            var mktSciName = dto.MktSciName != null && dto.MktSciName.Length > 150 ? dto.MktSciName.Substring(0, 150) : dto.MktSciName;
            var externalPkId = dto.ExternalPkId != null && dto.ExternalPkId.Length > 20 ? dto.ExternalPkId.Substring(0, 20) : dto.ExternalPkId;
            var cstbId = string.IsNullOrEmpty(dto.CstbId) ? "" : dto.CstbId;

            var sqlProduct = @"
                UPDATE PRODUCT SET
                    NAME = @Name,
                    SCI_NAME = @SciName,
                    EXTERNAL_PKID = @ExternalPkId,
                    ACTIVE = @Active,
                    QUARANTINE = @Quarantine,
                    CFAT = @CFat,
                    SIZE_ID = @SizeId,
                    STOCK_MIN = @StockMin,
                    PACK = @Pack,
                    pH = @Ph,
                    CST_ID = @CstId,
                    CSTB_ID = @CstbId,
                    SYS_UPDATE_DATE = GETDATE()
                WHERE PKId = @PKId";

            await _connection.ExecuteAsync(
                new CommandDefinition(sqlProduct, new
                {
                    dto.PKId,
                    Name = name,
                    SciName = sciName ?? (object)DBNull.Value,
                    ExternalPkId = externalPkId ?? (object)DBNull.Value,
                    dto.Active,
                    dto.Quarantine,
                    dto.CFat,
                    SizeId = (dto.SizeId == 0 || dto.SizeId == null) ? (object)DBNull.Value : dto.SizeId,
                    dto.StockMin,
                    dto.Pack,
                    Ph = dto.Ph ?? (object)DBNull.Value,
                    dto.CstId,
                    CstbId = cstbId
                }, cancellationToken: cancellationToken));

            if (dto.MarketId > 1)
            {
                var sqlMarket = @"
                    UPDATE MARKET_PRODUCT SET NAME = @MktName, SCI_NAME = @MktSciName
                    WHERE PRODUCT_ID = @PKId AND MARKET_ID = @MarketId";
                await _connection.ExecuteAsync(
                    new CommandDefinition(sqlMarket, new
                    {
                        dto.PKId,
                        dto.MarketId,
                        MktName = mktName ?? (object)DBNull.Value,
                        MktSciName = mktSciName ?? (object)DBNull.Value
                    }, cancellationToken: cancellationToken));
            }

            // PRODUCT_SUPPLIER_LINK: one main supplier per product in this screen
            var currentLinks = await _connection.QueryAsync<int>(
                new CommandDefinition("SELECT SUPPLIER_ID FROM PRODUCT_SUPPLIER_LINK WHERE PRODUCT_ID = @ProductId", new { ProductId = dto.PKId }, cancellationToken: cancellationToken));
            var currentSupplierId = currentLinks.FirstOrDefault();

            if (dto.SupplierId <= 0)
            {
                if (currentSupplierId != 0)
                    await _connection.ExecuteAsync(
                        new CommandDefinition("DELETE FROM PRODUCT_SUPPLIER_LINK WHERE PRODUCT_ID = @ProductId AND SUPPLIER_ID = @SupplierId",
                            new { ProductId = dto.PKId, SupplierId = currentSupplierId }, cancellationToken: cancellationToken));
            }
            else if (currentSupplierId != dto.SupplierId)
            {
                if (currentSupplierId != 0)
                    await _connection.ExecuteAsync(
                        new CommandDefinition("DELETE FROM PRODUCT_SUPPLIER_LINK WHERE PRODUCT_ID = @ProductId AND SUPPLIER_ID = @SupplierId",
                            new { ProductId = dto.PKId, SupplierId = currentSupplierId }, cancellationToken: cancellationToken));
                await _connection.ExecuteAsync(
                    new CommandDefinition("INSERT INTO PRODUCT_SUPPLIER_LINK (PRODUCT_ID, SUPPLIER_ID) VALUES (@ProductId, @SupplierId)",
                        new { ProductId = dto.PKId, dto.SupplierId }, cancellationToken: cancellationToken));
            }

            return (true, "Linha atualizada");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
