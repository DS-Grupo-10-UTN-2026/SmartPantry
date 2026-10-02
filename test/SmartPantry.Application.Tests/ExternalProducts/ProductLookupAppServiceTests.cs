using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace SmartPantry.ExternalProducts;

public class ProductLookupAppServiceTests
{
    private const string ValidBarcode = "3017620422003";

    private readonly IExternalProductCatalogClient _catalogClient;
    private readonly ProductLookupAppService _service;

    public ProductLookupAppServiceTests()
    {
        _catalogClient = Substitute.For<IExternalProductCatalogClient>();
        _service = new ProductLookupAppService(_catalogClient);
    }

    [Fact]
    public async Task Should_Return_Found_When_Product_Exists()
    {
        _catalogClient.GetByBarcodeAsync(ValidBarcode).Returns(
            Task.FromResult<ExternalProductDto?>(new ExternalProductDto
            {
                Barcode = ValidBarcode,
                Name = "Nutella",
                Brand = "Ferrero",
                Quantity = "400 g",
                ImageUrl = "https://example.com/nutella.jpg",
                EnergyKcalPer100g = 539,
                FatPer100g = 30.9m,
                SugarsPer100g = 56.3m,
                SaltPer100g = 0.107m
            }));

        var result = await _service.GetByBarcodeAsync(
            new GetProductByBarcodeInput { Barcode = ValidBarcode });

        Assert.Equal(ProductLookupStatus.Found, result.Status);
        Assert.NotNull(result.Product);
        Assert.Equal(ValidBarcode, result.Product!.Barcode);
        Assert.Equal("Nutella", result.Product.Name);
        Assert.Equal("Ferrero", result.Product.Brand);
        Assert.Equal(539m, result.Product.EnergyKcalPer100g);
        await _catalogClient.Received(1).GetByBarcodeAsync(ValidBarcode);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        _catalogClient.GetByBarcodeAsync(Arg.Any<string>())
            .Returns(Task.FromResult<ExternalProductDto?>(null));

        var result = await _service.GetByBarcodeAsync(
            new GetProductByBarcodeInput { Barcode = "0000000000000" });

        Assert.Equal(ProductLookupStatus.NotFound, result.Status);
        Assert.Null(result.Product);
    }

    [Fact]
    public async Task Should_Keep_Missing_Fields_As_Null()
    {
        _catalogClient.GetByBarcodeAsync(ValidBarcode).Returns(
            Task.FromResult<ExternalProductDto?>(new ExternalProductDto
            {
                Barcode = ValidBarcode
            }));

        var result = await _service.GetByBarcodeAsync(
            new GetProductByBarcodeInput { Barcode = ValidBarcode });

        Assert.Equal(ProductLookupStatus.Found, result.Status);
        Assert.NotNull(result.Product);
        Assert.Null(result.Product!.Name);
        Assert.Null(result.Product.Brand);
        Assert.Null(result.Product.Quantity);
        Assert.Null(result.Product.ImageUrl);
        Assert.Null(result.Product.EnergyKcalPer100g);
        Assert.Null(result.Product.SaltPer100g);
    }

    [Fact]
    public async Task Should_Return_RateLimited_When_Provider_Limits_Requests()
    {
        _catalogClient.GetByBarcodeAsync(Arg.Any<string>())
            .ThrowsAsync(new ExternalCatalogRateLimitException());

        var result = await _service.GetByBarcodeAsync(
            new GetProductByBarcodeInput { Barcode = ValidBarcode });

        Assert.Equal(ProductLookupStatus.RateLimited, result.Status);
        Assert.Null(result.Product);
    }

    [Fact]
    public async Task Should_Return_ServiceUnavailable_When_Provider_Fails()
    {
        _catalogClient.GetByBarcodeAsync(Arg.Any<string>())
            .ThrowsAsync(new ExternalCatalogUnavailableException("Proveedor caído"));

        var result = await _service.GetByBarcodeAsync(
            new GetProductByBarcodeInput { Barcode = ValidBarcode });

        Assert.Equal(ProductLookupStatus.ServiceUnavailable, result.Status);
        Assert.Null(result.Product);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]          // 7 dígitos: muy corto
    [InlineData("123456789012345")]  // 15 dígitos: muy largo
    [InlineData("30176204A2003")]    // contiene una letra
    public void Barcode_Validation_Should_Reject_Invalid_Codes(string barcode)
    {
        var input = new GetProductByBarcodeInput { Barcode = barcode };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            input, new ValidationContext(input), results, validateAllProperties: true);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("12345678")]         // 8 dígitos: mínimo
    [InlineData("3017620422003")]    // 13 dígitos: EAN-13
    [InlineData("12345678901234")]   // 14 dígitos: máximo
    public void Barcode_Validation_Should_Accept_Valid_Codes(string barcode)
    {
        var input = new GetProductByBarcodeInput { Barcode = barcode };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            input, new ValidationContext(input), results, validateAllProperties: true);

        Assert.True(isValid);
    }
}