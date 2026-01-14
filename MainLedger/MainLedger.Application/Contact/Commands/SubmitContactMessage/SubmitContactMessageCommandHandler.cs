using MainLedger.Domain.Entities;
using MainLedger.Domain.Repositories;
using MediatR;

namespace MainLedger.Application.Contact.Commands.SubmitContactMessage;

public class SubmitContactMessageCommandHandler : IRequestHandler<SubmitContactMessageCommand, Unit>
{
    private readonly IContactMessageRepository _contactMessageRepository;

    public SubmitContactMessageCommandHandler(IContactMessageRepository contactMessageRepository)
    {
        _contactMessageRepository = contactMessageRepository;
    }

    public async Task<Unit> Handle(
        SubmitContactMessageCommand request,
        CancellationToken cancellationToken
    )
    {
        // Create contact message entity
        var contactMessage = ContactMessage.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Subject,
            request.Message
        );

        // Add to database via repository
        await _contactMessageRepository.AddAsync(contactMessage, cancellationToken);

        return Unit.Value;
    }
}
