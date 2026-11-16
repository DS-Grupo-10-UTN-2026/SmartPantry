using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SmartPantry.ExpirationWarnings;
using SmartPantry.Products;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace SmartPantry.PantryItems;

[Authorize]
public class PantryItemAppService : SmartPantryAppService, IPantryItemAppService
{
    private readonly IRepository<PantryItem, Guid> _pantryItemRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ExpirationWarning, Guid> _warningRepository;

    public PantryItemAppService(
        IRepository<PantryItem, Guid> pantryItemRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ExpirationWarning, Guid> warningRepository)
    {
        _pantryItemRepository = pantryItemRepository;
        _productRepository = productRepository;
        _warningRepository = warningRepository;
    }

    public async Task<PantryItemDto> CreateAsync(CreatePantryItemDto input)
    {
        var product = await _productRepository.FindAsync(input.ProductId);
        if (product == null)
        {
            throw new EntityNotFoundException(typeof(Product), input.ProductId);
        }

        // El propietario siempre es el usuario autenticado; nunca lo envía el cliente.
        var item = new PantryItem(
            GuidGenerator.Create(),
            CurrentUser.GetId(),
            input.ProductId,
            input.ExpirationDate,
            input.Quantity,
            input.Unit);

        await _pantryItemRepository.InsertAsync(item, autoSave: true);

        return ToDto(item, product.Name);
    }

    public async Task<PagedResultDto<PantryItemDto>> GetListAsync(GetPantryItemListInput input)
    {
        var userId = CurrentUser.GetId();
        var items = await _pantryItemRepository.GetQueryableAsync();
        var products = await _productRepository.GetQueryableAsync();

        // LEFT JOIN: si el producto no está disponible, el nombre queda en null (RF-09).
        var query =
            from item in items
            where item.OwnerUserId == userId
            join product in products on item.ProductId equals product.Id into productGroup
            from product in productGroup.DefaultIfEmpty()
            select new PantryItemRow
            {
                Item = item,
                ProductName = product != null ? product.Name : null
            };

        if (!input.IncludeConsumed)
        {
            query = query.Where(x => x.Item.Status == PantryItemStatus.Active);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var rows = await AsyncExecuter.ToListAsync(
            ApplySorting(query, input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount));

        return new PagedResultDto<PantryItemDto>(
            totalCount,
            rows.Select(x => ToDto(x.Item, x.ProductName)).ToList());
    }

    public async Task<PantryItemDto> GetAsync(Guid id)
    {
        var item = await GetOwnedAsync(id);
        return ToDto(item, await FindProductNameAsync(item.ProductId));
    }

    public async Task<PantryItemDto> UpdateAsync(Guid id, UpdatePantryItemDto input)
    {
        var item = await GetOwnedAsync(id);

        // Primero lo que puede fallar, para no dejar cambios parciales.
        item.SetQuantity(input.Quantity, input.Unit);
        item.SetExpirationDate(input.ExpirationDate);

        await _pantryItemRepository.UpdateAsync(item, autoSave: true);

        return ToDto(item, await FindProductNameAsync(item.ProductId));
    }

    public async Task<PantryItemDto> MarkAsConsumedAsync(Guid id)
    {
        var item = await GetOwnedAsync(id);

        item.MarkAsConsumed();
        await _pantryItemRepository.UpdateAsync(item, autoSave: true);

        return ToDto(item, await FindProductNameAsync(item.ProductId));
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await GetOwnedAsync(id);

        // Se eliminan también sus advertencias, para que no quede una activa huérfana.
        await _warningRepository.DeleteAsync(w => w.PantryItemId == item.Id);
        await _pantryItemRepository.DeleteAsync(item);
    }

    /// <summary>
    /// Busca el ítem por Id y propietario. Un ítem inexistente o de otro usuario
    /// responde igual (404), para no revelar que existe.
    /// </summary>
    private async Task<PantryItem> GetOwnedAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var item = await _pantryItemRepository.FindAsync(x => x.Id == id && x.OwnerUserId == userId);
        if (item == null)
        {
            throw new EntityNotFoundException(typeof(PantryItem), id);
        }

        return item;
    }

    private async Task<string?> FindProductNameAsync(Guid productId)
    {
        var product = await _productRepository.FindAsync(productId);
        return product?.Name;
    }

    private static IOrderedQueryable<PantryItemRow> ApplySorting(IQueryable<PantryItemRow> query, string? sorting)
    {
        var parts = (sorting ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var field = parts.Length > 0 ? parts[0].ToLowerInvariant() : "expirationdate";
        var descending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        // Lista cerrada de campos: un texto cualquiera nunca se usa para armar la consulta.
        var ordered = field switch
        {
            "productname" => descending
                ? query.OrderByDescending(x => x.ProductName)
                : query.OrderBy(x => x.ProductName),
            "quantity" => descending
                ? query.OrderByDescending(x => x.Item.Quantity)
                : query.OrderBy(x => x.Item.Quantity),
            "unit" => descending
                ? query.OrderByDescending(x => x.Item.Unit)
                : query.OrderBy(x => x.Item.Unit),
            // Por defecto: fecha de vencimiento, con los ítems sin fecha al final.
            _ => descending
                ? query.OrderBy(x => x.Item.ExpirationDate == null).ThenByDescending(x => x.Item.ExpirationDate)
                : query.OrderBy(x => x.Item.ExpirationDate == null).ThenBy(x => x.Item.ExpirationDate)
        };

        // Desempate estable para que la paginación sea determinista.
        return ordered.ThenBy(x => x.Item.Id);
    }

    private static PantryItemDto ToDto(PantryItem item, string? productName)
    {
        return new PantryItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = productName,
            Status = item.Status,
            ExpirationDate = item.ExpirationDate,
            Quantity = item.Quantity,
            Unit = item.Unit
        };
    }

    private sealed class PantryItemRow
    {
        public PantryItem Item { get; init; } = default!;
        public string? ProductName { get; init; }
    }
}