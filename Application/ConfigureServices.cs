using System.Reflection;
using Application.Inerfaces;
using Application.Inerfaces.Auth;
using Application.Inerfaces.Egypt;
using Application.Inerfaces.Inventory;
using Application.Inerfaces.POS;
using Application.Services;
using Application.Services.Auth;
using Application.Services.Egypt;
using Application.Services.Inventory;
using Application.Services.POS;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        if (configuration != null)
            services.Configure<EtaSettings>(configuration.GetSection("EtaInvoicing"));

        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ILogHistoryService, LogHistoryService>();

        // Inventory
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();

        // POS
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICashRegisterService, CashRegisterService>();
        services.AddScoped<ICashSessionService, CashSessionService>();
        services.AddScoped<ISaleService, SaleService>();

        // Egypt - ETA e-invoicing
        services.AddHttpClient<IEtaTokenService, EtaTokenService>();
        services.AddHttpClient<IEInvoiceService, EInvoiceService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EtaSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(10, opts.RequestTimeoutSeconds));
        });

        // Auth
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
