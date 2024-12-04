using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class ApplicationUserToken : IdentityUserToken<int>
{
    public virtual ApplicationUser User { get; set; }
}
