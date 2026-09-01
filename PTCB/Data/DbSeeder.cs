using Microsoft.AspNetCore.Identity;

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
}