using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.UnitTests.BuildingBlocks;

public sealed class ResultTests
{
    private static readonly Error Taken = Error.Conflict("Test.Thing.Taken", "Taken.");

    [Fact]
    public void Success_carries_the_value_and_no_error()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_carries_the_error_and_refuses_to_give_a_value()
    {
        Result<int> result = Taken;

        Assert.True(result.IsFailure);
        Assert.Same(Taken, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Untyped_result_converts_from_an_error()
    {
        Result result = Taken;

        Assert.False(result.IsSuccess);
        Assert.Equal("Test.Thing.Taken", result.Error!.Code);
        Assert.True(Result.Success().IsSuccess);
    }

    [Fact]
    public void Failure_needs_an_error()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));
    }

    [Fact]
    public void Validation_error_has_the_fixed_code_and_its_fields()
    {
        var error = Error.Validation([new ValidationError("Code", "NotEmptyValidator", "Required.")]);

        Assert.Equal((Error.ValidationCode, ErrorType.Validation), (error.Code, error.Type));
        Assert.Equal("Code", Assert.Single(error.ValidationErrors).Property);
    }

    [Fact]
    public void Error_needs_a_code()
    {
        Assert.Throws<ArgumentException>(() => Error.NotFound(" ", "x"));
    }
}
