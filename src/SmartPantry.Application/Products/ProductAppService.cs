using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Products;

public class ProductAppService
    : CrudAppService<Product, ProductDto, Guid, PagedAndSortedResultRequestDto, CreateProductDto, UpdateProductDto>,
      IProductAppService
{
    private readonly IProductRepository _productRepository;

    public ProductAppService(IProductRepository productRepository) : base(productRepository)
    {
        _productRepository = productRepository;
    }

    public override async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        if (await _productRepository.BarcodeExistsAsync(input.Barcode))
        {
            throw new BusinessException("SmartPantry:BarcodeAlreadyExists")
                        .WithData("Barcode", input.Barcode); ;
        }

        var product = new Product(GuidGenerator.Create(), input.Barcode, input.Name);
        product = await _productRepository.InsertAsync(product);

        return await MapToGetOutputDtoAsync(product);
    }

    protected override Task MapToEntityAsync(UpdateProductDto updateInput, Product entity)
    {
        entity.SetName(updateInput.Name);
        return Task.CompletedTask;
    }
}