using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PTCB.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        const string adminRole = "Admin";
        const string adminUserName = "admin";
        const string adminPassword = "Admin123!";

        if (!await roleManager.RoleExistsAsync(adminRole))
            await roleManager.CreateAsync(new IdentityRole(adminRole));

        if (await userManager.FindByNameAsync(adminUserName) is null)
        {
            var admin = new ApplicationUser { UserName = adminUserName };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, adminRole);
        }
    }

    public static async Task SeedCentralBankAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        const string centralBankUserName = BankService.CentralBankUserName;
        const string centralBankPassword = "CentralBank123!";
        const decimal initialBalance = 100000m;

        if (await userManager.FindByNameAsync(centralBankUserName) is null)
        {
            var centralBank = new ApplicationUser { UserName = centralBankUserName };
            var result = await userManager.CreateAsync(centralBank, centralBankPassword);
            if (result.Succeeded)
            {
                centralBank.SumSumBalance = initialBalance;
                await db.SaveChangesAsync();

                var txDb = services.GetRequiredService<TransactionDbContext>();
                txDb.Transactions.Add(new Transaction
                {
                    UserId = centralBank.Id,
                    Amount = initialBalance,
                    Type = "Deposit",
                    Description = "Initial central bank issuance",
                    Timestamp = DateTime.UtcNow
                });
                await txDb.SaveChangesAsync();
            }
        }
    }
}
