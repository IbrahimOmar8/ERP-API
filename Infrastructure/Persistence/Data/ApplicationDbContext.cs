using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Domain.Models.Inventory;
using Domain.Models.POS;
using Domain.Models.Egypt;
using Domain.Models.Auth;
using Domain.Models.Accounting;
using Domain.Models.HR;
using Domain.Models.Integration;
using Domain.Models.Loyalty;
using Domain.Models.Cheques;
using Domain.Models.Notifications;
using Domain.Models.Payments;
using Domain.Models.Production;

namespace Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<LogHistory> LogHistories { get; set; }

        // Inventory
        public DbSet<Category> Categories { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<StockItem> StockItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<StockTransfer> StockTransfers { get; set; }
        public DbSet<StockTransferItem> StockTransferItems { get; set; }

        // POS
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CashRegister> CashRegisters { get; set; }
        public DbSet<CashSession> CashSessions { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }
        public DbSet<SalePayment> SalePayments { get; set; }
        public DbSet<SaleReturn> SaleReturns { get; set; }
        public DbSet<SaleReturnItem> SaleReturnItems { get; set; }
        public DbSet<HeldOrder> HeldOrders { get; set; }
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuotationItem> QuotationItems { get; set; }

        // Egypt
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<TaxRate> TaxRates { get; set; }
        public DbSet<EInvoiceSubmission> EInvoiceSubmissions { get; set; }

        // Accounting
        public DbSet<Expense> Expenses { get; set; }

        // Loyalty
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
        public DbSet<LoyaltySettings> LoyaltySettings { get; set; }

        // Integration
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; }
        public DbSet<WebhookDelivery> WebhookDeliveries { get; set; }

        // Payments (customer/supplier ledger)
        public DbSet<CustomerPayment> CustomerPayments { get; set; }
        public DbSet<SupplierPayment> SupplierPayments { get; set; }

        // Notifications
        public DbSet<Notification> Notifications { get; set; }

        // Cheques
        public DbSet<Cheque> Cheques { get; set; }

        // Production
        public DbSet<BillOfMaterials> BillsOfMaterials { get; set; }
        public DbSet<BomComponent> BomComponents { get; set; }
        public DbSet<ProductionOrder> ProductionOrders { get; set; }
        public DbSet<ProductionOrderItem> ProductionOrderItems { get; set; }

        // HR / Payroll
        public DbSet<Position> Positions { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<EmployeeLoan> EmployeeLoans { get; set; }

        // Auth
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Department>()
                .HasMany(w => w.Employees)
                .WithOne(s => s.Department)
                .HasForeignKey(s => s.DepartmentId);

            // Money columns with precision 18,4 to support EGP small denominations
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var prop in entity.GetProperties())
                {
                    if (prop.ClrType == typeof(decimal) || prop.ClrType == typeof(decimal?))
                    {
                        prop.SetPrecision(18);
                        prop.SetScale(4);
                    }
                }
            }

            // Inventory
            modelBuilder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Unit)
                .WithMany()
                .HasForeignKey(p => p.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Sku).IsUnique();
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode);

            modelBuilder.Entity<StockItem>()
                .HasIndex(s => new { s.ProductId, s.WarehouseId }).IsUnique();

            modelBuilder.Entity<StockItem>()
                .HasOne(s => s.Product)
                .WithMany(p => p.StockItems)
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockItem>()
                .HasOne(s => s.Warehouse)
                .WithMany(w => w.StockItems)
                .HasForeignKey(s => s.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockMovement>()
                .HasOne(m => m.Warehouse).WithMany().HasForeignKey(m => m.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockMovement>().Ignore(m => m.TotalCost);

            modelBuilder.Entity<PurchaseInvoice>()
                .HasMany(p => p.Items).WithOne(i => i.PurchaseInvoice)
                .HasForeignKey(i => i.PurchaseInvoiceId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PurchaseInvoice>().Ignore(p => p.Remaining);

            modelBuilder.Entity<StockTransfer>()
                .HasMany(t => t.Items).WithOne(i => i.StockTransfer)
                .HasForeignKey(i => i.StockTransferId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<StockTransfer>()
                .HasOne(t => t.FromWarehouse).WithMany().HasForeignKey(t => t.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockTransfer>()
                .HasOne(t => t.ToWarehouse).WithMany().HasForeignKey(t => t.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // POS
            modelBuilder.Entity<Sale>()
                .HasMany(s => s.Items).WithOne(i => i.Sale)
                .HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Sale>()
                .HasMany(s => s.Payments).WithOne(p => p.Sale)
                .HasForeignKey(p => p.SaleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Sale>()
                .HasIndex(s => s.InvoiceNumber).IsUnique();
            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Customer).WithMany(c => c.Sales)
                .HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SaleReturn>()
                .HasMany(r => r.Items).WithOne(i => i.SaleReturn)
                .HasForeignKey(i => i.SaleReturnId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<SaleReturn>()
                .HasOne(r => r.OriginalSale).WithMany().HasForeignKey(r => r.OriginalSaleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashSession>()
                .HasOne(s => s.CashRegister).WithMany(r => r.Sessions)
                .HasForeignKey(s => s.CashRegisterId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<CashSession>()
                .HasMany(s => s.Sales).WithOne(s => s.CashSession)
                .HasForeignKey(s => s.CashSessionId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CashRegister>()
                .HasOne(r => r.Warehouse).WithMany().HasForeignKey(r => r.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Auth
            modelBuilder.Entity<User>()
                .HasIndex(u => u.UserName).IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name).IsUnique();

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User).WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role).WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(t => t.User).WithMany()
                .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(t => t.Token).IsUnique();
            modelBuilder.Entity<RefreshToken>().Ignore(t => t.IsActive);

            // Loyalty
            modelBuilder.Entity<Coupon>()
                .HasIndex(c => c.Code).IsUnique();
            modelBuilder.Entity<LoyaltyTransaction>()
                .HasIndex(t => t.CustomerId);

            // Integration
            modelBuilder.Entity<ApiKey>()
                .HasIndex(k => k.KeyHash).IsUnique();

            // Quotations
            modelBuilder.Entity<Quotation>()
                .HasIndex(q => q.QuotationNumber).IsUnique();
            modelBuilder.Entity<Quotation>()
                .HasMany(q => q.Items).WithOne(i => i.Quotation)
                .HasForeignKey(i => i.QuotationId).OnDelete(DeleteBehavior.Cascade);

            // HR
            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(a => new { a.EmployeeId, a.Date }).IsUnique();
            modelBuilder.Entity<Payroll>()
                .HasIndex(p => new { p.EmployeeId, p.Year, p.Month }).IsUnique();
            modelBuilder.Entity<ShiftAssignment>()
                .HasIndex(a => new { a.EmployeeId, a.EffectiveFrom });

            // Cheques
            modelBuilder.Entity<Cheque>()
                .HasIndex(c => new { c.Type, c.Status });
            modelBuilder.Entity<Cheque>()
                .HasIndex(c => c.DueDate);

            // Production
            modelBuilder.Entity<ProductionOrder>()
                .HasIndex(p => p.OrderNumber).IsUnique();
            modelBuilder.Entity<BillOfMaterials>()
                .HasMany(b => b.Components).WithOne(c => c.BillOfMaterials)
                .HasForeignKey(c => c.BillOfMaterialsId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProductionOrder>()
                .HasMany(p => p.Items).WithOne(i => i.ProductionOrder)
                .HasForeignKey(i => i.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
