using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Identity.Application.Features.Roles.CreateRole;
using WarehouseERP.Identity.Application.Features.Users.CreateUser;
using WarehouseERP.Identity.Application.Features.Users.ListUsers;
using WarehouseERP.Identity.Application.Features.Authentication.Login;
using WarehouseERP.Identity.Application.Features.Authentication.RefreshApiToken;

namespace WarehouseERP.UnitTests.Identity;

public sealed class IdentityValidatorTests
{
    private static CreateUserCommand User(string userName = "store.keeper", string password = "Correct-horse-9", string? email = null) =>
        new(userName, "أمين المخزن", email, password, [], false, [], []);

    [Theory]
    [InlineData("store.keeper")]
    [InlineData("ahmed@branch-1")]
    [InlineData("A_B")]
    public void User_names_are_ascii_letters_digits_and_dot_at_dash_underscore(string userName)
    {
        Assert.True(new CreateUserValidator().Validate(User(userName)).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("أحمد")]
    [InlineData("semi;colon")]
    public void Other_user_names_are_refused_on_the_user_name_field(string userName)
    {
        var result = new CreateUserValidator().Validate(User(userName));

        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(CreateUserCommand.UserName), error.PropertyName);
    }

    [Fact]
    public void Password_and_email_shape_are_checked_but_strength_is_left_to_identity()
    {
        Assert.Contains(new CreateUserValidator().Validate(User(password: "")).Errors, e => e.PropertyName == "Password");
        Assert.Contains(new CreateUserValidator().Validate(User(email: "not-an-email")).Errors, e => e.PropertyName == "Email");
        Assert.True(new CreateUserValidator().Validate(User(password: "weak")).IsValid); // policy errors come from ASP.NET Core Identity
    }

    [Fact]
    public void Sign_in_inputs_need_values_of_sane_length()
    {
        Assert.False(new LoginValidator().Validate(new LoginCommand("", "x")).IsValid);
        Assert.False(new LoginValidator().Validate(new LoginCommand("u", new string('x', 257))).IsValid);
        Assert.False(new RefreshApiTokenValidator().Validate(new RefreshApiTokenCommand(new string('x', 101))).IsValid);
        Assert.True(new LoginValidator().Validate(new LoginCommand("u", "x")).IsValid);
    }

    [Fact]
    public void Role_needs_a_name_and_no_blank_permission()
    {
        Assert.False(new CreateRoleValidator().Validate(new CreateRoleCommand(" ", null, [])).IsValid);
        Assert.False(new CreateRoleValidator().Validate(new CreateRoleCommand("Clerk", null, [""])).IsValid);
        Assert.True(new CreateRoleValidator().Validate(new CreateRoleCommand("Clerk", null, [])).IsValid);
    }

    [Fact]
    public void User_list_paging_follows_the_configured_limits()
    {
        var validator = new ListUsersValidator(Options.Create(new PaginationOptions { MaxPageSize = 50 }));

        Assert.False(validator.Validate(new ListUsersQuery(0, null, null)).IsValid);
        Assert.False(validator.Validate(new ListUsersQuery(1, 51, null)).IsValid);
        Assert.True(validator.Validate(new ListUsersQuery(1, 50, "ahmed")).IsValid);
    }
}
