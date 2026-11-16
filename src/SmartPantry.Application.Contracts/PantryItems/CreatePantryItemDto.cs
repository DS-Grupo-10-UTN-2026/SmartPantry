using System;
using System.ComponentModel.DataAnnotations;

namespace SmartPantry.PantryItems;

public class CreatePantryItemDto
{
    public Guid ProductId { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    [Range(0d, 999999999999d)]
    public decimal? Quantity { get; set; }

    [StringLength(PantryItemConsts.MaxUnitLength)]
    public string? Unit { get; set; }
}