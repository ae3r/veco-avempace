using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Infrastructure.Services;

/// <summary>
/// ApplicationClaimsIdentityFactory class
/// </summary>
public class ApplicationClaimsIdentityFactory : UserClaimsPrincipalFactory<ApplicationUser,ApplicationRole>
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// Constructor : Initializes a new instance of ApplicationClaimsIdentityFactory 
    /// </summary>
    /// <param name="userManager"></param>
    /// <param name="roleManager"></param>
    /// <param name="optionsAccessor"></param>
    public ApplicationClaimsIdentityFactory(UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor) : base(userManager,roleManager, optionsAccessor)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }
    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);

        if (!string.IsNullOrEmpty(user.Site))
        {
            ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                new Claim(ClaimTypes.Locality, user.Site)
            });
        }
        if (!string.IsNullOrEmpty(user.ProfilePictureDataUrl))
        {
            //((ClaimsIdentity)principal.Identity).AddClaims(new[] {
            //    new Claim(ApplicationClaimTypes.ProfilePictureDataUrl, user.ProfilePictureDataUrl)
            //});
        }
        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                new Claim(ClaimTypes.GivenName, user.DisplayName)
            });
        }
        var appuser = await _userManager.FindByIdAsync(user.Id.ToString());
        var roles = await _userManager.GetRolesAsync(appuser);
        foreach (var rolename in roles)
        {
            var role = await _roleManager.FindByNameAsync(rolename);
            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims)
            {
                ((ClaimsIdentity)principal.Identity).AddClaim(claim);
            }
            ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
                new Claim(ClaimTypes.Role, rolename) });
        }
        return principal;
    }
}
