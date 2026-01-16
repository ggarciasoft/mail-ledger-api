using MainLedger.Domain.Enums;
using MainLedger.Domain.Services;

namespace MainLedger.Application.Common.Interfaces;

public interface IEmailProviderFactory
{
    IEmailProvider GetProvider(EmailProvider provider);
    IEnumerable<IEmailProvider> GetAllProviders();
}
