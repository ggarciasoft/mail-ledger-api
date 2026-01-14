using MediatR;

namespace MainLedger.Application.Contact.Commands.SubmitContactMessage;

public record SubmitContactMessageCommand(
    string FirstName,
    string LastName,
    string Email,
    string Subject,
    string Message
) : IRequest<Unit>;
