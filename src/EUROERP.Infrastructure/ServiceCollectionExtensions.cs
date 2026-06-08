using System.Data;
using EUROERP.Application;
using EUROERP.Application.Address;
using EUROERP.Application.Auth;
using EUROERP.Application.Clients;
using EUROERP.Application.Products;
using EUROERP.Application.Stock;
using EUROERP.Application.Suppliers;
using EUROERP.Application.Warranty;
using EUROERP.Infrastructure.Address;
using EUROERP.Infrastructure.Auth;
using EUROERP.Infrastructure.Clients;
using EUROERP.Infrastructure.Products;
using EUROERP.Infrastructure.Stock;
using EUROERP.Infrastructure.Suppliers;
using EUROERP.Infrastructure.Warranty;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EUROERP.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddScoped<IDbConnection>(_ => new SqlConnection(connectionString));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductReferenceService, ProductReferenceService>();
        services.AddScoped<IProductHistoryService, ProductHistoryService>();
        services.AddScoped<IProductMassInfoService, ProductMassInfoService>();
        services.AddScoped<IProductMassCostService, ProductMassCostService>();
        services.AddScoped<IProductListExportService, ProductListExportService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IClientReferenceService, ClientReferenceService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ISupplierReferenceService, SupplierReferenceService>();
        services.AddScoped<IWarrantyService, WarrantyService>();
        services.AddScoped<IStockInService, StockInService>();
        services.AddScoped<IStockInMassService, StockInMassService>();
        services.AddScoped<IStockAssetsReportService, StockAssetsReportService>();
        services.AddScoped<IStockAssetsBySupplierService, StockAssetsBySupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPurchaseStockService, PurchaseStockService>();
        services.AddScoped<ICityResolutionService, CityResolutionService>();
        return services;
    }
}
