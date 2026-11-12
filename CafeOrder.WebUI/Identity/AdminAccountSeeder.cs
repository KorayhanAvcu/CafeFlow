using CafeOrder.EntityLayer.Entities;
using CafeOrder.EntityLayer.Enums;
using Microsoft.AspNetCore.Identity;

namespace CafeOrder.WebUI.Identity;

/// <summary>
/// Uygulama açılırken Admin rolünü ve (hiç admin yoksa) ilk admin hesabını bir kez oluşturur.
/// Şifre veritabanına düz metin olarak değil, Identity'nin ürettiği özet (hash) olarak yazılır.
/// </summary>
public static class AdminAccountSeeder
{
    public static async Task EnsureSeededAsync(IServiceProvider services, ILogger logger)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var options = services.GetRequiredService<IConfiguration>().GetSection("AdminAccount").Get<AdminAccountOptions>();

        if (!await roles.RoleExistsAsync(AppRoles.Admin))
        {
            await roles.CreateAsync(new IdentityRole(AppRoles.Admin));
        }

        if ((await users.GetUsersInRoleAsync(AppRoles.Admin)).Count > 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options?.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            logger.LogWarning("No admin account exists and the AdminAccount settings are empty, so nobody can sign in to /admin yet.");
            return;
        }

        var admin = new AppUser
        {
            UserName = options.Email,
            Email = options.Email,
            EmailConfirmed = true,
            FullName = options.FullName
        };

        var created = await users.CreateAsync(admin, options.Password);
        if (!created.Succeeded)
        {
            logger.LogError("The first admin account could not be created: {Errors}",
                string.Join(" ", created.Errors.Select(e => e.Description)));
            return;
        }

        await users.AddToRoleAsync(admin, AppRoles.Admin);
        logger.LogInformation("The first admin account {Email} was created.", options.Email);
    }
}
