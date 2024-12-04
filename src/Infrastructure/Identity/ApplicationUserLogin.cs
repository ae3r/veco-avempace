using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

/// <summary>
/// ApplicationUserLogin class
/// </summary>
public class ApplicationUserLogin : IdentityUserLogin<int>
{
    public virtual ApplicationUser User { get; set; }
}
