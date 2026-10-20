using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace SmartPantry.ExpirationWarnings;

public class ExpirationWarning : AggregateRoot<Guid>
{
    public string LogicalKey { get; private set; } = default!;
    public Guid PantryItemId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public ExpirationWarningType Type { get; private set; }
    public DateOnly ExpirationDate { get; private set; }
    public ExpirationSeverity Severity { get; private set; }
    public bool IsActive { get; private set; }

    protected ExpirationWarning()
    {
        // Requerido por EF Core
    }

    public ExpirationWarning(
        Guid id,
        Guid pantryItemId,
        Guid ownerUserId,
        DateOnly expirationDate,
        ExpirationSeverity severity) : base(id)
    {
        PantryItemId = Check.NotDefaultOrNull<Guid>(pantryItemId, nameof(pantryItemId));
        OwnerUserId = Check.NotDefaultOrNull<Guid>(ownerUserId, nameof(ownerUserId));
        Type = ExpirationWarningType.Expiration;
        LogicalKey = ExpirationWarningConsts.BuildLogicalKey(pantryItemId, Type);
        Refresh(expirationDate, severity);
    }

    /// <summary>Actualiza los datos del aviso y lo (re)activa.</summary>
    public void Refresh(DateOnly expirationDate, ExpirationSeverity severity)
    {
        ExpirationDate = expirationDate;
        Severity = severity;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}