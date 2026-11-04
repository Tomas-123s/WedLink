using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WedLink.DataAccess.Customers;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Invitations;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Persistence.Configurations;
using WedLink.DataAccess.Persistence.Configurations.Orders;
using WedLink.DataAccess.Persistence.Configurations.Invitations;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence;

public class WedLinkDbContext : IdentityDbContext<AdminUser>
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderAddOn> OrderAddOns => Set<OrderAddOn>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();
    public DbSet<OrderPaymentCorrection> OrderPaymentCorrections => Set<OrderPaymentCorrection>();
    public DbSet<PrivateLinkToken> PrivateLinkTokens => Set<PrivateLinkToken>();
    public DbSet<CustomerWeddingSubmission> CustomerWeddingSubmissions => Set<CustomerWeddingSubmission>();
    public DbSet<CustomerSubmissionMedia> CustomerSubmissionMedia => Set<CustomerSubmissionMedia>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<InvitationCustomization> InvitationCustomizations => Set<InvitationCustomization>();
    public DbSet<InvitationMedia> InvitationMedia => Set<InvitationMedia>();
    public DbSet<PreviewReviewEvent> PreviewReviewEvents => Set<PreviewReviewEvent>();
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
        modelBuilder.ApplyConfiguration(new OrderPaymentConfiguration());
        modelBuilder.ApplyConfiguration(new OrderPaymentCorrectionConfiguration());
        modelBuilder.ApplyConfiguration(new PrivateLinkTokenConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerWeddingSubmissionConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerSubmissionMediaConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationCustomizationConfiguration());
        modelBuilder.ApplyConfiguration(new InvitationMediaConfiguration());
        modelBuilder.ApplyConfiguration(new PreviewReviewEventConfiguration());
        modelBuilder.ApplyConfiguration(new TemplateConfiguration());
        modelBuilder.ApplyConfiguration(new TemplateVersionConfiguration());
        modelBuilder.ApplyConfiguration(new FeatureConfiguration());
        modelBuilder.ApplyConfiguration(new TemplateFeatureConfiguration());
        modelBuilder.ApplyConfiguration(new AddOnConfiguration());
        modelBuilder.ApplyConfiguration(new TemplateAddOnConfiguration());
    }
}
