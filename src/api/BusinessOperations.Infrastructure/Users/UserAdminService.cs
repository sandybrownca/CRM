using BusinessOperations.Application.Common;
using BusinessOperations.Application.Users;
using BusinessOperations.Domain.Identity;
using BusinessOperations.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Users;

public sealed class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserAdminService(
        UserManager<ApplicationUser> users,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _users = users;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<UserListItemDto>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                u.DisplayName.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<UserListItemDto>(users.Count);
        foreach (var user in users)
        {
            items.Add(await MapAsync(user));
        }

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserListItemDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true
        };

        var created = await _users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new ValidationException(string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        var roleResult = await _users.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            throw new ValidationException(string.Join(" ", roleResult.Errors.Select(e => e.Description)));
        }

        return await MapAsync(user);
    }

    public async Task<UserListItemDto?> UpdateAsync(
        string id,
        UpdateUserRequest request,
        string actorUserId,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        if (user.Id == actorUserId && !request.IsActive)
        {
            throw new ValidationException("You cannot deactivate your own account.");
        }

        var currentRoles = await _users.GetRolesAsync(user);
        var removingLastAdmin = user.IsActive
            && currentRoles.Contains(AppRoles.Administrator)
            && (request.Role != AppRoles.Administrator || !request.IsActive);
        if (removingLastAdmin && await ActiveAdministratorCountAsync(user.Id) == 0)
        {
            throw new ValidationException("At least one active administrator is required.");
        }

        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            throw new ValidationException(string.Join(" ", updated.Errors.Select(e => e.Description)));
        }

        if (currentRoles.Count != 1 || !currentRoles.Contains(request.Role))
        {
            if (currentRoles.Count > 0)
            {
                var removed = await _users.RemoveFromRolesAsync(user, currentRoles);
                if (!removed.Succeeded)
                {
                    throw new ValidationException(string.Join(" ", removed.Errors.Select(e => e.Description)));
                }
            }

            var added = await _users.AddToRoleAsync(user, request.Role);
            if (!added.Succeeded)
            {
                throw new ValidationException(string.Join(" ", added.Errors.Select(e => e.Description)));
            }
        }

        return await MapAsync(user);
    }

    private async Task<int> ActiveAdministratorCountAsync(string exceptUserId)
    {
        var admins = await _users.GetUsersInRoleAsync(AppRoles.Administrator);
        return admins.Count(u => u.IsActive && u.Id != exceptUserId);
    }

    private async Task<UserListItemDto> MapAsync(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return new UserListItemDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.IsActive,
            roles.OrderBy(r => r).ToArray());
    }
}