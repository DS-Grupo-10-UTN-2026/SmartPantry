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

    protected PantryItem()
    {
        // Requerido por EF Core
    }

    public PantryItem(Guid id, Guid ownerUserId, Guid productId, DateOnly? expirationDate = null) : base(id)
    {
        OwnerUserId = Check.NotDefaultOrNull<Guid>(ownerUserId, nameof(ownerUserId));
        ProductId = Check.NotDefaultOrNull<Guid>(productId, nameof(productId));
        ExpirationDate = expirationDate;
        Status = PantryItemStatus.Active;
    }

    public void SetExpirationDate(DateOnly? expirationDate)
    {
        ExpirationDate = expirationDate;
    }

    public void MarkAsConsumed()
    {
        Status = PantryItemStatus.Consumed;
    }
}