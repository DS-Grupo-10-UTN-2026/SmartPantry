using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace SmartPantry.Products;

public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly IProductRepository _productRepository;

    public ProductAppService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        if (await _productRepository.BarcodeExistsAsync(input.Barcode))
        {
            throw new BusinessException("SmartPantry:BarcodeAlreadyExists")
                .WithData("Barcode", input.Barcode);
        }

        var product = new Product(GuidGenerator.Create(), input.Barcode, input.Name);
        product = await _productRepository.InsertAsync(product);

        return ObjectMapper.Map<Product, ProductDto>(product);
    }

    public async Task<ProductDto> GetAsync(Guid id)
    {
        var product = await _productRepository.GetAsync(id);
        return ObjectMapper.Map<Product, ProductDto>(product);
    }
}