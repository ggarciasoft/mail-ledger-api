using System.Text;
using System.Text.Json;
using MainLedger.Application.Common.Interfaces;
using MainLedger.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MainLedger.Application.FinancialRecords.Queries;

public class ExportFinancialRecordsQueryHandler
    : IRequestHandler<ExportFinancialRecordsQuery, ExportFileDto>
{
    private readonly IFinancialRecordRepository _recordRepository;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<ExportFinancialRecordsQueryHandler> _logger;

    public ExportFinancialRecordsQueryHandler(
        IFinancialRecordRepository recordRepository,
        ISubscriptionService subscriptionService,
        ILogger<ExportFinancialRecordsQueryHandler> logger
    )
    {
        _recordRepository = recordRepository;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<ExportFileDto> Handle(
        ExportFinancialRecordsQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Exporting financial records for user {UserId} in {Format} format",
            request.UserId,
            request.Format
        );

        // Check subscription permission
        if (!await _subscriptionService.CanUseExportAsync(request.UserId, cancellationToken))
        {
            _logger.LogWarning(
                "User {UserId} attempted export without subscription permission",
                request.UserId
            );
            throw new UnauthorizedAccessException("Export feature requires a paid subscription.");
        }

        // Get records
        var records = await _recordRepository.GetFilteredAsync(
            request.UserId,
            request.StartDate,
            request.EndDate,
            request.MinAmount,
            request.MaxAmount,
            request.Merchant,
            request.Currency,
            request.SourceBank,
            request.SortBy,
            request.SortOrder,
            cancellationToken
        );

        string fileName;
        string contentType;
        byte[] content;

        if (request.Format == ExportFormat.Json)
        {
            fileName = $"financial_records_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            contentType = "application/json";
            var json = JsonSerializer.Serialize(
                records,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }
            );
            content = Encoding.UTF8.GetBytes(json);
        }
        else // CSV
        {
            fileName = $"financial_records_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            contentType = "text/csv";
            var csv = GenerateCsv(records);
            content = Encoding.UTF8.GetBytes(csv);
        }

        return new ExportFileDto(fileName, contentType, content);
    }

    private string GenerateCsv(List<MainLedger.Domain.Entities.FinancialRecord> records)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(
            "Id,Date,Type,Direction,Amount,Currency,Merchant,SourceBank,SourceAccount,TargetBank,TargetAccount,Status,CreatedAt"
        );

        foreach (var record in records)
        {
            sb.Append($"{record.Id},");
            sb.Append($"{record.TransactionDate:yyyy-MM-dd HH:mm:ss},");
            sb.Append($"{record.Type},");
            sb.Append($"{record.Direction},");
            sb.Append($"{record.Amount.Amount},");
            sb.Append($"{record.Amount.Currency},");
            sb.Append($"{EscapeCsv(record.Merchant)},");
            sb.Append($"{EscapeCsv(record.SourceBank?.Name)},");
            sb.Append($"{EscapeCsv(record.SourceAccount?.Value)},");
            sb.Append($"{EscapeCsv(record.TargetBank?.Name)},");
            sb.Append($"{EscapeCsv(record.TargetAccount?.Value)},");
            sb.Append($"{record.Status},");
            sb.Append($"{record.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
