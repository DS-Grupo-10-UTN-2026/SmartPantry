using System;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace SmartPantry.Products;

public class ProductTests : SmartPantryDomainTestBase<SmartPantryDomainTestModule>
{
    [Fact]
    public void Should_Create_A_Valid_Product()
    {
        var product = new Product(
            Guid.NewGuid(),
            "  7790001112223  ",
            "  Arroz largo fino 1kg  "
        );

        product.Barcode.ShouldBe("7790001112223");
        product.Name.ShouldBe("Arroz largo fino 1kg");
    }

    [Fact]
    public void Should_Not_Create_A_Product_With_Empty_Barcode()
    {
        Should.Throw<AbpException>(() =>
        {
            new Product(Guid.NewGuid(), "   ", "Arroz largo fino 1kg");
        });
    }

    [Fact]
    public void Should_Not_Create_A_Product_With_Empty_Name()
    {
        Should.Throw<AbpException>(() =>
        {
            new Product(Guid.NewGuid(), "7790001112223", "   ");
        });
    }
}