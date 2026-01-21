using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

/// <summary>
/// Repository interface for Category entity operations.
/// Categories are global and shared across all users.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category by name (case-insensitive).
    /// </summary>
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category by name (case-insensitive).
    /// </summary>
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new category.
    /// </summary>
    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    void Update(Category category);

    /// <summary>
    /// Deletes a category.
    /// </summary>
    void Delete(Category category);

    /// <summary>
    /// Checks if a category name already exists (case-insensitive).
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
