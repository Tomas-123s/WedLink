using Microsoft.AspNetCore.Identity;

namespace WedLink.DataAccess.Identity;

public class AdminUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
