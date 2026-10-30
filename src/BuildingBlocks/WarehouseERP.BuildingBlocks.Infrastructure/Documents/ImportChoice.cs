namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>An allowed value of a choice column: the name the API expects and its label in each language.</summary>
public sealed record ImportChoice(string Value, string LabelEn, string LabelAr);
