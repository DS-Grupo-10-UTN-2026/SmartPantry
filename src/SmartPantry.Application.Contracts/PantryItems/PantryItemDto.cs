using System;
using Volo.Abp.Application.Dtos;

namespace SmartPantry.PantryItems;

public class PantryItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }

    /// <summary>Nombre del producto; null si no está disponible (RF-09).</summary>
    public string? ProductName { get; set; }

    public PantryItemStatus Status { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public decimal? Quantity { get; set; }

    public string? Unit { get; set; }
}