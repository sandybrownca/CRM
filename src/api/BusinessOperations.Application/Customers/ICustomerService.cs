using BusinessOperations.Application.Common;

namespace BusinessOperations.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerListItemDto>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CustomerDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerDetailsDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDetailsDto?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<CustomerActivityDto>?>SearchActivitiesAsync(
        Guid customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );
}
