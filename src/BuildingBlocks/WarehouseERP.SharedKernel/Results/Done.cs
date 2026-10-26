namespace WarehouseERP.SharedKernel.Results;

/// <summary>The value of a command that succeeds without returning anything (HTTP 204).</summary>
public readonly record struct Done
{
    public static Done Value => default;
}
