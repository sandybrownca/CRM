
using BusinessOperations.Domain.Common;

namespace BusinessOperations.Domain.Customers;
public class CustomerInteraction: EntityBase
{
    public Guid CustomerId { get; set;}
    public Customer? Customer { get; set;}
    public InteractionChannel Channel { get; set;}
    public string Subject { get; set;}= string.Empty;
    public string Body { get; set;}= string.Empty;
    public DateTime OccurredAtUtc { get; set;} = DateTime.UtcNow;
    public string RecordedByName { get; set;} = string.Empty;
}