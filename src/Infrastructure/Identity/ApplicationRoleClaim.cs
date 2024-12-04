using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// ApplicationRoleClaim class
/// </summary>
public class ApplicationRoleClaim : IdentityRoleClaim<int>
{
    public string Description { get; set; }
    public string Group { get; set; }
    public virtual ApplicationRole Role { get; set; }
    public ApplicationRoleClaim() : base()
    {
    }
    public ApplicationRoleClaim(string roleClaimDescription = null, string roleClaimGroup = null) : base()
    {
        Description = roleClaimDescription;
        Group = roleClaimGroup;
    }
}
