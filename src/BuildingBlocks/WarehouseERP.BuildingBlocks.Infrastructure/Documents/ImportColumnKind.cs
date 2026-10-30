namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>How a template column is formatted and validated in Excel.</summary>
public enum ImportColumnKind
{
    Text,
    Code,
    Number,
    Choice,
}
