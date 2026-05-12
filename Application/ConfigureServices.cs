using System.Reflection;
using Application.Inerfaces;
using Application.Inerfaces.Egypt;
using Application.Inerfaces.Inventory;
using Application.Inerfaces.POS;
using Application.Services;
using Application.Services.Egypt;
using Application.Services.Inventory;
using Application.Services.POS;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

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

        // Egypt
        services.AddScoped<IEInvoiceService, EInvoiceService>();

        return services;
    }
}
