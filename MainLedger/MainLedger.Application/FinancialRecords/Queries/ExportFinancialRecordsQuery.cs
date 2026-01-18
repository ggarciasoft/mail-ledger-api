using MediatR;

namespace MainLedger.Application.FinancialRecords.Queries;

public enum ExportFormat
{
    Csv,
    Json,
}

public record ExportFileDto(string FileName, string ContentType, byte[] Content);

public record ExportFinancialRecordsQuery(
    Guid UserId,
    ExportFormat Format,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? Merchant = null,
    string? Currency = null,
    string? SourceBank = null,
    string SortBy = "transactionDate",
    string SortOrder = "desc"
) : IRequest<ExportFileDto>;
