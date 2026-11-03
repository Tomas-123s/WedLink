using Microsoft.AspNetCore.Identity;

namespace WedLink.DataAccess.Identity;

public class AdminUser : IdentityUser
{
    public bool IsActive { get; set; } = true;
}
