using System.Data;
using Dapper;
using EUROERP.Application.Warranty;

namespace EUROERP.Infrastructure.Warranty;

public class WarrantyService : IWarrantyService
{
    private readonly IDbConnection _connection;

    public WarrantyService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<int> CreateAsync(WarrantyCreateDto dto, string userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO [WARRANTY]
                ([CLIENT_ID], [ORDER_ID], [SUP_NF], [EB_NF], [EB_CERT], [SUP_CERT],
                 [MODEL], [VEHICLE_TYPE], [CHASSI_NO], [BODY_NO], [PLATE_NO], [SERIAL_NO],
                 [INSTALLATION_DATE], [EXPIRATION_DATE], [SYS_CREATION_DATE], [USER_ID],
                 [INSTALLED_BY], [PARTS_USED], [MEMO])
            VALUES
                (@ClientId, @OrderId, @SupNf, @EbNf, @EbCert, @SupCert,
                 @Model, @VehicleType, @ChassiNo, @BodyNo, @PlateNo, @SerialNo,
                 @InstallationDate, @ExpirationDate, GETDATE(), @UserId,
                 @InstalledBy, @PartsUsed, @Memo);
            SELECT CAST(SCOPE_IDENTITY() AS int);";

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new
            {
                dto.ClientId,
                dto.OrderId,
                SupNf = dto.SupNf ?? "",
                EbNf = dto.EbNf ?? "",
                EbCert = dto.EbCert ?? "",
                SupCert = dto.SupCert ?? "",
                Model = dto.Model ?? "",
                dto.VehicleType,
                ChassiNo = dto.ChassiNo ?? "",
                BodyNo = dto.BodyNo ?? "",
                PlateNo = string.IsNullOrWhiteSpace(dto.PlateNo) ? "-" : dto.PlateNo.Trim(),
                SerialNo = dto.SerialNo ?? "",
                dto.InstallationDate,
                dto.ExpirationDate,
                UserId = userId,
                InstalledBy = dto.InstalledBy ?? "",
                PartsUsed = dto.PartsUsed ?? "",
                Memo = dto.Memo ?? ""
            }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<WarrantyListItemDto>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT PKId AS Id, USER_ID AS UserId, SYS_CREATION_DATE AS SysCreationDate
            FROM WARRANTY
            WHERE CLIENT_ID = @ClientId
            ORDER BY SYS_CREATION_DATE DESC";

        var list = await _connection.QueryAsync<WarrantyListItemDto>(
            new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));
        return list.ToList();
    }

    public async Task<WarrantyDto?> GetByIdAsync(int warrantyId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                PKId AS Id,
                CLIENT_ID AS ClientId,
                ORDER_ID AS OrderId,
                SUP_NF AS SupNf,
                EB_NF AS EbNf,
                EB_CERT AS EbCert,
                SUP_CERT AS SupCert,
                MODEL AS Model,
                VEHICLE_TYPE AS VehicleType,
                CHASSI_NO AS ChassiNo,
                BODY_NO AS BodyNo,
                PLATE_NO AS PlateNo,
                SERIAL_NO AS SerialNo,
                INSTALLATION_DATE AS InstallationDate,
                EXPIRATION_DATE AS ExpirationDate,
                INSTALLED_BY AS InstalledBy,
                PARTS_USED AS PartsUsed,
                MEMO AS Memo,
                SYS_CREATION_DATE AS SysCreationDate,
                USER_ID AS UserId
            FROM WARRANTY
            WHERE PKId = @WarrantyId";

        return await _connection.QueryFirstOrDefaultAsync<WarrantyDto>(
            new CommandDefinition(sql, new { WarrantyId = warrantyId }, cancellationToken: cancellationToken));
    }
}
