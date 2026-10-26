using FluentValidation;

namespace WarehouseERP.Identity.Application.Features.Authentication.RefreshApiToken;

public sealed class RefreshApiTokenValidator : AbstractValidator<RefreshApiTokenCommand>
{
    public RefreshApiTokenValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(100);
}
