using FluentValidation;
using WarehouseERP.Identity.Application.Shared;

namespace WarehouseERP.Identity.Application.Features.Authentication.IssueApiToken;

public sealed class IssueApiTokenValidator : AbstractValidator<IssueApiTokenCommand>
{
    public IssueApiTokenValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).PresentPassword();
        RuleFor(x => x.DeviceId).MaximumLength(100);
    }
}
