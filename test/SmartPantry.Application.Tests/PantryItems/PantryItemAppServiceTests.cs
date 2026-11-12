using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Shouldly;
using SmartPantry.ExpirationWarnings;
using SmartPantry.Products;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Volo.Abp.Validation;
using Xunit;

namespace SmartPantry.PantryItems;

public class PantryItemAppServiceTests : SmartPantryApplicationTestBase<SmartPantryApplicationTestModule>
{
    // Fecha fija (criterio UTC): las pruebas no dependen del reloj.
    private static readonly DateOnly Today = new(2026, 10, 28);

    private readonly IPantryItemAppService _service;
    private readonly IProductAppService _products;
    private readonly ICurrentPrincipalAccessor _principalAccessor;
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private int _barcodeCounter;

    public PantryItemAppServiceTests()
    {
        _service = GetRequiredService<IPantryItemAppService>();
        _products = GetRequiredService<IProductAppService>();
        _principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    // Simula un usuario autenticado mientras dure el bloque using.
    private IDisposable LoginAs(Guid userId) =>
        _principalAccessor.Change(new Claim(AbpClaimTypes.UserId, userId.ToString()));

    private async Task<ProductDto> CreateProductAsync(string name = "Arroz largo fino 1kg")
    {
        _barcodeCounter++;
        return await _products.CreateAsync(new CreateProductDto
        {
            Barcode = $"7790000{_barcodeCounter:D6}",
            Name = name
        });
    }

    private async Task<PantryItemDto> CreateItemAsync(
        Guid userId,
        Guid productId,
        DateOnly? expirationDate = null,
        decimal? quantity = null,
        string? unit = null)
    {
        using (LoginAs(userId))
        {
            return await _service.CreateAsync(new CreatePantryItemDto
            {
                ProductId = productId,
                ExpirationDate = expirationDate,
                Quantity = quantity,
                Unit = unit
            });
        }
    }

    [Fact]
    public async Task Should_Create_And_Get_A_PantryItem_With_Normalized_Unit()
    {
        var product = await CreateProductAsync("Arroz largo fino 1kg");

        var created = await CreateItemAsync(_userA, product.Id, Today.AddDays(5), 2.5m, "  kg ");

        created.Id.ShouldNotBe(Guid.Empty);
        created.ProductName.ShouldBe("Arroz largo fino 1kg");
        created.Status.ShouldBe(PantryItemStatus.Active);
        created.Unit.ShouldBe("kg");

        using (LoginAs(_userA))
        {
            var fetched = await _service.GetAsync(created.Id);

            fetched.Id.ShouldBe(created.Id);
            fetched.ProductName.ShouldBe("Arroz largo fino 1kg");
            fetched.Quantity.ShouldBe(2.5m);
            fetched.Unit.ShouldBe("kg");
            fetched.ExpirationDate.ShouldBe(Today.AddDays(5));
        }
    }

    [Fact]
    public async Task Should_Not_Create_A_PantryItem_For_An_Unknown_Product()
    {
        await Should.ThrowAsync<EntityNotFoundException>(() => CreateItemAsync(_userA, Guid.NewGuid()));
    }

    [Fact]
    public async Task Should_List_Only_The_Items_Of_The_Current_User()
    {
        var product = await CreateProductAsync();
        var a1 = await CreateItemAsync(_userA, product.Id);
        var a2 = await CreateItemAsync(_userA, product.Id);
        var b1 = await CreateItemAsync(_userB, product.Id);

        using (LoginAs(_userA))
        {
            var result = await _service.GetListAsync(new GetPantryItemListInput());

            result.TotalCount.ShouldBe(2);
            result.Items.Select(x => x.Id).ShouldBe(new[] { a1.Id, a2.Id }, ignoreOrder: true);
        }

        using (LoginAs(_userB))
        {
            var result = await _service.GetListAsync(new GetPantryItemListInput());

            result.TotalCount.ShouldBe(1);
            result.Items.Single().Id.ShouldBe(b1.Id);
        }
    }

    [Fact]
    public async Task Should_Paginate_The_List()
    {
        var product = await CreateProductAsync();
        await CreateItemAsync(_userA, product.Id, Today.AddDays(1));
        await CreateItemAsync(_userA, product.Id, Today.AddDays(2));
        await CreateItemAsync(_userA, product.Id, Today.AddDays(3));

        using (LoginAs(_userA))
        {
            var page1 = await _service.GetListAsync(new GetPantryItemListInput { MaxResultCount = 2, SkipCount = 0 });
            var page2 = await _service.GetListAsync(new GetPantryItemListInput { MaxResultCount = 2, SkipCount = 2 });

            page1.TotalCount.ShouldBe(3);
            page1.Items.Count.ShouldBe(2);
            page2.TotalCount.ShouldBe(3);
            page2.Items.Count.ShouldBe(1);

            page1.Items.Concat(page2.Items).Select(x => x.Id).Distinct().Count().ShouldBe(3);
        }
    }

    [Fact]
    public async Task Should_Exclude_Consumed_Items_Unless_Requested()
    {
        var product = await CreateProductAsync();
        var kept = await CreateItemAsync(_userA, product.Id);
        var consumed = await CreateItemAsync(_userA, product.Id);

        using (LoginAs(_userA))
        {
            await _service.MarkAsConsumedAsync(consumed.Id);

            var active = await _service.GetListAsync(new GetPantryItemListInput());
            active.TotalCount.ShouldBe(1);
            active.Items.Single().Id.ShouldBe(kept.Id);

            var all = await _service.GetListAsync(new GetPantryItemListInput { IncludeConsumed = true });
            all.TotalCount.ShouldBe(2);
            all.Items.Single(x => x.Id == consumed.Id).Status.ShouldBe(PantryItemStatus.Consumed);
        }
    }

    [Fact]
    public async Task Should_Sort_By_Expiration_Date_With_Undated_Items_Last()
    {
        var product = await CreateProductAsync();
        var late = await CreateItemAsync(_userA, product.Id, Today.AddDays(9));
        var undated = await CreateItemAsync(_userA, product.Id);
        var early = await CreateItemAsync(_userA, product.Id, Today.AddDays(1));

        using (LoginAs(_userA))
        {
            var ascending = await _service.GetListAsync(new GetPantryItemListInput());
            ascending.Items.Select(x => x.Id).ShouldBe(new[] { early.Id, late.Id, undated.Id });

            var descending = await _service.GetListAsync(new GetPantryItemListInput { Sorting = "ExpirationDate desc" });
            descending.Items.Select(x => x.Id).ShouldBe(new[] { late.Id, early.Id, undated.Id });
        }
    }

    [Fact]
    public async Task Should_Return_Null_ProductName_When_The_Product_Is_Not_Available()
    {
        var product = await CreateProductAsync("Producto a eliminar");
        var item = await CreateItemAsync(_userA, product.Id);

        await _products.DeleteAsync(product.Id);

        using (LoginAs(_userA))
        {
            var fetched = await _service.GetAsync(item.Id);
            fetched.ProductName.ShouldBeNull();

            var list = await _service.GetListAsync(new GetPantryItemListInput());
            list.Items.Single().ProductName.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Should_Update_Quantity_Unit_And_Expiration_Date()
    {
        var product = await CreateProductAsync();
        var item = await CreateItemAsync(_userA, product.Id, Today.AddDays(5), 1m, "kg");

        using (LoginAs(_userA))
        {
            var updated = await _service.UpdateAsync(item.Id, new UpdatePantryItemDto
            {
                ExpirationDate = Today.AddDays(9),
                Quantity = 3m,
                Unit = "  g "
            });

            updated.Quantity.ShouldBe(3m);
            updated.Unit.ShouldBe("g");
            updated.ExpirationDate.ShouldBe(Today.AddDays(9));

            var fetched = await _service.GetAsync(item.Id);
            fetched.Quantity.ShouldBe(3m);
            fetched.Unit.ShouldBe("g");
            fetched.ExpirationDate.ShouldBe(Today.AddDays(9));
        }
    }

    [Fact]
    public async Task Should_Reject_An_Invalid_Update_Without_Changing_The_Item()
    {
        var product = await CreateProductAsync();
        var item = await CreateItemAsync(_userA, product.Id, Today.AddDays(5), 1m, "kg");

        using (LoginAs(_userA))
        {
            await Should.ThrowAsync<AbpValidationException>(() => _service.UpdateAsync(item.Id, new UpdatePantryItemDto
            {
                ExpirationDate = Today.AddDays(30),
                Quantity = -1m,
                Unit = "g"
            }));

            await Should.ThrowAsync<AbpValidationException>(() => _service.UpdateAsync(item.Id, new UpdatePantryItemDto
            {
                ExpirationDate = Today.AddDays(30),
                Quantity = 1m,
                Unit = new string('x', PantryItemConsts.MaxUnitLength + 1)
            }));

            var unchanged = await _service.GetAsync(item.Id);
            unchanged.Quantity.ShouldBe(1m);
            unchanged.Unit.ShouldBe("kg");
            unchanged.ExpirationDate.ShouldBe(Today.AddDays(5));
        }
    }

    [Fact]
    public async Task Should_Mark_An_Item_As_Consumed()
    {
        var product = await CreateProductAsync();
        var item = await CreateItemAsync(_userA, product.Id, Today.AddDays(2));

        using (LoginAs(_userA))
        {
            var consumed = await _service.MarkAsConsumedAsync(item.Id);
            consumed.Status.ShouldBe(PantryItemStatus.Consumed);

            var fetched = await _service.GetAsync(item.Id);
            fetched.Status.ShouldBe(PantryItemStatus.Consumed);
        }
    }

    [Fact]
    public async Task Should_Delete_An_Item_And_Its_Expiration_Warnings()
    {
        var product = await CreateProductAsync();
        var item = await CreateItemAsync(_userA, product.Id, Today.AddDays(1));

        var manager = GetRequiredService<ExpirationWarningManager>();
        var warnings = GetRequiredService<IRepository<ExpirationWarning, Guid>>();

        // El worker (su servicio) genera la advertencia del ítem.
        await WithUnitOfWorkAsync(() => manager.ProcessAsync(Today));
        var before = await WithUnitOfWorkAsync(() => warnings.CountAsync(w => w.PantryItemId == item.Id));
        before.ShouldBe(1);

        using (LoginAs(_userA))
        {
            await _service.DeleteAsync(item.Id);

            await Should.ThrowAsync<EntityNotFoundException>(() => _service.GetAsync(item.Id));
        }

        var after = await WithUnitOfWorkAsync(() => warnings.CountAsync(w => w.PantryItemId == item.Id));
        after.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Let_Another_User_Read_Update_Consume_Or_Delete_An_Item()
    {
        var product = await CreateProductAsync();
        var item = await CreateItemAsync(_userA, product.Id, Today.AddDays(5), 1m, "kg");

        using (LoginAs(_userB))
        {
            // Un ítem ajeno responde igual que uno inexistente (404).
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.GetAsync(item.Id));

            await Should.ThrowAsync<EntityNotFoundException>(() =>
                _service.UpdateAsync(item.Id, new UpdatePantryItemDto { Quantity = 99m }));

            await Should.ThrowAsync<EntityNotFoundException>(() => _service.MarkAsConsumedAsync(item.Id));

            await Should.ThrowAsync<EntityNotFoundException>(() => _service.DeleteAsync(item.Id));
        }

        // El dueño conserva su ítem sin cambios.
        using (LoginAs(_userA))
        {
            var unchanged = await _service.GetAsync(item.Id);
            unchanged.Status.ShouldBe(PantryItemStatus.Active);
            unchanged.Quantity.ShouldBe(1m);
            unchanged.Unit.ShouldBe("kg");
        }
    }
}