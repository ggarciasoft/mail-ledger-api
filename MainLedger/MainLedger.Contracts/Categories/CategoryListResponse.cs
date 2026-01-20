namespace MainLedger.Contracts.Categories;

/// <summary>
/// Response containing a list of categories.
/// </summary>
public record CategoryListResponse
{
    public List<CategoryDto> Categories { get; init; } = new();
}
