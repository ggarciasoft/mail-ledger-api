using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Services;

namespace MainLedger.Application.Services;

public class EmailProviderFactory : IEmailProviderFactory
{
    private readonly IEnumerable<IEmailProvider> _providers;

    public EmailProviderFactory(IEnumerable<IEmailProvider> providers)
    {
        _providers = providers;
    }

    public IEmailProvider GetProvider(EmailProvider provider)
    {
        return _providers.FirstOrDefault(p => p.ProviderType == provider)
            ?? throw new InvalidOperationException($"Email provider {provider} is not registered");
    }

    public IEnumerable<IEmailProvider> GetAllProviders()
    {
        return _providers;
    }
}
