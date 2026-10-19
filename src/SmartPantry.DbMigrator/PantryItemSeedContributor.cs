using System;
using System.Threading.Tasks;
using SmartPantry.PantryItems;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace SmartPantry.DbMigrator;

/// <summary>
/// Datos de ejemplo SOLO para desarrollo local: se ejecutan con el DbMigrator, no en las pruebas.
/// </summary>
public class PantryItemSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IGuidGenerator _guidGenerator;

    public PantryItemSeedContributor(
        IRepository<PantryItem, Guid> pantryItemRepository,
        IGuidGenerator guidGenerator)
    {
        _pantryItemRepository = pantryItemRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // Idempotente: si ya hay ítems, no vuelve a cargar nada.
        if (await _pantryItemRepository.GetCountAsync() > 0)
        {
            return;
        }

        var owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var productId = _guidGenerator.Create();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var consumed = new PantryItem(_guidGenerator.Create(), owner, productId, today.AddDays(1));
        consumed.MarkAsConsumed();

        var items = new[]
        {
            new PantryItem(_guidGenerator.Create(), owner, productId, today.AddDays(2)),   // por vencer
            new PantryItem(_guidGenerator.Create(), owner, productId, today.AddDays(3)),   // día límite
            new PantryItem(_guidGenerator.Create(), owner, productId, today.AddDays(-1)),  // vencido
            new PantryItem(_guidGenerator.Create(), owner, productId, today.AddDays(20)),  // lejos
            new PantryItem(_guidGenerator.Create(), owner, productId, null),               // sin fecha
            consumed                                                                       // consumido
        };

        await _pantryItemRepository.InsertManyAsync(items, autoSave: true);
    }
}