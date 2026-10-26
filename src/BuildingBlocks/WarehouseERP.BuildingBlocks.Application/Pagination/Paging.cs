using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace WarehouseERP.BuildingBlocks.Application.Pagination;

/// <summary>One page of a list, the shape every list endpoint returns.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Bound to the <c>Pagination</c> configuration section.</summary>
public sealed class PaginationOptions
{
    public const string SectionName = "Pagination";

    [Range(1, 500)]
    public int DefaultPageSize { get; init; } = 20;

    [Range(1, 500)]
    public int MaxPageSize { get; init; } = 100;
}

/// <summary>Implemented by list queries; the page size is optional and defaults from configuration.</summary>
public interface IPagedQuery
{
    int Page { get; }
    int? PageSize { get; }
}

public static class PagingRules
{
    public static int ResolvePageSize(this IPagedQuery query, PaginationOptions options) =>
        query.PageSize ?? options.DefaultPageSize;

    /// <summary>Page from 1; page size, when given, from 1 to the configured maximum.</summary>
    public static void AddPagingRules<TQuery>(this AbstractValidator<TQuery> validator, PaginationOptions options)
        where TQuery : IPagedQuery
    {
        validator.RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        validator.RuleFor(q => q.PageSize!.Value)
            .InclusiveBetween(1, options.MaxPageSize)
            .OverridePropertyName(nameof(IPagedQuery.PageSize))
            .When(q => q.PageSize.HasValue);
    }
}
