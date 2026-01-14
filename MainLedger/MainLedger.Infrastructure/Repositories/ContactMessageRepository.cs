using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MainLedger.Infrastructure.Persistence;

namespace MainLedger.Infrastructure.Repositories;

public class ContactMessageRepository : IContactMessageRepository
{
    private readonly MailLedgerDbContext _context;

    public ContactMessageRepository(MailLedgerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        ContactMessage contactMessage,
        CancellationToken cancellationToken = default
    )
    {
        await _context.ContactMessages.AddAsync(contactMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
