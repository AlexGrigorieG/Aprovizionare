using Aprovizionare.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aprovizionare.Api.Data;

public static class DbSeeder
{
    private const string DefaultPassword = "Parola123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await SeedRolesAsync(services.GetRequiredService<RoleManager<IdentityRole>>());
        await SeedUsersAsync(services.GetRequiredService<UserManager<AppUser>>());
        await SeedCatalogAsync(db);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = [Roles.Admin, Roles.Purchasing, Roles.Warehouse, Roles.Accounting];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedUsersAsync(UserManager<AppUser> userManager)
    {
        (string Email, string FullName, string Role)[] users =
        [
            ("andrei@firma.ro", "Andrei Popescu", Roles.Purchasing),
            ("maria@firma.ro", "Maria Ionescu", Roles.Warehouse),
            ("elena@firma.ro", "Elena Dumitrescu", Roles.Accounting),
            ("admin@firma.ro", "Administrator", Roles.Admin),
        ];

        foreach (var (email, fullName, role) in users)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Nu am putut crea utilizatorul {email}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));

            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static async Task SeedCatalogAsync(AppDbContext db)
    {
        if (!await db.Suppliers.AnyAsync())
        {
            db.Suppliers.AddRange(
                new Supplier { Name = "Agro Parts SRL", TaxId = "RO12345678", Email = "comenzi@agroparts.example.com" },
                new Supplier { Name = "Garden Tools GmbH", TaxId = "DE811234567", Email = "orders@gardentools.example.com" },
                new Supplier { Name = "Tehnic Distribuție SA", TaxId = "RO87654321", Email = "vanzari@tehnicdistributie.example.com" });
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Code = "MC-100", Name = "Motocoasă 2,5 CP", Unit = "buc" },
                new Product { Code = "LD-040", Name = "Lanț drujbă 40 cm", Unit = "buc" },
                new Product { Code = "MP-001", Name = "Mănuși de protecție", Unit = "pereche" },
                new Product { Code = "UM-2T", Name = "Ulei motor 2T", Unit = "l" },
                new Product { Code = "IG-NPK", Name = "Îngrășământ NPK", Unit = "kg" });
        }

        await db.SaveChangesAsync();
    }
}
