namespace EUROERP.Application.Stock;

public interface IStockAssetsReportService
{
    Task<StockAssetsResultDto> SearchAsync(StockAssetsFilterDto filter, CancellationToken cancellationToken = default);
}
