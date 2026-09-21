using Riok.Mapperly.Abstractions;
using SmartPantry.Products;
using Volo.Abp.Mapperly;

namespace SmartPantry;

[Mapper]
public partial class SmartPantryApplicationMappers : MapperBase<Product, ProductDto>
{
    public override partial ProductDto Map(Product source);

    public override partial void Map(Product source, ProductDto destination);
}