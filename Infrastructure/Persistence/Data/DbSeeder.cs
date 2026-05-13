using Domain.Enums;
using Domain.Models.Auth;
using Domain.Models.Egypt;
using Domain.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, string defaultAdminPassword)
        {
            await SeedRolesAsync(context);
            await SeedAdminUserAsync(context, defaultAdminPassword);
            await SeedTaxRatesAsync(context);
            await SeedCompanyProfileAsync(context);
            await SeedDefaultUnitsAsync(context);
            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            string[] defaultRoles = {
                Roles.Admin, Roles.Manager, Roles.Cashier,
                Roles.WarehouseKeeper, Roles.Accountant
            };

            foreach (var name in defaultRoles)
            {
                if (!await context.Roles.AnyAsync(r => r.Name == name))
                {
                    context.Roles.Add(new Role { Name = name, IsActive = true });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedAdminUserAsync(ApplicationDbContext context, string defaultPassword)
        {
            if (await context.Users.AnyAsync(u => u.UserName == "admin")) return;

            var adminRole = await context.Roles.FirstAsync(r => r.Name == Roles.Admin);

            var user = new User
            {
                UserName = "admin",
                FullName = "مدير النظام",
                Email = "admin@erp.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                IsActive = true
            };
            context.Users.Add(user);
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
            await context.SaveChangesAsync();
        }

        private static async Task SeedTaxRatesAsync(ApplicationDbContext context)
        {
            if (await context.TaxRates.AnyAsync()) return;
            context.TaxRates.AddRange(
                new TaxRate { Code = "VAT14", NameAr = "ضريبة قيمة مضافة 14%", NameEn = "VAT 14%", Rate = 14, EtaTaxType = "T1", EtaTaxSubType = "V009" },
                new TaxRate { Code = "VAT0", NameAr = "صفر %", NameEn = "Zero rated", Rate = 0, EtaTaxType = "T1", EtaTaxSubType = "V010" },
                new TaxRate { Code = "EXEMPT", NameAr = "معفاة", NameEn = "Exempt", Rate = 0, EtaTaxType = "T1", EtaTaxSubType = "V011" }
            );
        }

        private static async Task SeedCompanyProfileAsync(ApplicationDbContext context)
        {
            if (await context.CompanyProfiles.AnyAsync()) return;
            context.CompanyProfiles.Add(new CompanyProfile
            {
                NameAr = "اسم الشركة",
                TaxRegistrationNumber = "000-000-000",
                Address = "العنوان",
                Governorate = "القاهرة",
                EtaEnabled = false
            });
        }

        private static async Task SeedDefaultUnitsAsync(ApplicationDbContext context)
        {
            if (await context.Units.AnyAsync()) return;
            context.Units.AddRange(
                new Unit { Code = "EA", NameAr = "قطعة", NameEn = "Each" },
                new Unit { Code = "KG", NameAr = "كيلو", NameEn = "Kilogram" },
                new Unit { Code = "BOX", NameAr = "كرتونة", NameEn = "Box" },
                new Unit { Code = "L", NameAr = "لتر", NameEn = "Liter" },
                new Unit { Code = "M", NameAr = "متر", NameEn = "Meter" }
            );
        }
    }
}
