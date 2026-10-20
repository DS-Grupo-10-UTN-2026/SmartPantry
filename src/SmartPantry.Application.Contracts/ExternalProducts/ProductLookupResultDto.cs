namespace SmartPantry.ExternalProducts;

public class ProductLookupResultDto
{
    public ProductLookupStatus Status { get; set; }
    public ExternalProductInfoDto? Product { get; set; }
}