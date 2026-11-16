using Volo.Abp.Application.Dtos;

namespace SmartPantry.PantryItems;

public class GetPantryItemListInput : PagedAndSortedResultRequestDto
{
    /// <summary>Si es true, el listado incluye los ítems ya consumidos (historial).</summary>
    public bool IncludeConsumed { get; set; }
}