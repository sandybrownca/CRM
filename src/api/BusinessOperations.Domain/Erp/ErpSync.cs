using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Invoices;

namespace BusinessOperations.Domain.Erp;

public sealed class ErpSync : EntityBase
{
    public Guid InvoiceId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public ErpSyncStatus Status { get; set; } = ErpSyncStatus.Pending;
    public string? ExternalReference { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }

    public Invoice? Invoice { get; set; }
}