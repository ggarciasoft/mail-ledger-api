using MainLedger.Application.Common.Interfaces;
using MainLedger.Application.Common.Models;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.Emails.Commands;

/// <summary>
/// Command to classify a single email using AI.
/// </summary>
public record ClassifyEmailCommand(Guid EmailId) : IRequest<ClassificationResult>;

public class ClassifyEmailCommandHandler : IRequestHandler<ClassifyEmailCommand, ClassificationResult>
{
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IClassificationService _classificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClassifyEmailCommandHandler> _logger;

    public ClassifyEmailCommandHandler(
        IEmailMessageRepository emailRepository,
        IClassificationService classificationService,
        IUnitOfWork unitOfWork,
        ILogger<ClassifyEmailCommandHandler> logger)
    {
        _emailRepository = emailRepository;
        _classificationService = classificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ClassificationResult> Handle(ClassifyEmailCommand request, CancellationToken cancellationToken)
    {
        var email = await _emailRepository.GetByIdAsync(request.EmailId, cancellationToken);
        
        if (email == null)
        {
            throw new KeyNotFoundException($"Email not found: {request.EmailId}");
        }

        _logger.LogInformation("Classifying email {EmailId}", request.EmailId);

        // Call classification service
        var result = await _classificationService.ClassifyEmailAsync(email, cancellationToken);

        // Update email with classification
        email.SetClassification(result.IsFinancial, result.Category, result.Confidence);

        _emailRepository.Update(email);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Email {EmailId} classified: IsFinancial={IsFinancial}, Category={Category}",
            request.EmailId, result.IsFinancial, result.Category);

        return result;
    }
}
