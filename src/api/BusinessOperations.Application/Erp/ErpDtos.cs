using BusinessOperations.Domain.Erp;

namespace BusinessOperations.Application.Erp;

public sealed record ErpInvoiceSubmission(
    string IdempotencyKey,
    string InvoiceNumber,
    string CustomerName,
    decimal TotalAmount
);

public sealed record ErpSyncDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    ErpSyncStatus Status,
    string IdempotencyKey,
    string? ExternalReference,
    int AttemptCount,
    string? LastError,
    DateTime? LastAttemptAtUtc
);

public sealed class ErpUnavailableException: Exception
{
    public ErpUnavailableException(string message): base(message)
    {
        
    }
}