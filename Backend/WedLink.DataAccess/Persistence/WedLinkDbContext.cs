using Microsoft.EntityFrameworkCore;

namespace WedLink.DataAccess.Persistence;

public class WedLinkDbContext : DbContext
{
    public WedLinkDbContext(DbContextOptions<WedLinkDbContext> options)
        : base(options)
    {
    }
}
