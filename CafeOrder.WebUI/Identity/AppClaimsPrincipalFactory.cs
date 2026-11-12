using System.Security.Claims;
using CafeOrder.EntityLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CafeOrder.WebUI.Identity;

/// <summary>
/// Giriş çerezine (cookie) kullanıcının adını da yazar. Böylece her sayfada adı göstermek için veritabanına gitmeyiz.
/// </summary>
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public const string FullNameClaim = "full_name";

    public AppClaimsPrincipalFactory(UserManager<AppUser> users, RoleManager<IdentityRole> roles, IOptions<IdentityOptions> options)
        : base(users, roles, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(FullNameClaim, string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "Admin" : user.FullName));
        return identity;
    }
}
