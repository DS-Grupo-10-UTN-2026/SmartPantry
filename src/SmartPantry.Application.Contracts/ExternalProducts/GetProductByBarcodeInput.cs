using System.ComponentModel.DataAnnotations;

namespace SmartPantry.ExternalProducts;

public class GetProductByBarcodeInput
{
    [Required]
    [RegularExpression(@"^\d{8,14}$",
        ErrorMessage = "El código de barras debe tener entre 8 y 14 dígitos.")]
    public string Barcode { get; set; } = string.Empty;
}