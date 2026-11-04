using BusinessOperations.Domain.Common;

namespace BusinessOperations.Domain.Customers;

public class CustomerActivity : EntityBase
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? RelatedRecordId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
