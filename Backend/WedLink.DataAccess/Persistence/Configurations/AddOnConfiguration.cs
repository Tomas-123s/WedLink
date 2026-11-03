using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WedLink.DataAccess.Templates;

namespace WedLink.DataAccess.Persistence.Configurations;

public class AddOnConfiguration : IEntityTypeConfiguration<AddOn>
{
    public void Configure(EntityTypeBuilder<AddOn> builder)
    {
        builder.HasKey(addOn => addOn.AddOnId);

        builder.Property(addOn => addOn.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(addOn => addOn.Description)
            .HasMaxLength(1000);

        builder.Property(addOn => addOn.CurrentPrice)
            .HasPrecision(18, 2);

        builder.Property(addOn => addOn.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("EGP")
            .IsRequired();

        builder.Property(addOn => addOn.IsActive)
            .HasDefaultValue(true);

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AddOns_CurrentPrice",
            "[CurrentPrice] >= 0"));
    }
}
