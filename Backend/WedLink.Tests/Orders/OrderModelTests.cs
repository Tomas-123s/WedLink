using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using WedLink.DataAccess.Customers;
using WedLink.DataAccess.Identity;
using WedLink.DataAccess.Orders;
using WedLink.DataAccess.Persistence;
using WedLink.DataAccess.Templates;

namespace WedLink.Tests.Orders;

public sealed class OrderModelTests
{
    [Fact]
    public void OrderModel_EnforcesRelationshipsAndNonNegativeBasePrice()
    {
        using var dbContext = CreateDbContext();
        var entity = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Order))!;

        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_Orders_AgreedBasePrice_NonNegative"
            && constraint.Sql == "[AgreedBasePrice] >= 0");
        AssertRestrictDelete(entity, nameof(Order.CustomerId), typeof(Customer));
        AssertRestrictDelete(entity, nameof(Order.SelectedTemplateVersionId), typeof(TemplateVersion));
    }

    [Fact]
    public void OrderAddOnModel_PreservesSnapshotAndPreventsDuplicateSelection()
    {
        using var dbContext = CreateDbContext();
        var entity = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(OrderAddOn))!;

        Assert.Contains(entity.GetCheckConstraints(), constraint =>
            constraint.Name == "CK_OrderAddOns_PriceAtPurchase_NonNegative"
            && constraint.Sql == "[PriceAtPurchase] >= 0");
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(OrderAddOn.OrderId), nameof(OrderAddOn.AddOnId)]));
        Assert.Equal(150, entity.FindProperty(nameof(OrderAddOn.NameAtPurchase))!.GetMaxLength());
    }

    [Fact]
    public void OrderStatusHistory_UsesIdentityStringKeyAndRestrictsDeletes()
    {
        using var dbContext = CreateDbContext();
        var entity = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(OrderStatusHistory))!;
        var adminForeignKey = entity.GetForeignKeys().Single(foreignKey =>
            foreignKey.Properties.Any(property =>
                property.Name == nameof(OrderStatusHistory.ChangedByAdminUserId)));

        Assert.Equal(typeof(AdminUser), adminForeignKey.PrincipalEntityType.ClrType);
        Assert.Equal(typeof(string), adminForeignKey.Properties.Single().ClrType);
        Assert.Equal(DeleteBehavior.Restrict, adminForeignKey.DeleteBehavior);
        AssertRestrictDelete(entity, nameof(OrderStatusHistory.OrderId), typeof(Order));
    }

    private static WedLinkDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WedLinkDbContext>()
            .UseSqlServer("Server=(local);Database=WedLinkModelTests;Integrated Security=True;")
            .Options;
        return new WedLinkDbContext(options);
    }

    private static void AssertRestrictDelete(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entity,
        string propertyName,
        Type principalType)
    {
        var foreignKey = entity.GetForeignKeys().Single(key =>
            key.Properties.Any(property => property.Name == propertyName));

        Assert.Equal(principalType, foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }
}
