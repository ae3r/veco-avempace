using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// ApplicationRole class
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    public string Description { get; set; }
    public virtual ICollection<ApplicationRoleClaim> RoleClaims { get; set; }
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }
    public ApplicationRole() : base()
    {
        RoleClaims = new HashSet<ApplicationRoleClaim>();
    }
    public ApplicationRole(string roleName, string roleDescription = null) : base(roleName)
    {
        RoleClaims = new HashSet<ApplicationRoleClaim>();
        Description = roleDescription;
    }
}
