using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Products;

public class CreateProductDto
{
    [Required]
    [StringLength(ProductConsts.MaxBarcodeLength)]
    public string Barcode { get; set; }

    [Required]
    [StringLength(ProductConsts.MaxNameLength)]
    public string Name { get; set; }
}