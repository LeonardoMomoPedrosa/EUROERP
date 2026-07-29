using System.Data;
using EUROERP.Application;
using EUROERP.Application.Account;
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
using EUROERP.Application.SalesReports;
using EUROERP.Application.AccountsPayable;
using EUROERP.Application.AccountsReceivable;
using EUROERP.Application.CashFlow;
using EUROERP.Application.ClearCustomer;
using EUROERP.Application.ReferenceData;
using EUROERP.Application.RevenueReporting;
using EUROERP.Application.Activities;
using EUROERP.Application.Markets;
using EUROERP.Application.Master;
using EUROERP.Application.RoleActivities;
using EUROERP.Application.Roles;
using EUROERP.Application.UserActivities;
using EUROERP.Application.UserManagement;
using EUROERP.Application.UserRoles;
using EUROERP.Application.Widgets;
using EUROERP.Infrastructure.Account;
using EUROERP.Infrastructure.AccountsPayable;
using EUROERP.Infrastructure.AccountsReceivable;
using EUROERP.Infrastructure.CashFlow;
using EUROERP.Infrastructure.ClearCustomer;
using EUROERP.Infrastructure.ReferenceData;
using EUROERP.Infrastructure.RevenueReporting;
using EUROERP.Infrastructure.Config;
using EUROERP.Infrastructure.NFe;
using EUROERP.Infrastructure.Nfes;
using EUROERP.Infrastructure.SalesReports;
using EUROERP.Infrastructure.Address;
using EUROERP.Infrastructure.Auth;
using EUROERP.Infrastructure.Clients;
using EUROERP.Infrastructure.Orders;
using EUROERP.Infrastructure.Products;
using EUROERP.Infrastructure.Stock;
using EUROERP.Infrastructure.Suppliers;
using EUROERP.Infrastructure.Warranty;
using EUROERP.Infrastructure.Activities;
using EUROERP.Infrastructure.Markets;
using EUROERP.Infrastructure.Master;
using EUROERP.Infrastructure.RoleActivities;
using EUROERP.Infrastructure.Roles;
using EUROERP.Infrastructure.UserActivities;
using EUROERP.Infrastructure.UserManagement;
using EUROERP.Infrastructure.UserRoles;
using EUROERP.Infrastructure.Widgets;
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

        services.AddMemoryCache();

        services.AddScoped<IDbConnection>(_ => new SqlConnection(connectionString));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
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
        services.AddScoped<ISalesGroupReportService, SalesGroupReportService>();
        services.AddScoped<IClientSalesRankService, ClientSalesRankService>();

        services.AddScoped<IBillsToPaySearchService, BillsToPaySearchService>();
        services.AddScoped<ICreateBillsToPayService, CreateBillsToPayService>();
        services.AddScoped<IUpdateBillsToPayService, UpdateBillsToPayService>();
        services.AddScoped<IBillsToPayPaymentService, BillsToPayPaymentService>();
        services.AddScoped<IBillsToPayReportDailyService, BillsToPayReportDailyService>();
        services.AddScoped<IBillsToPayReportPaymentByGroupService, BillsToPayReportPaymentByGroupService>();
        services.AddScoped<IBillsToPayApproveService, BillsToPayApproveService>();

        services.AddScoped<IBillsToReceiveSearchService, BillsToReceiveSearchService>();
        services.AddScoped<IUpdateBillsToReceiveService, UpdateBillsToReceiveService>();
        services.AddScoped<IBillsToReceiveReceiveService, BillsToReceiveReceiveService>();
        services.AddScoped<IBillsToReceiveReportService, BillsToReceiveReportService>();

        services.AddScoped<IRevenueReportDailyService, RevenueReportDailyService>();
        services.AddScoped<IRevenueReportMonthlyService, RevenueReportMonthlyService>();
        services.AddScoped<IRevenueReportMonthlySupplierService, RevenueReportMonthlySupplierService>();
        services.AddScoped<IRevenueReportYearlyService, RevenueReportYearlyService>();
        services.AddScoped<IClearCustomerService, ClearCustomerService>();
        services.AddScoped<ICashFlowReportService, CashFlowReportService>();

        services.AddScoped<IProductGroupService, ProductGroupService>();
        services.AddScoped<IFiscalClassService, FiscalClassService>();
        services.AddScoped<ICurrencyService, CurrencyService>();

        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserRolesService, UserRolesService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IRoleActivityService, RoleActivityService>();
        services.AddScoped<IUserActivityService, UserActivityService>();
        services.AddScoped<IMarketUserService, MarketUserService>();
        services.AddScoped<IMasterSqlService, MasterSqlService>();

        services.AddScoped<IWidgetPreferenceService, WidgetPreferenceService>();
        services.AddScoped<IWidgetDailySalesDataService, WidgetDailySalesDataService>();
        services.AddScoped<IUserShortcutService, UserShortcutService>();

        return services;
    }
}
