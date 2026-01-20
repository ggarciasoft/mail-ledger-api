using MainLedger.Domain.Entities;
using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.ExtractionCandidates.Commands;

public class ConfirmExtractionCandidateCommandHandler
    : IRequestHandler<ConfirmExtractionCandidateCommand, Guid>
{
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IFinancialRecordRepository _financialRecordRepository;
    private readonly IExtractionVersionRepository _versionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ConfirmExtractionCandidateCommandHandler> _logger;

    public ConfirmExtractionCandidateCommandHandler(
        IExtractionCandidateRepository candidateRepository,
        IEmailMessageRepository emailRepository,
        IFinancialRecordRepository financialRecordRepository,
        IExtractionVersionRepository versionRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<ConfirmExtractionCandidateCommandHandler> logger
    )
    {
        _candidateRepository = candidateRepository;
        _emailRepository = emailRepository;
        _financialRecordRepository = financialRecordRepository;
        _versionRepository = versionRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> Handle(
        ConfirmExtractionCandidateCommand request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Confirming extraction candidate {CandidateId} for user {UserId}",
            request.CandidateId,
            request.UserId
        );

        // Get candidate
        var candidate = await _candidateRepository.GetByIdAsync(
            request.CandidateId,
            cancellationToken
        );
        if (candidate == null)
        {
            throw new KeyNotFoundException($"Extraction candidate {request.CandidateId} not found");
        }

        // Verify user authorization
        var email = await _emailRepository.GetByIdAsync(
            candidate.EmailMessageId,
            cancellationToken
        );
        if (email == null || email.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException(
                $"User {request.UserId} does not have access to candidate {request.CandidateId}"
            );
        }

        // Verify status is Pending
        if (candidate.Status != RecordStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot confirm candidate with status {candidate.Status}. Only Pending candidates can be confirmed."
            );
        }

        // Confirm the candidate
        candidate.Confirm();

        // Get extraction version
        var version = await _versionRepository.GetByIdAsync(
            candidate.ExtractionVersionId,
            cancellationToken
        );
        if (version == null)
        {
            throw new InvalidOperationException(
                $"Extraction version {candidate.ExtractionVersionId} not found"
            );
        }

        // Create financial record from candidate
        if (candidate.Amount == null)
        {
            throw new InvalidOperationException("Cannot create financial record without amount");
        }

        // Calculate overall confidence
        var confidenceValue = CalculateOverallConfidence(candidate);
        var confidence = Confidence.Create(confidenceValue);

        // Determine transaction type and direction (simplified logic)
        var transactionType = DetermineTransactionType(candidate);
        var direction = DetermineDirection(candidate);

        // Handle category auto-creation
        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            // Check if category exists (case-insensitive)
            var existingCategory = await _categoryRepository.GetByNameAsync(
                request.Category,
                cancellationToken
            );

            if (existingCategory != null)
            {
                categoryId = existingCategory.Id;
                _logger.LogDebug(
                    "Using existing category {CategoryId} '{CategoryName}'",
                    existingCategory.Id,
                    existingCategory.Name
                );
            }
            else
            {
                // Create new global category
                var newCategory = Category.Create(request.Category);
                await _categoryRepository.AddAsync(newCategory, cancellationToken);
                categoryId = newCategory.Id;

                _logger.LogInformation(
                    "Created new category {CategoryId} '{CategoryName}'",
                    newCategory.Id,
                    newCategory.Name
                );
            }

            // Set category on candidate
            candidate.SetCategory(categoryId);
        }

        var financialRecord = FinancialRecord.Create(
            userId: request.UserId,
            emailMessageId: candidate.EmailMessageId,
            type: transactionType,
            amount: candidate.Amount,
            direction: direction,
            confidence: confidence,
            extractionVersion: version.Version,
            transactionDate: candidate.TransactionDate ?? email.ReceivedAt,
            merchant: request.Merchant ?? candidate.Merchant, // Use request merchant if provided
            sourceAccount: candidate.SourceAccount,
            sourceBank: candidate.SourceBank,
            targetAccount: candidate.TargetAccount,
            targetBank: candidate.TargetBank,
            taxAmount: candidate.Tax,
            feeAmount: candidate.Fees,
            categoryId: categoryId
        );

        // Confirm the financial record immediately since user confirmed it
        financialRecord.Confirm();

        // Save both
        await _financialRecordRepository.AddAsync(financialRecord, cancellationToken);
        _candidateRepository.Update(candidate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created financial record {RecordId} from candidate {CandidateId}",
            financialRecord.Id,
            request.CandidateId
        );

        return financialRecord.Id;
    }

    private double CalculateOverallConfidence(ExtractionCandidate candidate)
    {
        var confidences = new List<double>();

        if (candidate.AmountConfidence != null)
            confidences.Add(candidate.AmountConfidence.Value);
        if (candidate.DateConfidence != null)
            confidences.Add(candidate.DateConfidence.Value);
        if (candidate.MerchantConfidence != null)
            confidences.Add(candidate.MerchantConfidence.Value);

        return confidences.Any() ? confidences.Average() : 0.5;
    }

    private TransactionType DetermineTransactionType(ExtractionCandidate candidate)
    {
        // Simplified logic - can be enhanced based on email category or other factors
        if (!string.IsNullOrWhiteSpace(candidate.Merchant))
            return TransactionType.Payment;

        if (candidate.SourceAccount != null && candidate.TargetAccount != null)
            return TransactionType.Transfer;

        return TransactionType.Payment;
    }

    private TransactionDirection DetermineDirection(ExtractionCandidate candidate)
    {
        // Simplified logic - would need more context to determine accurately
        // For now, assume payments are outgoing
        return TransactionDirection.Out;
    }
}
