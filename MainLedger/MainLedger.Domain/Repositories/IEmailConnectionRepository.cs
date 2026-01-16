using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;

namespace MainLedger.Domain.Repositories;

public interface IEmailConnectionRepository
{
    Task<EmailConnection?> GetByIdAsync(Guid id);
    Task<EmailConnection?> GetByUserAndProviderAsync(Guid userId, EmailProvider provider);
    Task<List<EmailConnection>> GetByUserIdAsync(Guid userId);
    Task<EmailConnection> AddAsync(EmailConnection connection);
    Task UpdateAsync(EmailConnection connection);
    Task DeleteAsync(Guid id);
}
