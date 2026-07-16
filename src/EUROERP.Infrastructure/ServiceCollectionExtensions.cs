using System.Data;
using EUROERP.Application;
using EUROERP.Application.Address;
using EUROERP.Application.Auth;
using EUROERP.Application.Clients;
using EUROERP.Application.Products;
using EUROERP.Application.Orders;
using EUROERP.Application.Stock;
using EUROERP.Application.Suppliers;
using EUROERP.Application.Warranty;
using EUROERP.Application.Config;
using EUROERP.Application.NFe;
using EUROERP.Application.Nfes;
using EUROERP.Infrastructure.Config;
using EUROERP.Infrastructure.NFe;
using EUROERP.Infrastructure.Nfes;
using EUROERP.Infrastructure.Address;
using EUROERP.Infrastructure.Auth;
using EUROERP.Infrastructure.Clients;
using EUROERP.Infrastructure.Orders;
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
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IWarrantyService, WarrantyService>();
        services.AddScoped<IStockInService, StockInService>();
        services.AddScoped<IStockInMassService, StockInMassService>();
        services.AddScoped<IStockAssetsReportService, StockAssetsReportService>();
        services.AddScoped<IStockAssetsBySupplierService, StockAssetsBySupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPurchaseStockService, PurchaseStockService>();
        services.AddScoped<ICityResolutionService, CityResolutionService>();
        services.AddScoped<ISysControlService, SysControlService>();
        services.AddScoped<NfesConfigService>();
        services.AddScoped<INfesConfigService>(sp => sp.GetRequiredService<NfesConfigService>());
        services.AddScoped<INfesConfigProvider>(sp => sp.GetRequiredService<NfesConfigService>());
        services.AddScoped<INfesCertificateProvider, NfesCertificateProvider>();
        services.AddScoped<INfesPrefeituraClient, NfesPrefeituraClient>();
        services.AddScoped<INfesSimplissClient, NfesSimplissClient>();
        services.AddScoped<PrefeituraSpNfesBackend>();
        services.AddScoped<SimplissNfesBackend>();
        services.AddScoped<INfesEmissionService, NfesEmissionService>();
        services.AddScoped<INfesCancellationService, NfesCancellationService>();

        services.AddSingleton<INfeCertificateActiveConfigStore, NfeCertificateActiveConfigStore>();
        services.AddSingleton<INfeCertificateProvider, NfeCertificateProvider>();
        services.AddScoped<INfeXmlBuilder, NfeXmlBuilder>();
        services.AddScoped<INfeXmlSigner, NfeXmlSigner>();
        services.AddScoped<INfeSchemaValidator, NfeSchemaValidator>();
        services.AddScoped<INfeFileStorage, NfeFileStorage>();
        services.AddScoped<INfePdfGenerator, NfePdfGenerator>();
        services.AddScoped<INfeSefazClient, NfeSefazClient>();
        services.AddScoped<INfeIndividualService, NfeIndividualService>();
        services.AddScoped<IReceiptInNfeDataService, ReceiptInNfeDataService>();

        return services;
    }
}
