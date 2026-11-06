using BusinessOperations.Application.Common;

namespace BusinessOperations.Application.Users;

public interface IUserAdminService
{
    Task<PagedResult<UserListItemDto>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UserListItemDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserListItemDto?> UpdateAsync(
        string id,
        UpdateUserRequest request,
        string actorUserId,
        CancellationToken cancellationToken = default);
}