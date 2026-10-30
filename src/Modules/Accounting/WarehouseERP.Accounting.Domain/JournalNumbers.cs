using System.Globalization;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// Journal numbers: the type's prefix, the year its fiscal year starts in and a six-digit sequence per type and fiscal year
/// (<c>GJ-2027-000001</c>). Drafts carry their own reference (<c>DJ-000123</c>) until they post.
/// </summary>
public static class JournalNumbers
{
    public static string Prefix(JournalType type) => type switch
    {
        JournalType.Manual => "GJ",
        JournalType.OpeningBalance => "OB",
        JournalType.Reversal => "RJ",
        JournalType.YearEndClosing => "CJ",
        JournalType.CustomerReceipt => "RV",
        JournalType.SupplierPayment => "PV",
        JournalType.CustomerRefund => "CF",
        JournalType.SupplierRefund => "SF",
        JournalType.CashReceipt => "CR",
        JournalType.CashPayment => "CP",
        JournalType.CashTransfer => "TR",
        JournalType.ExpenseBill => "EB",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown journal type."),
    };

    public static string Format(JournalType type, int fiscalYearStart, int sequence) =>
        string.Create(CultureInfo.InvariantCulture, $"{Prefix(type)}-{fiscalYearStart}-{sequence:D6}");

    public static string DraftReference(int draftNumber) => string.Create(CultureInfo.InvariantCulture, $"DJ-{draftNumber:D6}");
}
