using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Validation;
using Xunit;

namespace SmartPantry.Products;

public class ProductAppServiceTests : SmartPantryApplicationTestBase<SmartPantryApplicationTestModule>
{
    private readonly IProductAppService _productAppService;

    public ProductAppServiceTests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
    }

    [Fact]
    public async Task Should_Create_And_Get_A_Product()
    {
        var created = await _productAppService.CreateAsync(new CreateProductDto
        {
            Barcode = "7790001112223",
            Name = "Arroz largo fino 1kg"
        });

        created.Id.ShouldNotBe(Guid.Empty);
        created.Barcode.ShouldBe("7790001112223");
        created.Name.ShouldBe("Arroz largo fino 1kg");

        var fetched = await _productAppService.GetAsync(created.Id);

        fetched.Id.ShouldBe(created.Id);
        fetched.Barcode.ShouldBe(created.Barcode);
        fetched.Name.ShouldBe(created.Name);
    }

    [Fact]
    public async Task Should_Not_Create_A_Product_With_Duplicated_Barcode()
    {
        await _productAppService.CreateAsync(new CreateProductDto
        {
            Barcode = "7790009998887",
            Name = "Fideos tallarín 500g"
        });

        await Should.ThrowAsync<BusinessException>(async () =>
        {
            await _productAppService.CreateAsync(new CreateProductDto
            {
                Barcode = "7790009998887",
                Name = "Otro producto con el mismo código"
            });
        });
    }

    [Fact]
    public async Task Should_Throw_EntityNotFound_When_Getting_Unknown_Id()
    {
        await Should.ThrowAsync<EntityNotFoundException>(async () =>
        {
            await _productAppService.GetAsync(Guid.NewGuid());
        });
    }

    [Fact]
    public async Task Should_Not_Create_A_Product_Without_Required_Fields()
    {
        await Should.ThrowAsync<AbpValidationException>(async () =>
        {
            await _productAppService.CreateAsync(new CreateProductDto
            {
                Barcode = "",
                Name = "Producto sin barcode"
            });
        });
    }
}