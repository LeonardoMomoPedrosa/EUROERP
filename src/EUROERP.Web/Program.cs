using System.Security.Claims;
using EUROERP.Application;
using EUROERP.Application.Address;
using EUROERP.Application.Auth;
using EUROERP.Application.Nfes;
using EUROERP.Application.Products;
using EUROERP.Infrastructure;
using EUROERP.Infrastructure.Address;
using EUROERP.Web.Components;
using EUROERP.Web.Infrastructure;
using EUROERP.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient<IViaCepService, ViaCepService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddSingleton<IAuthorizationHandler, AllowAnonymousForBlazorFrameworkHandler>();

builder.Services.AddSingleton<IMenuService, MenuService>();
builder.Services.AddScoped<IMenuStateService, MenuStateService>();
builder.Services.AddScoped<ILayoutStateService, LayoutStateService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();

app.MapPost("/auth/login", async (HttpContext ctx, IAuthService authService) =>
{
    var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);
    var userName = form["UserName"].ToString();
    var password = form["Password"].ToString();
    var returnUrl = form["ReturnUrl"].ToString();
    if (string.IsNullOrWhiteSpace(returnUrl)) returnUrl = "/";

    var result = await authService.ValidateAsync(userName ?? "", password ?? "");
    if (!result.Success || !result.UserId.HasValue || result.UserName == null)
        return Results.Redirect($"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.UserId.Value.ToString()),
        new(ClaimTypes.Name, result.UserName)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
        new AuthenticationProperties { IsPersistent = true });
    return Results.Redirect(returnUrl);
}).AllowAnonymous();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

static ProductListRequest ParseProductListRequest(HttpRequest request)
{
    var mode = request.Query["mode"].ToString() switch
    {
        "priceInStock" => ProductListMode.PriceInStock,
        "stock" => ProductListMode.Stock,
        _ => ProductListMode.PriceAll
    };
    var allGroups = request.Query["allGroups"] == "1";
    var groupIds = new List<byte>();
    var raw = request.Query["groupIds"].ToString();
    if (!string.IsNullOrWhiteSpace(raw))
    {
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (byte.TryParse(part, out var id))
                groupIds.Add(id);
        }
    }
    return new ProductListRequest { Mode = mode, AllGroups = allGroups, GroupIds = groupIds };
}

app.MapGet("/api/productlist/export/pdf", async (HttpRequest request, IProductListExportService exportService) =>
{
    var bytes = await exportService.GeneratePdfAsync(ParseProductListRequest(request));
    var mode = request.Query["mode"].ToString();
    var prefix = mode == "stock" ? "saldos" : "precos";
    var fileName = $"{prefix}_{DateTime.Today:yyyyMMdd}.pdf";
    return Results.File(bytes, "application/pdf", fileName);
}).RequireAuthorization();

app.MapGet("/api/productlist/export/excel", async (HttpRequest request, IProductListExportService exportService) =>
{
    var bytes = await exportService.GenerateExcelAsync(ParseProductListRequest(request));
    var mode = request.Query["mode"].ToString();
    var prefix = mode == "stock" ? "saldos" : "precos";
    var fileName = $"{prefix}_{DateTime.Today:yyyyMMdd}.xlsx";
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
}).RequireAuthorization();

app.MapGet("/api/nfes/imprimir", async (HttpRequest request, INfesEmissionService nfesService) =>
{
    if (!int.TryParse(request.Query["orderId"], out var orderId) || orderId <= 0)
        return Results.BadRequest("Informe orderId válido.");

    var result = await nfesService.GetDanfsePdfAsync(orderId);
    if (!result.Success || result.PdfBytes == null)
        return Results.Problem(detail: result.Message, statusCode: StatusCodes.Status400BadRequest);

    return Results.File(result.PdfBytes, "application/pdf");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
