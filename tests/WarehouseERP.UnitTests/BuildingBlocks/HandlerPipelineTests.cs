using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Messaging;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.UnitTests.BuildingBlocks;

public sealed class HandlerPipelineTests
{
    public sealed record Rename(string Name);

    private sealed class RenameValidator : AbstractValidator<Rename>
    {
        public RenameValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(5);
    }

    private sealed class RenameHandler : ICommandHandler<Rename, string>
    {
        public int Calls { get; private set; }

        public Task<Result<string>> HandleAsync(Rename command, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<Result<string>>(command.Name == "taken"
                ? Error.Conflict("Test.Name.Taken", "Taken.")
                : command.Name.ToUpperInvariant());
        }
    }

    private static (ICommandHandler<Rename, string> Pipeline, RenameHandler Inner, ListLogger Logs) Build()
    {
        var logs = new ListLogger();
        var services = new ServiceCollection()
            .AddSingleton<IValidator<Rename>, RenameValidator>()
            .AddSingleton<ILoggerFactory>(new SingleLoggerFactory(logs))
            .AddLogging()
            .AddCommandHandler<Rename, string, RenameHandler>()
            .BuildServiceProvider();
        var scope = services.CreateScope().ServiceProvider;
        return (scope.GetRequiredService<ICommandHandler<Rename, string>>(), scope.GetRequiredService<RenameHandler>(), logs);
    }

    [Fact]
    public async Task Invalid_command_never_reaches_the_handler()
    {
        var (pipeline, inner, logs) = Build();

        var result = await pipeline.HandleAsync(new Rename(""), default);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Equal("Name", Assert.Single(result.Error.ValidationErrors).Property);
        Assert.Equal(0, inner.Calls);
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("rejected by validation"));
    }

    [Fact]
    public async Task Valid_command_runs_once_and_logs_success()
    {
        var (pipeline, inner, logs) = Build();

        var result = await pipeline.HandleAsync(new Rename("abc"), default);

        Assert.Equal("ABC", result.Value);
        Assert.Equal(1, inner.Calls);
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Information && e.Message.StartsWith("Rename succeeded", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Business_failure_is_logged_as_a_warning_with_its_code_but_not_the_payload()
    {
        var (pipeline, _, logs) = Build();

        var result = await pipeline.HandleAsync(new Rename("taken"), default);

        Assert.Equal("Test.Name.Taken", result.Error!.Code);
        var entry = Assert.Single(logs.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("Test.Name.Taken", entry.Message);
        Assert.DoesNotContain("taken", entry.Message, StringComparison.Ordinal);
    }

    public sealed record ListThings(int Page, int? PageSize) : IPagedQuery;

    private sealed class ListThingsValidator : AbstractValidator<ListThings>
    {
        public ListThingsValidator(PaginationOptions options) => this.AddPagingRules(options);
    }

    [Theory]
    [InlineData(1, null, true)]
    [InlineData(1, 100, true)]
    [InlineData(0, null, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void Paging_rules_follow_the_configured_maximum(int page, int? pageSize, bool valid)
    {
        var validator = new ListThingsValidator(new PaginationOptions { MaxPageSize = 100 });
        Assert.Equal(valid, validator.Validate(new ListThings(page, pageSize)).IsValid);
    }

    [Fact]
    public void Page_size_defaults_from_configuration()
    {
        Assert.Equal(25, new ListThings(1, null).ResolvePageSize(new PaginationOptions { DefaultPageSize = 25 }));
        Assert.Equal(5, new ListThings(1, 5).ResolvePageSize(new PaginationOptions { DefaultPageSize = 25 }));
    }
}
