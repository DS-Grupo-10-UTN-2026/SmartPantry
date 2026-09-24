using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SmartPantry.Products;

public interface IProductAppService : ICrudAppService<ProductDto, ProductDto, Guid, PagedAndSortedResultRequestDto, CreateProductDto, UpdateProductDto>
{
}