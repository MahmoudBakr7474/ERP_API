namespace WarehouseERP.Pos.Contracts;

/// <summary>A sale, a return or a shift close as Accounting discovers it.</summary>
public sealed record PosPostedDocument(Guid Id, PosDocumentType DocumentType, string Number, DateTime OccurredAtUtc);
