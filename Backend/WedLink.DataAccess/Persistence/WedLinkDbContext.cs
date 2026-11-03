using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WedLink.DataAccess.Customers;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Persistence.Configurations;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence;

public class WedLinkDbContext : IdentityDbContext<AdminUser>
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderAddOn> OrderAddOns => Set<OrderAddOn>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<TemplateFeature> TemplateFeatures => Set<TemplateFeature>();
    public DbSet<AddOn> AddOns => Set<AddOn>();
    public DbSet<TemplateAddOn> TemplateAddOns => Set<TemplateAddOn>();

    public WedLinkDbContext(DbContextOptions<WedLinkDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new AdminUserConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderAddOnConfiguration());
        modelBuilder.ApplyConfiguration(new OrderStatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new TemplateConfiguration());
        modelBuilder.ApplyConfiguration(new TemplateVersionConfiguration());
        modelBuilder.ApplyConfiguration(new FeatureConfiguration());
        modelBuilder.ApplyConfiguration(new TemplateFeatureConfiguration());
        modelBuilder.ApplyConfiguration(new AddOnConfiguration());
        modelBuilder.ApplyConfiguration(new TemplateAddOnConfiguration());
    }
}
