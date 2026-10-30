using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Accounts;

// The default chart for an Egyptian trading and distribution company (owner decision 2026-10-30): five classes, numeric
// codes extending their parent's, Arabic and English names, and a mapping for every posting role. It is a starting point
// the company's accountant edits (or replaces by importing the approved chart) before accounting starts; nothing in the
// code depends on these codes. Accounts only the system feeds (inventory, GRNI, unbilled receivables, till clearing, cost
// of goods sold and inventory differences) refuse manual journals, so their balances always come from their sources.
// Purchase price differences under review take them: the accountant moves reviewed differences to cost of sales or another
// approved account by journal (V1 decision 8).
internal static class DefaultChart
{
    private const AccountType A = AccountType.Asset;
    private const AccountType L = AccountType.Liability;
    private const AccountType E = AccountType.Equity;
    private const AccountType R = AccountType.Revenue;
    private const AccountType X = AccountType.Expense;

    public static IReadOnlyList<ChartAccountRow> Accounts { get; } =
    [
        H("1", null, A, "الأصول", "Assets"),
        H("11", "1", A, "الأصول غير المتداولة", "Non-current assets"),
        H("1101", "11", A, "الأصول الثابتة", "Property, plant and equipment"),
        P("110101", "1101", A, StatementSection.NonCurrentAssets, "الأراضي", "Land"),
        P("110102", "1101", A, StatementSection.NonCurrentAssets, "المباني", "Buildings"),
        P("110103", "1101", A, StatementSection.NonCurrentAssets, "وسائل النقل", "Vehicles"),
        P("110104", "1101", A, StatementSection.NonCurrentAssets, "الأثاث والتجهيزات", "Furniture and fixtures"),
        P("110105", "1101", A, StatementSection.NonCurrentAssets, "أجهزة الحاسب والمعدات", "Computers and equipment"),
        H("1102", "11", A, "مجمع الإهلاك", "Accumulated depreciation"),
        P("110201", "1102", A, StatementSection.NonCurrentAssets, "مجمع إهلاك المباني", "Accumulated depreciation - buildings"),
        P("110202", "1102", A, StatementSection.NonCurrentAssets, "مجمع إهلاك وسائل النقل", "Accumulated depreciation - vehicles"),
        P("110203", "1102", A, StatementSection.NonCurrentAssets, "مجمع إهلاك الأثاث والتجهيزات", "Accumulated depreciation - furniture and fixtures"),
        P("110204", "1102", A, StatementSection.NonCurrentAssets, "مجمع إهلاك أجهزة الحاسب والمعدات", "Accumulated depreciation - computers and equipment"),
        H("12", "1", A, "الأصول المتداولة", "Current assets"),
        H("1201", "12", A, "المخزون", "Inventory"),
        P("120101", "1201", A, StatementSection.CurrentAssets, "مخزون البضاعة", "Merchandise inventory", manual: false),
        H("1202", "12", A, "العملاء والمدينون", "Customers and receivables"),
        P("120201", "1202", A, StatementSection.CurrentAssets, "العملاء", "Customers", party: PartyType.Customer),
        P("120202", "1202", A, StatementSection.CurrentAssets, "إيرادات مستحقة غير مفوترة", "Unbilled receivables", manual: false),
        P("120203", "1202", A, StatementSection.CurrentAssets, "سلف العاملين", "Employee advances"),
        P("120204", "1202", A, StatementSection.CurrentAssets, "مدينون متنوعون", "Other receivables"),
        H("1203", "12", A, "ضرائب مدينة", "Tax receivables"),
        P("120301", "1203", A, StatementSection.CurrentAssets, "ضريبة القيمة المضافة على المشتريات", "Input VAT"),
        P("120302", "1203", A, StatementSection.CurrentAssets, "ضرائب الخصم والإضافة لدى العملاء", "Withholding tax deducted by customers"),
        H("1204", "12", A, "النقدية والبنوك", "Cash and banks"),
        P("120401", "1204", A, StatementSection.CurrentAssets, "الخزينة الرئيسية", "Main cashbox"),
        P("120402", "1204", A, StatementSection.CurrentAssets, "البنك - حساب جاري", "Bank - current account"),
        P("120403", "1204", A, StatementSection.CurrentAssets, "وسيط نقدية نقاط البيع", "POS till clearing", manual: false),
        P("120404", "1204", A, StatementSection.CurrentAssets, "وسيط مدفوعات البطاقات", "Card payments clearing"),
        H("1205", "12", A, "أرصدة مدينة أخرى", "Other current assets"),
        P("120501", "1205", A, StatementSection.CurrentAssets, "مصروفات مدفوعة مقدماً", "Prepaid expenses"),
        P("120502", "1205", A, StatementSection.CurrentAssets, "دفعات مقدمة للموردين", "Advances to suppliers"),
        P("120503", "1205", A, StatementSection.CurrentAssets, "فروق أسعار شراء تحت المراجعة", "Purchase price differences under review"),

        H("2", null, L, "الالتزامات", "Liabilities"),
        H("21", "2", L, "الالتزامات طويلة الأجل", "Non-current liabilities"),
        P("2101", "21", L, StatementSection.NonCurrentLiabilities, "قروض طويلة الأجل", "Long-term loans"),
        H("22", "2", L, "الالتزامات المتداولة", "Current liabilities"),
        H("2201", "22", L, "الموردون والدائنون", "Suppliers and payables"),
        P("220101", "2201", L, StatementSection.CurrentLiabilities, "الموردون", "Suppliers", party: PartyType.Supplier),
        P("220102", "2201", L, StatementSection.CurrentLiabilities, "بضاعة مستلمة لم ترد فواتيرها", "Goods received not invoiced", manual: false),
        P("220103", "2201", L, StatementSection.CurrentLiabilities, "أذون استلام مخزنية دون مورد", "Inventory receipts without a supplier", manual: false),
        H("2202", "22", L, "ضرائب مستحقة", "Taxes payable"),
        P("220201", "2202", L, StatementSection.CurrentLiabilities, "ضريبة القيمة المضافة على المبيعات", "Output VAT"),
        P("220202", "2202", L, StatementSection.CurrentLiabilities, "تسوية ضريبة القيمة المضافة", "VAT settlement"),
        P("220203", "2202", L, StatementSection.CurrentLiabilities, "ضرائب الخصم والإضافة المستحقة", "Withholding tax payable"),
        P("220204", "2202", L, StatementSection.CurrentLiabilities, "ضريبة الدخل المستحقة", "Income tax payable"),
        P("220205", "2202", L, StatementSection.CurrentLiabilities, "ضرائب كسب العمل والتأمينات الاجتماعية", "Payroll taxes and social insurance"),
        H("2203", "22", L, "أرصدة دائنة أخرى", "Other current liabilities"),
        P("220301", "2203", L, StatementSection.CurrentLiabilities, "مصروفات مستحقة", "Accrued expenses"),
        P("220302", "2203", L, StatementSection.CurrentLiabilities, "دفعات مقدمة من العملاء", "Advances from customers"),
        P("220303", "2203", L, StatementSection.CurrentLiabilities, "دائنون متنوعون", "Other payables"),
        P("2204", "22", L, StatementSection.CurrentLiabilities, "قروض قصيرة الأجل وسحب على المكشوف", "Short-term loans and overdrafts"),

        H("3", null, E, "حقوق الملكية", "Equity"),
        P("31", "3", E, StatementSection.Equity, "رأس المال", "Paid-in capital"),
        P("32", "3", E, StatementSection.Equity, "الاحتياطي القانوني", "Legal reserve"),
        P("33", "3", E, StatementSection.Equity, "الأرباح المرحلة", "Retained earnings"),
        P("34", "3", E, StatementSection.Equity, "جاري الشركاء", "Owners' current accounts"),
        P("35", "3", E, StatementSection.Equity, "فروق الأرصدة الافتتاحية", "Opening balance differences"),

        H("4", null, R, "الإيرادات", "Revenue"),
        H("41", "4", R, "إيرادات المبيعات", "Sales revenue"),
        P("4101", "41", R, StatementSection.Revenue, "مبيعات التوزيع", "Distribution sales"),
        P("4102", "41", R, StatementSection.Revenue, "مبيعات التجزئة (نقاط البيع)", "Retail sales (POS)"),
        P("4103", "41", R, StatementSection.Revenue, "مردودات مبيعات التوزيع", "Distribution sales returns"),
        P("4104", "41", R, StatementSection.Revenue, "مردودات مبيعات التجزئة", "Retail sales returns"),
        H("42", "4", R, "إيرادات أخرى", "Other income"),
        P("4201", "42", R, StatementSection.OtherIncome, "أرباح فروق المخزون", "Inventory gains"),
        P("4202", "42", R, StatementSection.OtherIncome, "فوائد دائنة", "Interest income"),
        P("4203", "42", R, StatementSection.OtherIncome, "إيرادات متنوعة", "Miscellaneous income"),

        H("5", null, X, "المصروفات", "Expenses"),
        H("51", "5", X, "تكلفة المبيعات", "Cost of sales"),
        P("5101", "51", X, StatementSection.CostOfSales, "تكلفة البضاعة المباعة", "Cost of goods sold", manual: false),
        P("5102", "51", X, StatementSection.CostOfSales, "فروق أسعار الشراء", "Purchase price variances"),
        P("5103", "51", X, StatementSection.CostOfSales, "تسويات المخزون", "Inventory adjustments", manual: false),
        P("5104", "51", X, StatementSection.CostOfSales, "فروق الجرد", "Stock count differences", manual: false),
        H("52", "5", X, "مصروفات البيع والتوزيع", "Selling and distribution expenses"),
        P("5201", "52", X, StatementSection.OperatingExpenses, "رواتب وأجور البيع", "Sales salaries and wages"),
        P("5202", "52", X, StatementSection.OperatingExpenses, "مصروفات النقل والتوزيع", "Transport and delivery"),
        P("5203", "52", X, StatementSection.OperatingExpenses, "عمولات البطاقات والمدفوعات", "Card and payment fees"),
        P("5204", "52", X, StatementSection.OperatingExpenses, "دعاية وإعلان", "Advertising"),
        H("53", "5", X, "المصروفات العمومية والإدارية", "General and administrative expenses"),
        P("5301", "53", X, StatementSection.OperatingExpenses, "رواتب وأجور إدارية", "Administrative salaries and wages"),
        P("5302", "53", X, StatementSection.OperatingExpenses, "الإيجارات", "Rent"),
        P("5303", "53", X, StatementSection.OperatingExpenses, "الكهرباء والمياه", "Utilities"),
        P("5304", "53", X, StatementSection.OperatingExpenses, "الاتصالات والإنترنت", "Communications"),
        P("5305", "53", X, StatementSection.OperatingExpenses, "الإهلاكات", "Depreciation"),
        P("5306", "53", X, StatementSection.OperatingExpenses, "أدوات مكتبية ومطبوعات", "Office supplies and printing"),
        P("5307", "53", X, StatementSection.OperatingExpenses, "أتعاب مهنية واستشارات", "Professional fees"),
        P("5308", "53", X, StatementSection.OperatingExpenses, "صرف مخزني للاستخدام الداخلي", "Internal use of inventory", manual: false),
        H("54", "5", X, "مصروفات أخرى", "Other expenses"),
        P("5401", "54", X, StatementSection.OtherExpenses, "خسائر وهالك المخزون", "Inventory losses and write-offs", manual: false),
        P("5402", "54", X, StatementSection.OtherExpenses, "عجز وزيادة النقدية", "Cash shortages and overages"),
        P("5403", "54", X, StatementSection.OtherExpenses, "مصروفات بنكية", "Bank charges"),
        P("5404", "54", X, StatementSection.OtherExpenses, "فوائد مدينة", "Interest expense"),
        P("55", "5", X, StatementSection.IncomeTax, "ضريبة الدخل", "Income tax expense"),
    ];

    /// <summary>The account code each posting role uses in the default chart.</summary>
    public static IReadOnlyList<ChartRoleRow> Roles { get; } =
    [
        new(PostingRole.RetainedEarnings, "33"),
        new(PostingRole.OpeningBalanceEquity, "35"),
        new(PostingRole.Inventory, "120101"),
        new(PostingRole.GoodsReceivedNotInvoiced, "220102"),
        new(PostingRole.PurchasePriceVariance, "5102"),
        new(PostingRole.PurchaseVarianceUnderReview, "120503"),
        new(PostingRole.AccountsReceivable, "120201"),
        new(PostingRole.UnbilledReceivables, "120202"),
        new(PostingRole.AccountsPayable, "220101"),
        new(PostingRole.InputVat, "120301"),
        new(PostingRole.OutputVat, "220201"),
        new(PostingRole.WithholdingTaxReceivable, "120302"),
        new(PostingRole.WithholdingTaxPayable, "220203"),
        new(PostingRole.SalesRevenue, "4101"),
        new(PostingRole.SalesReturns, "4103"),
        new(PostingRole.PosRevenue, "4102"),
        new(PostingRole.PosReturns, "4104"),
        new(PostingRole.CostOfSales, "5101"),
        new(PostingRole.InventoryLosses, "5401"),
        new(PostingRole.InventoryGains, "4201"),
        new(PostingRole.InventoryAdjustments, "5103"),
        new(PostingRole.StockCountDifferences, "5104"),
        new(PostingRole.UnmatchedReceipts, "220103"),
        new(PostingRole.InternalIssues, "5308"),
        new(PostingRole.CashOverShort, "5402"),
        new(PostingRole.CardClearing, "120404"),
        new(PostingRole.PosTillClearing, "120403"),
        new(PostingRole.CardFees, "5203"),
    ];

    private static ChartAccountRow H(string code, string? parent, AccountType type, string nameAr, string nameEn) =>
        new(code, nameAr, nameEn, parent, type, null, IsPosting: false, AllowsManualPosting: false, PartyType: null);

    private static ChartAccountRow P(
        string code, string parent, AccountType type, StatementSection section, string nameAr, string nameEn, bool manual = true,
        PartyType? party = null) =>
        new(code, nameAr, nameEn, parent, type, section, IsPosting: true, AllowsManualPosting: manual, PartyType: party);
}
