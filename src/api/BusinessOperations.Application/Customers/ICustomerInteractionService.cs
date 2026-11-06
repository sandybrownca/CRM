using BusinessOperations.Application.Common;
using BusinessOperations.Domain.Customers;

namespace BusinessOperations.Application.Customers;

public interface ICustomerInteractionService
{
    Task<PagedResult<CustomerInteractionDto>?> SearchAsync(
        Guid customerId,
        InteractionChannel? channel,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    Task<CustomerInteractionDto?> LogAsync(
        Guid customerId,
        LogInteractionRequest? channel,
        string recordedByName,
        CancellationToken cancellationToken = default
    );
}