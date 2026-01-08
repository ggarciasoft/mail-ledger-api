using MainLedger.Domain.Enums;
using MainLedger.Domain.Repositories;
using MainLedger.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.ExtractionCandidates.Commands;

public class UpdateExtractionCandidateCommandHandler : IRequestHandler<UpdateExtractionCandidateCommand, Unit>
{
    private readonly IExtractionCandidateRepository _candidateRepository;
    private readonly IEmailMessageRepository _emailRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateExtractionCandidateCommandHandler> _logger;

    public UpdateExtractionCandidateCommandHandler(
        IExtractionCandidateRepository candidateRepository,
        IEmailMessageRepository emailRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateExtractionCandidateCommandHandler> logger)
    {
        _candidateRepository = candidateRepository;
        _emailRepository = emailRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateExtractionCandidateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Updating extraction candidate {CandidateId} for user {UserId}",
            request.CandidateId, request.UserId);

        // Get candidate
        var candidate = await _candidateRepository.GetByIdAsync(request.CandidateId, cancellationToken);
        if (candidate == null)
        {
            throw new KeyNotFoundException($"Extraction candidate {request.CandidateId} not found");
        }

        // Verify user authorization
        var email = await _emailRepository.GetByIdAsync(candidate.EmailMessageId, cancellationToken);
        if (email == null || email.UserId != request.UserId)
        {
            throw new UnauthorizedAccessException($"User {request.UserId} does not have access to candidate {request.CandidateId}");
        }

        // Verify status is Pending (can't edit confirmed or rejected)
        if (candidate.Status != RecordStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot update candidate with status {candidate.Status}. Only Pending candidates can be updated.");
        }

        // Update transaction data if provided
        var amount = request.Amount ?? candidate.Amount?.Amount;
        var currency = request.Currency ?? candidate.Amount?.Currency.ToString();
        var merchant = request.Merchant ?? candidate.Merchant;
        var transactionDate = request.TransactionDate ?? candidate.TransactionDate;

        if (amount.HasValue && !string.IsNullOrWhiteSpace(currency))
        {
            if (Enum.TryParse<Currency>(currency, true, out var currencyEnum))
            {
                var money = Money.Create(amount.Value, currencyEnum);
                candidate.SetTransactionData(
                    money,
                    transactionDate,
                    merchant,
                    candidate.AmountConfidence,
                    candidate.DateConfidence,
                    candidate.MerchantConfidence);
            }
        }

        // Update account info if provided
        var sourceAccount = !string.IsNullOrWhiteSpace(request.SourceAccount)
            ? AccountNumber.Create(request.SourceAccount)
            : candidate.SourceAccount;

        var targetAccount = !string.IsNullOrWhiteSpace(request.TargetAccount)
            ? AccountNumber.Create(request.TargetAccount)
            : candidate.TargetAccount;

        var sourceBank = !string.IsNullOrWhiteSpace(request.SourceBank)
            ? BankProvider.Create(request.SourceBank, request.SourceBank.ToUpperInvariant())
            : candidate.SourceBank;

        var targetBank = !string.IsNullOrWhiteSpace(request.TargetBank)
            ? BankProvider.Create(request.TargetBank, request.TargetBank.ToUpperInvariant())
            : candidate.TargetBank;

        candidate.SetAccountInfo(sourceAccount, targetAccount, sourceBank, targetBank);

        // Update additional details if provided
        Money? fees = null;
        Money? tax = null;

        if (request.Fees.HasValue && candidate.Amount != null)
        {
            fees = Money.Create(request.Fees.Value, candidate.Amount.Currency);
        }

        if (request.Tax.HasValue && candidate.Amount != null)
        {
            tax = Money.Create(request.Tax.Value, candidate.Amount.Currency);
        }

        var referenceId = request.ReferenceId ?? candidate.ReferenceId;

        candidate.SetAdditionalDetails(fees ?? candidate.Fees, tax ?? candidate.Tax, referenceId);

        // Save changes
        _candidateRepository.Update(candidate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated extraction candidate {CandidateId}", request.CandidateId);

        return Unit.Value;
    }
}
