using System.Threading.Tasks;
using SmartPantry.ExternalProducts;

namespace SmartPantry.ExternalProducts;

public class ProductLookupAppService : SmartPantryAppService, IProductLookupAppService
{
    private readonly IExternalProductCatalogClient _catalogClient;

    public ProductLookupAppService(IExternalProductCatalogClient catalogClient)
    {
        _catalogClient = catalogClient;
    }

    public async Task<ProductLookupResultDto> GetByBarcodeAsync(GetProductByBarcodeInput input)
    {
        try
        {
            var external = await _catalogClient.GetByBarcodeAsync(input.Barcode);

            if (external is null)
            {
                return new ProductLookupResultDto { Status = ProductLookupStatus.NotFound };
            }

            return new ProductLookupResultDto
            {
                Status = ProductLookupStatus.Found,
                Product = new ExternalProductInfoDto
                {
                    Barcode = external.Barcode,
                    Name = external.Name,
                    Brand = external.Brand,
                    Quantity = external.Quantity,
                    ImageUrl = external.ImageUrl,
                    EnergyKcalPer100g = external.EnergyKcalPer100g,
                    FatPer100g = external.FatPer100g,
                    SugarsPer100g = external.SugarsPer100g,
                    SaltPer100g = external.SaltPer100g
                }
            };
        }
        catch (ExternalCatalogRateLimitException)
        {
            return new ProductLookupResultDto { Status = ProductLookupStatus.RateLimited };
        }
        catch (ExternalCatalogUnavailableException)
        {
            return new ProductLookupResultDto { Status = ProductLookupStatus.ServiceUnavailable };
        }
    }
}
