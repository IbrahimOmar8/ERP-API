using System.Reflection;
using Application.Inerfaces;
using Application.Inerfaces.Accounting;
using Application.Inerfaces.Auth;
using Application.Inerfaces.Egypt;
using Application.Inerfaces.HR;
using Application.Inerfaces.Import;
using Application.Inerfaces.Integration;
using Application.Inerfaces.Inventory;
using Application.Inerfaces.Loyalty;
using Application.Inerfaces.Notifications;
using Application.Inerfaces.Payments;
using Application.Inerfaces.POS;
using Application.Inerfaces.Reports;
using Application.Services;
using Application.Services.Accounting;
using Application.Services.Auth;
using Application.Services.Egypt;
using Application.Services.HR;
using Application.Services.Import;
using Application.Services.Integration;
using Application.Services.Inventory;
using Application.Services.Loyalty;
using Application.Services.Notifications;
using Application.Services.Payments;
using Application.Services.POS;
using Application.Services.Reports;
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

        // HR
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IEmployeeHrService, EmployeeHrService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<IPayrollService, PayrollService>();

        // Inventory
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
        services.AddScoped<IStockTransferService, StockTransferService>();

        // POS
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICashRegisterService, CashRegisterService>();
        services.AddScoped<ICashSessionService, CashSessionService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IHeldOrderService, HeldOrderService>();
        services.AddScoped<IQuotationService, QuotationService>();

        // Egypt - ETA e-invoicing
        services.AddHttpClient<IEtaTokenService, EtaTokenService>();
        services.AddHttpClient<IEInvoiceService, EInvoiceService>((sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EtaSettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(10, opts.RequestTimeoutSeconds));
        });
        services.AddScoped<ICompanyProfileService, CompanyProfileService>();

        // Reports
        services.AddScoped<IReportService, ReportService>();

        // Accounting
        services.AddScoped<IExpenseService, ExpenseService>();

        // Import
        services.AddScoped<IImportService, ImportService>();

        // Loyalty / Coupons
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();

        // Integration
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddHttpClient<IWebhookService, WebhookService>();

        // Customer / Supplier payments
        services.AddScoped<ICustomerPaymentService, CustomerPaymentService>();
        services.AddScoped<ISupplierPaymentService, SupplierPaymentService>();

        // Notifications (bell icon)
        services.AddScoped<INotificationService, NotificationService>();

        // Auth
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
