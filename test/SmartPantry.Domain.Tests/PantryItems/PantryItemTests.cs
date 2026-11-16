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

    [Fact]
    public void Should_Create_A_PantryItem_With_Quantity_And_Normalized_Unit()
    {
        var item = new PantryItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            quantity: 2.5m, unit: "  kg ");

        item.Quantity.ShouldBe(2.5m);
        item.Unit.ShouldBe("kg");
    }

    [Fact]
    public void Quantity_And_Unit_Are_Optional()
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        item.Quantity.ShouldBeNull();
        item.Unit.ShouldBeNull();
    }

    [Fact]
    public void Blank_Unit_Is_Stored_As_Null()
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        item.SetQuantity(1m, "   ");

        item.Unit.ShouldBeNull();
    }

    [Fact]
    public void Should_Not_Accept_A_Negative_Quantity()
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Should.Throw<ArgumentOutOfRangeException>(() => item.SetQuantity(-1m, "kg"));
    }

    [Fact]
    public void Should_Not_Accept_A_Unit_Longer_Than_The_Maximum()
    {
        var item = new PantryItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var tooLong = new string('x', PantryItemConsts.MaxUnitLength + 1);

        Should.Throw<ArgumentException>(() => item.SetQuantity(1m, tooLong));
    }

    [Fact]
    public void An_Invalid_Update_Leaves_The_Previous_State()
    {
        var item = new PantryItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            quantity: 1m, unit: "kg");

        Should.Throw<ArgumentOutOfRangeException>(() => item.SetQuantity(-5m, "g"));

        item.Quantity.ShouldBe(1m);
        item.Unit.ShouldBe("kg");
    }
}