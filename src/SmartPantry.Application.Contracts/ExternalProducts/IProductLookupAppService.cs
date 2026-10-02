using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace SmartPantry.ExternalProducts;

public interface IProductLookupAppService : IApplicationService
{
    Task<ProductLookupResultDto> GetByBarcodeAsync(GetProductByBarcodeInput input);
}
