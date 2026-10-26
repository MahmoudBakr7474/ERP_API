using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.UnitTests.BuildingBlocks;

public sealed class ResultHttpMappingTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.BusinessRule, 422)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    public void Every_error_type_has_one_status(ErrorType type, int status)
    {
        Assert.Equal(status, ResultHttpMapping.StatusCodeFor(type));
    }

    [Fact]
    public void Problem_carries_the_stable_error_code()
    {
        var problem = Assert.IsType<ProblemHttpResult>(
            Error.BusinessRule("Organization.Warehouse.TransitCannotSell", "No.").ToProblem());

        Assert.Equal(422, problem.StatusCode);
        Assert.Equal("Organization.Warehouse.TransitCannotSell", problem.ProblemDetails.Extensions["errorCode"]);
        Assert.Equal("No.", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Validation_problem_lists_fields_in_camel_case()
    {
        var error = Error.Validation(
        [
            new ValidationError("NameAr", "NotEmptyValidator", "Required."),
            new ValidationError("Lines[0].UnitId", "NotEmptyValidator", "Required."),
        ]);

        var problem = Assert.IsType<ProblemHttpResult>(error.ToProblem());
        var fields = Assert.IsType<List<ProblemFieldError>>(problem.ProblemDetails.Extensions["errors"]);

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(["nameAr", "lines[0].unitId"], fields.Select(f => f.Property));
        Assert.Equal(Error.ValidationCode, problem.ProblemDetails.Extensions["errorCode"]);
    }

    [Fact]
    public void Success_uses_the_given_result()
    {
        Result<int> result = 7;
        var http = Assert.IsType<Ok<int>>(result.ToHttpResult(v => TypedResults.Ok(v)));
        Assert.Equal(7, http.Value);
    }
}
