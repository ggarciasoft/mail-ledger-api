using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MainLedger.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly MailLedgerDbContext _context;

    public CategoryRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Category>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Categories.ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Categories.FirstOrDefaultAsync(
            c => c.Name.ToLower() == name.ToLower(),
            cancellationToken
        );
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
    }

    public void Update(Category category)
    {
        _context.Categories.Update(category);
    }

    public void Delete(Category category)
    {
        _context.Categories.Remove(category);
    }

    public async Task<bool> ExistsAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.Categories.AnyAsync(
            c => c.Name.ToLower() == name.ToLower(),
            cancellationToken
        );
    }
}
