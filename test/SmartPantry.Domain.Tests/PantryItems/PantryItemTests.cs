using System;
using Shouldly;
using Xunit;

namespace SmartPantry.PantryItems;

public class PantryItemTests : SmartPantryDomainTestBase<SmartPantryDomainTestModule>
{
    [Fact]
    public void Should_Create_An_Active_PantryItem()
    {
        var date = new DateOnly(2026, 10, 20);
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), date);

        item.Status.ShouldBe(PantryItemStatus.Active);
        item.ExpirationDate.ShouldBe(date);
    }

    [Fact]
    public void Should_Not_Create_A_PantryItem_Without_Owner()
    {
        Should.Throw<ArgumentException>(() =>
            new PantryItem(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Should_Not_Create_A_PantryItem_Without_Product()
    {
        Should.Throw<ArgumentException>(() =>
            new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void MarkAsConsumed_Cambia_El_Estado()
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        item.MarkAsConsumed();

        item.Status.ShouldBe(PantryItemStatus.Consumed);
    }
}