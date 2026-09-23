using System.ComponentModel.DataAnnotations;

namespace SmartPantry.Products;

public class UpdateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}