namespace MainLedger.Domain.Entities;

/// <summary>
/// Represents a global category for financial records.
/// Categories help organize transactions (e.g., Groceries, Gasoline, Transport).
/// Categories are shared across all users.
/// </summary>
public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    private Category() { }

    private Category(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new global category.
    /// </summary>
    public static Category Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(name));

        if (description?.Length > 500)
            throw new ArgumentException(
                "Category description cannot exceed 500 characters",
                nameof(description)
            );

        return new Category(name.Trim(), description?.Trim());
    }

    /// <summary>
    /// Updates the category name and description.
    /// </summary>
    public void Update(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name cannot exceed 100 characters", nameof(name));

        if (description?.Length > 500)
            throw new ArgumentException(
                "Category description cannot exceed 500 characters",
                nameof(description)
            );

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
