using System;
using System.Linq;
using System.Threading.Tasks;
using SmartPantry.PantryItems;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace SmartPantry.ExpirationWarnings;

public class ExpirationWarningManager : DomainService
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<ExpirationWarning, Guid> _warningRepository;

    public ExpirationWarningManager(
        IRepository<PantryItem, Guid> pantryItemRepository,
        IRepository<ExpirationWarning, Guid> warningRepository)
    {
        _pantryItemRepository = pantryItemRepository;
        _warningRepository = warningRepository;
    }

    /// <summary>
    /// Crea, actualiza o desactiva las advertencias de vencimiento según la fecha recibida.
    /// La fecha se recibe por parámetro (criterio UTC) para poder probar la regla sin depender del reloj.
    /// </summary>
    public async Task<ExpirationProcessingResult> ProcessAsync(
        DateOnly today,
        int thresholdDays = ExpirationWarningConsts.DefaultThresholdDays)
    {
        var limit = today.AddDays(thresholdDays);
        int created = 0, updated = 0, deactivated = 0;

        var items = await _pantryItemRepository.GetListAsync();
        var warningsByKey = (await _warningRepository.GetListAsync())
            .ToDictionary(w => w.LogicalKey);

        foreach (var item in items)
        {
            var key = ExpirationWarningConsts.BuildLogicalKey(item.Id, ExpirationWarningType.Expiration);
            warningsByKey.TryGetValue(key, out var warning);

            var expirationDate = item.ExpirationDate;
            var applies = item.Status == PantryItemStatus.Active
                          && expirationDate.HasValue
                          && expirationDate.Value <= limit;

            if (applies)
            {
                var date = expirationDate!.Value;
                var severity = date < today
                    ? ExpirationSeverity.Expired
                    : ExpirationSeverity.ExpiringSoon;

                if (warning == null)
                {
                    await _warningRepository.InsertAsync(new ExpirationWarning(
                        GuidGenerator.Create(), item.Id, item.OwnerUserId, date, severity));
                    created++;
                }
                else if (!warning.IsActive
                         || warning.ExpirationDate != date
                         || warning.Severity != severity)
                {
                    warning.Refresh(date, severity);
                    await _warningRepository.UpdateAsync(warning);
                    updated++;
                }
            }
            else if (warning is { IsActive: true })
            {
                warning.Deactivate();
                await _warningRepository.UpdateAsync(warning);
                deactivated++;
            }
        }

        return new ExpirationProcessingResult(created, updated, deactivated);
    }
}