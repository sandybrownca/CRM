using BusinessOperations.Application.Common;

namespace BusinessOperations.Application.Payments;

public interface IPaymentService
{
    Task<PaymentDto?> RecordAsync(
        Guid invoiceId,
        RecordPaymentRequest request,
        CancellationToken cancellationToken = default);
    Task<PagedResult<PaymentListItemDto>> SearchAsync(
        Guid? invoiceId,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    
}