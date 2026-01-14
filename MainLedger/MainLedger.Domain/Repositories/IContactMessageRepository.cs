using MainLedger.Domain.Entities;

namespace MainLedger.Domain.Repositories;

public interface IContactMessageRepository
{
    Task AddAsync(ContactMessage contactMessage, CancellationToken cancellationToken = default);
}
