namespace BusinessOperations.Application.Users;

public sealed record UserListItemDto(
    string Id,
    string Email,
    string DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles);

public sealed record CreateUserRequest(
    string Email,
    string DisplayName,
    string Password,
    string Role);

public sealed record UpdateUserRequest(
    string DisplayName,
    string Role,
    bool IsActive);