using BusinessOperations.Domain.Identity;
using FluentValidation;

namespace BusinessOperations.Application.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(10).MaximumLength(100);
        RuleFor(x => x.Role).Must(role => AppRoles.All.Contains(role))
            .WithMessage("Role must be Administrator or Operations.");
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).Must(role => AppRoles.All.Contains(role))
            .WithMessage("Role must be Administrator or Operations.");
    }
}