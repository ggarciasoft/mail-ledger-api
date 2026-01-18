namespace MainLedger.Contracts.Users;

public record NotificationPreferencesDto(
    bool EmailNotificationsEnabled,
    bool NotifyOnEmailSync,
    bool NotifyOnClassification,
    bool NotifyOnExtraction
);
