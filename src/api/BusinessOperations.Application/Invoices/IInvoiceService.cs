using BusinessOperations.Application.Common;
using BusinessOperations.Domain.Invoices;

namespace BusinessOperations.Application.Invoices;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> SearchAsync(
        Guid? customerId,
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<InvoiceDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvoiceDetailsDto> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);

    Task<InvoiceDetailsDto?> UpdateAsync(Guid id, UpdateInvoiceRequest request, CancellationToken cancellationToken = default);

    Task<InvoiceDetailsDto?> IssueAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvoiceDetailsDto?> VoidAsync(Guid id, CancellationToken cancellationToken = default);
}
