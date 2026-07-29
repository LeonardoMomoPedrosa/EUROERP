using System.Data;
using Dapper;
using EUROERP.Application.ReferenceData;
using Microsoft.Data.SqlClient;

namespace EUROERP.Infrastructure.ReferenceData;

public class ProductGroupService : IProductGroupService
{
    private const int SqlErrorForeignKeyConstraint = 547;
    private readonly IDbConnection _connection;

    public ProductGroupService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IReadOnlyList<ProductGroupDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT  pg.PKId AS Id,
                    pg.NAME AS Name,
                    ISNULL(pg.IGNORE_ORDER_DISC, 0) AS IgnoreOrderDisc,
                    pc.NAME AS ClassName,
                    pg.PRODUCT_CLASS_ID AS ProductClassId
            FROM PRODUCT_GROUP pg
            INNER JOIN PRODUCT_CLASS pc ON pg.PRODUCT_CLASS_ID = pc.PKId
            ORDER BY pc.NAME, pg.NAME";
        var list = await _connection.QueryAsync<ProductGroupDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken)).ConfigureAwait(false);
        return list.ToList();
    }

    public async Task CreateAsync(string name, int productClassId, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;
        var trimmedName = (name ?? "").Trim();
        if (trimmedName.Length > 150) trimmedName = trimmedName[..150];

        const string sql = @"
            INSERT INTO [PRODUCT_GROUP] ([NAME], [PRODUCT_CLASS_ID], [SYS_CREATION_DATE], [APPLICATION_ID], [USER_ID])
            VALUES (@Name, @ProductClassId, GETDATE(), @ApplicationId, @UserId)";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { Name = trimmedName, ProductClassId = productClassId, ApplicationId = appId, UserId = uid }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task UpdateAsync(int id, string name, bool ignoreOrderDisc, string applicationId, string userId, CancellationToken cancellationToken = default)
    {
        var appId = applicationId.Length > 8 ? applicationId[..8] : applicationId;
        var uid = userId.Length > 20 ? userId[..20] : userId;
        var trimmedName = (name ?? "").Trim();
        if (trimmedName.Length > 150) trimmedName = trimmedName[..150];

        const string sql = @"
            UPDATE [PRODUCT_GROUP]
            SET [NAME] = @Name, [IGNORE_ORDER_DISC] = @IgnoreOrderDisc, [SYS_UPDATE_DATE] = GETDATE(),
                [APPLICATION_ID] = @ApplicationId, [USER_ID] = @UserId
            WHERE PKId = @Id";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, Name = trimmedName, IgnoreOrderDisc = ignoreOrderDisc, ApplicationId = appId, UserId = uid }, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM [PRODUCT_GROUP] WHERE [PKId] = @Id";
        try
        {
            await _connection.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == SqlErrorForeignKeyConstraint)
        {
            throw new InvalidOperationException("Erro ao excluir grupo de produto. Verifique se existem produtos associados a ele.", ex);
        }
    }
}
