using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace SmartPantry.PantryItems;

public class PantryItem : AuditedAggregateRoot<Guid>
{
    public Guid OwnerUserId { get; private set; }
    public Guid ProductId { get; private set; }
    public PantryItemStatus Status { get; private set; }
    public DateOnly? ExpirationDate { get; private set; }
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }

    protected PantryItem()
    {
        // Requerido por EF Core
    }

    public PantryItem(
        Guid id,
        Guid ownerUserId,
        Guid productId,
        DateOnly? expirationDate = null,
        decimal? quantity = null,
        string? unit = null) : base(id)
    {
        OwnerUserId = Check.NotDefaultOrNull<Guid>(ownerUserId, nameof(ownerUserId));
        ProductId = Check.NotDefaultOrNull<Guid>(productId, nameof(productId));
        ExpirationDate = expirationDate;
        Status = PantryItemStatus.Active;
        SetQuantity(quantity, unit);
    }

    public void SetExpirationDate(DateOnly? expirationDate)
    {
        ExpirationDate = expirationDate;
    }

    /// <summary>
    /// Cantidad y unidad son opcionales. Se valida todo antes de asignar,
    /// para que un dato inválido no deje la entidad con cambios parciales.
    /// </summary>
    public void SetQuantity(decimal? quantity, string? unit)
    {
        if (quantity is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad no puede ser negativa.");
        }

        var normalizedUnit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        if (normalizedUnit is not null)
        {
            Check.Length(normalizedUnit, nameof(unit), PantryItemConsts.MaxUnitLength);
        }

        Quantity = quantity;
        Unit = normalizedUnit;
    }

    public void MarkAsConsumed()
    {
        Status = PantryItemStatus.Consumed;
    }
}