using System.Text.RegularExpressions;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// A till: it sells from one physical warehouse that allows selling, out of one selling location of that warehouse, and
/// numbers its own receipts, returns and shifts. Its code and warehouse never change (receipts carry them); its name,
/// selling location, accepted payment methods and receipt texts may. Master data, versioned in a temporal table.
/// </summary>
public sealed partial class PosTerminal : MasterEntity
{
    public const int MaxCodeLength = 20;
    public const int MaxNameLength = 100;
    public const int MaxReceiptTextLength = 500;

    private PosTerminal() { } // EF Core

    /// <summary>Unique and fixed: upper-case letters, digits and dashes (CAI-T1).</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>The warehouse's branch, fixed with it.</summary>
    public Guid BranchId { get; private set; }

    /// <summary>Fixed: a till in another warehouse is another terminal.</summary>
    public Guid WarehouseId { get; private set; }

    /// <summary>The location of the warehouse sales leave from and returns come back into.</summary>
    public Guid SellingLocationId { get; private set; }

    public bool AcceptsCash { get; private set; }
    public bool AcceptsCard { get; private set; }

    /// <summary>Printed at the top of its receipts (shop name, address, phone).</summary>
    public string? ReceiptHeader { get; private set; }

    /// <summary>Printed at the bottom of its receipts (return policy, thanks).</summary>
    public string? ReceiptFooter { get; private set; }

    public static Result<PosTerminal> Create(
        string code, string name, Guid branchId, Guid warehouseId, Guid sellingLocationId, bool acceptsCash, bool acceptsCard,
        string? receiptHeader, string? receiptFooter)
    {
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        if (!CodePattern().IsMatch(normalized))
            return PosTerminalDomainErrors.CodeInvalid;
        var terminal = new PosTerminal { Code = normalized, BranchId = branchId, WarehouseId = warehouseId };
        var updated = terminal.Update(name, sellingLocationId, acceptsCash, acceptsCard, receiptHeader, receiptFooter);
        return updated.IsFailure ? updated.Error : terminal;
    }

    public Result Update(
        string name, Guid sellingLocationId, bool acceptsCash, bool acceptsCard, string? receiptHeader, string? receiptFooter)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length is 0 or > MaxNameLength)
            return PosTerminalDomainErrors.NameInvalid;
        if (!acceptsCash && !acceptsCard)
            return PosTerminalDomainErrors.NoPaymentMethod;
        var header = Text.Optional(receiptHeader);
        var footer = Text.Optional(receiptFooter);
        if (header?.Length > MaxReceiptTextLength || footer?.Length > MaxReceiptTextLength)
            return PosTerminalDomainErrors.ReceiptTextTooLong;
        Name = trimmed;
        SellingLocationId = sellingLocationId;
        AcceptsCash = acceptsCash;
        AcceptsCard = acceptsCard;
        ReceiptHeader = header;
        ReceiptFooter = footer;
        return Result.Success();
    }

    public bool Accepts(PosPaymentMethod method) => method switch
    {
        PosPaymentMethod.Cash => AcceptsCash,
        PosPaymentMethod.Card => AcceptsCard,
        _ => false,
    };

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{0,19}$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
