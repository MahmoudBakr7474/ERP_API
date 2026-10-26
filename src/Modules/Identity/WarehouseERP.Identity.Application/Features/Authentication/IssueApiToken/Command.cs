namespace WarehouseERP.Identity.Application.Features.Authentication.IssueApiToken;

/// <param name="DeviceId">The POS terminal, when the caller is one.</param>
public sealed record IssueApiTokenCommand(string UserName, string Password, string? DeviceId);
