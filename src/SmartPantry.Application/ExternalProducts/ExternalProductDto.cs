namespace SmartPantry.ExternalProducts;

public class ExternalProductDto
{
    public string Barcode { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? EnergyKcalPer100g { get; set; }
    public decimal? FatPer100g { get; set; }
    public decimal? SugarsPer100g { get; set; }
    public decimal? SaltPer100g { get; set; }
}