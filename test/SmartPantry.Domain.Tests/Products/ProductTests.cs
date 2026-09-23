using System;
using Shouldly;
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
        Should.Throw<ArgumentException>(() =>
        {
            new Product(Guid.NewGuid(), "   ", "Arroz largo fino 1kg");
        });
    }

    [Fact]
    public void Should_Not_Create_A_Product_With_Empty_Name()
    {
        Should.Throw<ArgumentException>(() =>
        {
            new Product(Guid.NewGuid(), "7790001112223", "   ");
        });
    }
    [Fact]
    public void SetName_Con_Nombre_Valido_Lo_Normaliza()
    {
        var product = new Product(Guid.NewGuid(), "7790001", "Leche");

        product.SetName("  Leche entera  ");

        product.Name.ShouldBe("Leche entera");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_Con_Nombre_Invalido_Lanza_Y_Conserva_El_Estado(string? invalido)
    {
        var product = new Product(Guid.NewGuid(), "7790001", "Leche");

        Should.Throw<ArgumentException>(() => product.SetName(invalido!));

        product.Name.ShouldBe("Leche");
    }

    [Fact]
    public void SetName_Demasiado_Largo_Lanza_Y_Conserva_El_Estado()
    {
        var product = new Product(Guid.NewGuid(), "7790001", "Leche");
        var largo = new string('a', ProductConsts.MaxNameLength + 1);

        Should.Throw<ArgumentException>(() => product.SetName(largo));

        product.Name.ShouldBe("Leche");
    }
}