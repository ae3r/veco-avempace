using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// ApplicationUserClaim class
/// </summary>
public class ApplicationUserClaim : IdentityUserClaim<int>
{
    public string Description { get; set; }
    public virtual ApplicationUser User { get; set; }
    public ApplicationUserClaim() : base()
    {
    }
    public ApplicationUserClaim(string userClaimDescription = null) : base()
    {
        Description = userClaimDescription;
    }
}
