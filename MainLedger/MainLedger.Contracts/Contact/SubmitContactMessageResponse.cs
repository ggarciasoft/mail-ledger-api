namespace MainLedger.Contracts.Contact;

public record SubmitContactMessageResponse
{
    public string Message { get; init; } = string.Empty;
}
