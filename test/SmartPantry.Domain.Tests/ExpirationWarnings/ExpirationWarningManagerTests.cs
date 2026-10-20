using System;
using System.Threading.Tasks;
using Shouldly;
using SmartPantry.PantryItems;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace SmartPantry.ExpirationWarnings;

public class ExpirationWarningManagerTests : SmartPantryDomainTestBase<SmartPantryDomainTestModule>
{
    // Fecha fija (criterio UTC): las pruebas no dependen del reloj.
    private static readonly DateOnly Today = new(2026, 10, 16);
    private static readonly Guid Owner = Guid.NewGuid();

    private readonly ExpirationWarningManager _manager;
    private readonly IRepository<PantryItem, Guid> _items;
    private readonly IRepository<ExpirationWarning, Guid> _warnings;

    public ExpirationWarningManagerTests()
    {
        _manager = GetRequiredService<ExpirationWarningManager>();
        _items = GetRequiredService<IRepository<PantryItem, Guid>>();
        _warnings = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
    }

    private async Task<PantryItem> AddItemAsync(DateOnly? expirationDate, bool consumed = false)
    {
        var item = new PantryItem(Guid.NewGuid(), Owner, Guid.NewGuid(), expirationDate);
        if (consumed)
        {
            item.MarkAsConsumed();
        }

        return await WithUnitOfWorkAsync(() => _items.InsertAsync(item, autoSave: true));
    }

    private Task<ExpirationProcessingResult> ProcessAsync() =>
        WithUnitOfWorkAsync(() => _manager.ProcessAsync(Today));

    private Task<ExpirationWarning?> GetWarningAsync(Guid pantryItemId) =>
        WithUnitOfWorkAsync(async () => await _warnings.FindAsync(w => w.PantryItemId == pantryItemId));

    private Task UpdateItemAsync(Guid itemId, Action<PantryItem> change) =>
        WithUnitOfWorkAsync(async () =>
        {
            var item = await _items.GetAsync(itemId);
            change(item);
            await _items.UpdateAsync(item, autoSave: true);
        });

    [Fact]
    public async Task Debe_crear_advertencia_dentro_del_umbral()
    {
        var item = await AddItemAsync(Today.AddDays(1));

        var result = await ProcessAsync();

        result.Created.ShouldBe(1);
        var warning = await GetWarningAsync(item.Id);
        warning.ShouldNotBeNull();
        warning.IsActive.ShouldBeTrue();
        warning.Severity.ShouldBe(ExpirationSeverity.ExpiringSoon);
        warning.OwnerUserId.ShouldBe(Owner);
    }

    [Fact]
    public async Task Debe_crear_advertencia_el_dia_limite()
    {
        var item = await AddItemAsync(Today.AddDays(ExpirationWarningConsts.DefaultThresholdDays));

        await ProcessAsync();

        var warning = await GetWarningAsync(item.Id);
        warning.ShouldNotBeNull();
        warning.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task No_debe_crear_advertencia_fuera_del_umbral()
    {
        var item = await AddItemAsync(Today.AddDays(ExpirationWarningConsts.DefaultThresholdDays + 1));

        await ProcessAsync();

        (await GetWarningAsync(item.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Item_vencido_debe_tener_severidad_Expired()
    {
        var item = await AddItemAsync(Today.AddDays(-1));

        await ProcessAsync();

        var warning = await GetWarningAsync(item.Id);
        warning.ShouldNotBeNull();
        warning.Severity.ShouldBe(ExpirationSeverity.Expired);
    }

    [Fact]
    public async Task Item_sin_fecha_no_debe_generar_advertencia()
    {
        var item = await AddItemAsync(null);

        await ProcessAsync();

        (await GetWarningAsync(item.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Item_consumido_no_debe_generar_advertencia()
    {
        var item = await AddItemAsync(Today.AddDays(1), consumed: true);

        await ProcessAsync();

        (await GetWarningAsync(item.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task Ejecutar_dos_veces_no_debe_duplicar_la_advertencia()
    {
        var item = await AddItemAsync(Today.AddDays(1));

        await ProcessAsync();
        var second = await ProcessAsync();

        second.Created.ShouldBe(0);
        second.Updated.ShouldBe(0);
        second.Deactivated.ShouldBe(0);

        var count = await WithUnitOfWorkAsync(() => _warnings.CountAsync(w => w.PantryItemId == item.Id));
        count.ShouldBe(1);
    }

    [Fact]
    public async Task Cambiar_la_fecha_debe_actualizar_desactivar_y_reactivar_la_misma_advertencia()
    {
        var item = await AddItemAsync(Today.AddDays(1));
        await ProcessAsync();
        var warningId = (await GetWarningAsync(item.Id))!.Id;

        // La fecha cambia pero sigue dentro del umbral: se actualiza.
        await UpdateItemAsync(item.Id, i => i.SetExpirationDate(Today.AddDays(2)));
        var updated = await ProcessAsync();
        updated.Updated.ShouldBe(1);
        (await GetWarningAsync(item.Id))!.ExpirationDate.ShouldBe(Today.AddDays(2));

        // La fecha pasa a estar lejos: se desactiva.
        await UpdateItemAsync(item.Id, i => i.SetExpirationDate(Today.AddDays(30)));
        var deactivated = await ProcessAsync();
        deactivated.Deactivated.ShouldBe(1);
        (await GetWarningAsync(item.Id))!.IsActive.ShouldBeFalse();

        // La fecha vuelve a estar cerca: se reactiva la MISMA fila.
        await UpdateItemAsync(item.Id, i => i.SetExpirationDate(Today.AddDays(1)));
        await ProcessAsync();
        var reactivated = (await GetWarningAsync(item.Id))!;
        reactivated.IsActive.ShouldBeTrue();
        reactivated.Id.ShouldBe(warningId);
    }

    [Fact]
    public async Task Consumir_un_item_debe_desactivar_su_advertencia()
    {
        var item = await AddItemAsync(Today.AddDays(1));
        await ProcessAsync();

        await UpdateItemAsync(item.Id, i => i.MarkAsConsumed());
        var result = await ProcessAsync();

        result.Deactivated.ShouldBe(1);
        (await GetWarningAsync(item.Id))!.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task El_indice_unico_debe_impedir_dos_advertencias_para_el_mismo_item()
    {
        var item = await AddItemAsync(Today.AddDays(1));

        await WithUnitOfWorkAsync(() => _warnings.InsertAsync(
            new ExpirationWarning(Guid.NewGuid(), item.Id, Owner, Today, ExpirationSeverity.ExpiringSoon),
            autoSave: true));

        await Should.ThrowAsync<Exception>(() => WithUnitOfWorkAsync(() => _warnings.InsertAsync(
            new ExpirationWarning(Guid.NewGuid(), item.Id, Owner, Today, ExpirationSeverity.ExpiringSoon),
            autoSave: true)));
    }
}