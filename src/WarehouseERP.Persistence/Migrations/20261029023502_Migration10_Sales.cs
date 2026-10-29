using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration10_Sales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CustomerType = table.Column<byte>(type: "tinyint", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NameArKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEnKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    TaxRegistrationNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    CommercialRegistrationNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    CommercialRegistry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CommercialRegistryKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MobileKey = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    IsOnCreditHold = table.Column<bool>(type: "bit", nullable: false),
                    CreditHoldReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreditChangedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreditChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.CheckConstraint("CK_Customers_CommercialRegistration", "[CommercialRegistrationNumber] IS NULL OR [CustomerType] = 2");
                    table.CheckConstraint("CK_Customers_CreditHold", "[IsOnCreditHold] = 0 OR [CreditHoldReason] IS NOT NULL");
                    table.CheckConstraint("CK_Customers_CreditLimit", "[CreditLimit] IS NULL OR [CreditLimit] >= 0");
                    table.CheckConstraint("CK_Customers_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
                    table.CheckConstraint("CK_Customers_Type", "[CustomerType] IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_Customers_Countries_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "organization",
                        principalTable: "Countries",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CustomersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "DocumentNumberSequences",
                schema: "sales",
                columns: table => new
                {
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: false),
                    LastNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentNumberSequences", x => new { x.DocumentType, x.BranchId, x.FiscalYear });
                    table.CheckConstraint("CK_DocumentNumberSequences_LastNumber", "[LastNumber] > 0");
                    table.CheckConstraint("CK_DocumentNumberSequences_Type", "[DocumentType] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_SalesNumberSequences_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                schema: "sales",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    Fingerprint = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "SalesPrices",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesPrices", x => x.Id);
                    table.CheckConstraint("CK_SalesPrices_UnitPrice", "[UnitPrice] > 0");
                    table.ForeignKey(
                        name: "FK_SalesPrices_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesPrices_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SalesPricesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "SalesSettings",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    QuotationsRequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    OrdersRequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    DiscountApprovalThreshold = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    ManualPriceRequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    PriceBelowListRequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    CreditHoldPolicy = table.Column<byte>(type: "tinyint", nullable: false),
                    CreditHoldBlocksDeliveries = table.Column<bool>(type: "bit", nullable: false),
                    DefaultVatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    QuotationValidityDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesSettings", x => x.Id);
                    table.CheckConstraint("CK_SalesSettings_CreditHoldPolicy", "[CreditHoldPolicy] IN (1, 2)");
                    table.CheckConstraint("CK_SalesSettings_DefaultVatRate", "[DefaultVatRate] > 0 AND [DefaultVatRate] <= 100");
                    table.CheckConstraint("CK_SalesSettings_DiscountThreshold", "[DiscountApprovalThreshold] IS NULL OR ([DiscountApprovalThreshold] >= 0 AND [DiscountApprovalThreshold] < 100)");
                    table.CheckConstraint("CK_SalesSettings_QuotationValidity", "[QuotationValidityDays] BETWEEN 1 AND 365");
                    table.CheckConstraint("CK_SalesSettings_Single", "[Id] = 1");
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressType = table.Column<byte>(type: "tinyint", nullable: false),
                    Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.CheckConstraint("CK_CustomerAddresses_Type", "[AddressType] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Cities_CityId",
                        column: x => x.CityId,
                        principalSchema: "organization",
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CustomerAddressesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContacts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CustomerContactsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "SalesDeliveries",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockDocumentNumber = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    CostAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDeliveries", x => x.Id);
                    table.CheckConstraint("CK_SalesDeliveries_Posted", "([Status] = 2 AND [Number] IS NOT NULL AND [StockDocumentId] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) OR ([Status] <> 2 AND [Number] IS NULL AND [StockDocumentId] IS NULL)");
                    table.CheckConstraint("CK_SalesDeliveries_Status", "[Status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_SalesDeliveries_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveries_StockDocuments_StockDocumentId",
                        column: x => x.StockDocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveries_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesDeliveryEvents",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDeliveryEvents", x => x.Id);
                    table.CheckConstraint("CK_SalesDeliveryEvents_Action", "[Action] BETWEEN 1 AND 15");
                    table.ForeignKey(
                        name: "FK_SalesDeliveryEvents_SalesDeliveries_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "sales",
                        principalTable: "SalesDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesDeliveryLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDeliveryLines", x => x.Id);
                    table.CheckConstraint("CK_SalesDeliveryLines_Cost", "[CostAmount] IS NULL OR [CostAmount] >= 0");
                    table.CheckConstraint("CK_SalesDeliveryLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.CheckConstraint("CK_SalesDeliveryLines_Returned", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLines_SalesDeliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "sales",
                        principalTable: "SalesDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLines_StockDocumentLines_StockLineId",
                        column: x => x.StockLineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDeliveryLines_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceEvents",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceEvents", x => x.Id);
                    table.CheckConstraint("CK_SalesInvoiceEvents_Action", "[Action] BETWEEN 1 AND 15");
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    PriceSource = table.Column<byte>(type: "tinyint", nullable: false),
                    ListUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxTreatment = table.Column<byte>(type: "tinyint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_SalesInvoiceLines_Price", "[UnitPrice] > 0 AND [PriceSource] IN (1, 2) AND [DiscountPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_SalesInvoiceLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.CheckConstraint("CK_SalesInvoiceLines_Tax", "([TaxTreatment] = 1 AND [TaxRate] > 0 AND [TaxRate] <= 100) OR ([TaxTreatment] IN (2, 3) AND [TaxRate] = 0)");
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.Id);
                    table.CheckConstraint("CK_SalesInvoices_Dates", "[DueDate] >= [InvoiceDate]");
                    table.CheckConstraint("CK_SalesInvoices_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
                    table.CheckConstraint("CK_SalesInvoices_Posted", "([Status] = 2 AND [Number] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) OR ([Status] <> 2 AND [Number] IS NULL)");
                    table.CheckConstraint("CK_SalesInvoices_Status", "[Status] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderEvents",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderEvents", x => x.Id);
                    table.CheckConstraint("CK_SalesOrderEvents_Action", "[Action] BETWEEN 1 AND 15");
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeliveredQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ClosedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReturnedInvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoicedNetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    InvoicedTaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    PriceSource = table.Column<byte>(type: "tinyint", nullable: false),
                    ListUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxTreatment = table.Column<byte>(type: "tinyint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderLines", x => x.Id);
                    table.CheckConstraint("CK_SalesOrderLines_Delivered", "[DeliveredQuantity] >= 0 AND [ClosedQuantity] >= 0 AND [DeliveredQuantity] + [ClosedQuantity] <= [Quantity]");
                    table.CheckConstraint("CK_SalesOrderLines_Invoiced", "[InvoicedQuantity] >= 0 AND [InvoicedQuantity] + [ReturnedQuantity] - [ReturnedInvoicedQuantity] <= [DeliveredQuantity]");
                    table.CheckConstraint("CK_SalesOrderLines_Price", "[UnitPrice] > 0 AND [PriceSource] IN (1, 2) AND [DiscountPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_SalesOrderLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.CheckConstraint("CK_SalesOrderLines_Returned", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [DeliveredQuantity] AND [ReturnedInvoicedQuantity] >= 0 AND [ReturnedInvoicedQuantity] <= [ReturnedQuantity]");
                    table.CheckConstraint("CK_SalesOrderLines_Tax", "([TaxTreatment] = 1 AND [TaxRate] > 0 AND [TaxRate] <= 100) OR ([TaxTreatment] IN (2, 3) AND [TaxRate] = 0)");
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrderLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CustomerReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    ShippingAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceQuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    InvoicedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditedAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ApprovalReasonFlags = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ClosedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.CheckConstraint("CK_SalesOrders_Amounts", "[InvoicedAmount] >= 0 AND [CreditedAmount] >= 0");
                    table.CheckConstraint("CK_SalesOrders_Number", "([Status] IN (3, 4, 5, 6) AND [Number] IS NOT NULL AND [SequenceNumber] IS NOT NULL) OR ([Status] IN (1, 2) AND [Number] IS NULL) OR [Status] = 7");
                    table.CheckConstraint("CK_SalesOrders_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
                    table.CheckConstraint("CK_SalesOrders_Status", "[Status] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_SalesOrders_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotations",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    QuotationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    ApprovalReasonFlags = table.Column<int>(type: "int", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DeclinedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConvertedOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotations", x => x.Id);
                    table.CheckConstraint("CK_SalesQuotations_Converted", "([Status] = 4 AND [ConvertedOrderId] IS NOT NULL) OR ([Status] <> 4 AND [ConvertedOrderId] IS NULL)");
                    table.CheckConstraint("CK_SalesQuotations_Number", "([Status] IN (3, 4, 5) AND [Number] IS NOT NULL AND [SequenceNumber] IS NOT NULL) OR ([Status] IN (1, 2) AND [Number] IS NULL) OR [Status] = 6");
                    table.CheckConstraint("CK_SalesQuotations_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
                    table.CheckConstraint("CK_SalesQuotations_Status", "[Status] BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_SalesQuotations_Validity", "[ValidUntil] >= [QuotationDate]");
                    table.ForeignKey(
                        name: "FK_SalesQuotations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_SalesOrders_ConvertedOrderId",
                        column: x => x.ConvertedOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturns",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    DeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockDocumentNumber = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    CostAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditNetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditTaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditTotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturns", x => x.Id);
                    table.CheckConstraint("CK_SalesReturns_Amounts", "[CostAmount] >= 0 AND [CreditNetAmount] >= 0 AND [CreditTaxAmount] >= 0 AND [CreditTotalAmount] = [CreditNetAmount] + [CreditTaxAmount]");
                    table.CheckConstraint("CK_SalesReturns_Approved", "([Status] = 3 AND [Number] IS NOT NULL AND [StockDocumentId] IS NOT NULL AND [ApprovedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [Number] IS NULL AND [StockDocumentId] IS NULL)");
                    table.CheckConstraint("CK_SalesReturns_Status", "[Status] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_SalesReturns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "sales",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_SalesDeliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalSchema: "sales",
                        principalTable: "SalesDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_SalesOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_StockDocuments_StockDocumentId",
                        column: x => x.StockDocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotationEvents",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationEvents", x => x.Id);
                    table.CheckConstraint("CK_SalesQuotationEvents_Action", "[Action] BETWEEN 1 AND 15");
                    table.ForeignKey(
                        name: "FK_SalesQuotationEvents_SalesQuotations_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "sales",
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesQuotationLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    PriceSource = table.Column<byte>(type: "tinyint", nullable: false),
                    ListUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxTreatment = table.Column<byte>(type: "tinyint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotationLines", x => x.Id);
                    table.CheckConstraint("CK_SalesQuotationLines_Price", "[UnitPrice] > 0 AND [PriceSource] IN (1, 2) AND [DiscountPercent] BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_SalesQuotationLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.CheckConstraint("CK_SalesQuotationLines_Tax", "([TaxTreatment] = 1 AND [TaxRate] > 0 AND [TaxRate] <= 100) OR ([TaxTreatment] IN (2, 3) AND [TaxRate] = 0)");
                    table.ForeignKey(
                        name: "FK_SalesQuotationLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotationLines_SalesQuotations_QuotationId",
                        column: x => x.QuotationId,
                        principalSchema: "sales",
                        principalTable: "SalesQuotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuotationLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnEvents",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<byte>(type: "tinyint", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnEvents", x => x.Id);
                    table.CheckConstraint("CK_SalesReturnEvents_Action", "[Action] BETWEEN 1 AND 15");
                    table.ForeignKey(
                        name: "FK_SalesReturnEvents_SalesReturns_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "sales",
                        principalTable: "SalesReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnLines",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    DeliveryLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Condition = table.Column<byte>(type: "tinyint", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CostAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    UninvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreditNetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditTaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnLines", x => x.Id);
                    table.CheckConstraint("CK_SalesReturnLines_Condition", "[Condition] = 1");
                    table.CheckConstraint("CK_SalesReturnLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.CheckConstraint("CK_SalesReturnLines_Split", "[UninvoicedQuantity] >= 0 AND [InvoicedQuantity] >= 0 AND [CreditNetAmount] >= 0 AND [CreditTaxAmount] >= 0 AND ([StockLineId] IS NULL OR [UninvoicedQuantity] + [InvoicedQuantity] = [Quantity])");
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_SalesDeliveryLines_DeliveryLineId",
                        column: x => x.DeliveryLineId,
                        principalSchema: "sales",
                        principalTable: "SalesDeliveryLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_SalesOrderLines_OrderLineId",
                        column: x => x.OrderLineId,
                        principalSchema: "sales",
                        principalTable: "SalesOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_SalesReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalSchema: "sales",
                        principalTable: "SalesReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_StockDocumentLines_StockLineId",
                        column: x => x.StockLineId,
                        principalSchema: "inventory",
                        principalTable: "StockDocumentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "sales",
                table: "SalesSettings",
                columns: new[] { "Id", "CreatedAtUtc", "CreatedByUserId", "CreditHoldBlocksDeliveries", "CreditHoldPolicy", "DefaultVatRate", "DiscountApprovalThreshold", "ManualPriceRequiresApproval", "OrdersRequireApproval", "PriceBelowListRequiresApproval", "QuotationValidityDays", "QuotationsRequireApproval", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[] { 1, new DateTime(2026, 10, 29, 0, 0, 0, 0, DateTimeKind.Utc), null, true, (byte)1, 14m, null, false, false, false, 14, false, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CityId",
                schema: "sales",
                table: "CustomerAddresses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerId",
                schema: "sales",
                table: "CustomerAddresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                schema: "sales",
                table: "CustomerContacts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_MobileKey",
                schema: "sales",
                table: "Customers",
                column: "MobileKey");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NameArKey",
                schema: "sales",
                table: "Customers",
                column: "NameArKey");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NameEnKey",
                schema: "sales",
                table: "Customers",
                column: "NameEnKey");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_Code",
                schema: "sales",
                table: "Customers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Customers_CommercialRegistration",
                schema: "sales",
                table: "Customers",
                columns: new[] { "CountryCode", "CommercialRegistryKey", "CommercialRegistrationNumber" },
                unique: true,
                filter: "[CommercialRegistrationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_TaxNumber",
                schema: "sales",
                table: "Customers",
                columns: new[] { "CountryCode", "TaxRegistrationNumber" },
                unique: true,
                filter: "[TaxRegistrationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentNumberSequences_BranchId",
                schema: "sales",
                table: "DocumentNumberSequences",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_SubjectId",
                schema: "sales",
                table: "IdempotencyRecords",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveries_BranchId_FiscalYear_SequenceNumber",
                schema: "sales",
                table: "SalesDeliveries",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveries_CreatedAtUtc",
                schema: "sales",
                table: "SalesDeliveries",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveries_CustomerId",
                schema: "sales",
                table: "SalesDeliveries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveries_DraftNumber",
                schema: "sales",
                table: "SalesDeliveries",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveries_Number",
                schema: "sales",
                table: "SalesDeliveries",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveries_OrderId",
                schema: "sales",
                table: "SalesDeliveries",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveries_WarehouseId_Status",
                schema: "sales",
                table: "SalesDeliveries",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_SalesDeliveries_StockDocument",
                schema: "sales",
                table: "SalesDeliveries",
                column: "StockDocumentId",
                unique: true,
                filter: "[StockDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryEvents_DocumentId",
                schema: "sales",
                table: "SalesDeliveryEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLines_DeliveryId_LineNumber",
                schema: "sales",
                table: "SalesDeliveryLines",
                columns: new[] { "DeliveryId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLines_LocationId",
                schema: "sales",
                table: "SalesDeliveryLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLines_OrderLineId",
                schema: "sales",
                table: "SalesDeliveryLines",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLines_ProductId",
                schema: "sales",
                table: "SalesDeliveryLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLines_ProductUnitId",
                schema: "sales",
                table: "SalesDeliveryLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLines_StockLineId",
                schema: "sales",
                table: "SalesDeliveryLines",
                column: "StockLineId",
                unique: true,
                filter: "[StockLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDeliveryLines_UnitId",
                schema: "sales",
                table: "SalesDeliveryLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceEvents_DocumentId",
                schema: "sales",
                table: "SalesInvoiceEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_InvoiceId_LineNumber",
                schema: "sales",
                table: "SalesInvoiceLines",
                columns: new[] { "InvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_OrderLineId",
                schema: "sales",
                table: "SalesInvoiceLines",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_ProductId",
                schema: "sales",
                table: "SalesInvoiceLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_ProductUnitId",
                schema: "sales",
                table: "SalesInvoiceLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_UnitId",
                schema: "sales",
                table: "SalesInvoiceLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_BranchId_FiscalYear_SequenceNumber",
                schema: "sales",
                table: "SalesInvoices",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CreatedAtUtc",
                schema: "sales",
                table: "SalesInvoices",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CustomerId_Status",
                schema: "sales",
                table: "SalesInvoices",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_DraftNumber",
                schema: "sales",
                table: "SalesInvoices",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_Number",
                schema: "sales",
                table: "SalesInvoices",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_OrderId",
                schema: "sales",
                table: "SalesInvoices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_WarehouseId_Status",
                schema: "sales",
                table: "SalesInvoices",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderEvents_DocumentId",
                schema: "sales",
                table: "SalesOrderEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_OrderId_LineNumber",
                schema: "sales",
                table: "SalesOrderLines",
                columns: new[] { "OrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_ProductId",
                schema: "sales",
                table: "SalesOrderLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_ProductUnitId",
                schema: "sales",
                table: "SalesOrderLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_UnitId",
                schema: "sales",
                table: "SalesOrderLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_BranchId_FiscalYear_SequenceNumber",
                schema: "sales",
                table: "SalesOrders",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CreatedAtUtc",
                schema: "sales",
                table: "SalesOrders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CustomerId_Status",
                schema: "sales",
                table: "SalesOrders",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_DraftNumber",
                schema: "sales",
                table: "SalesOrders",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_Number",
                schema: "sales",
                table: "SalesOrders",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_SourceQuotationId",
                schema: "sales",
                table: "SalesOrders",
                column: "SourceQuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_WarehouseId_Status",
                schema: "sales",
                table: "SalesOrders",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesPrices_ProductId",
                schema: "sales",
                table: "SalesPrices",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "UX_SalesPrices_ProductUnit",
                schema: "sales",
                table: "SalesPrices",
                column: "ProductUnitId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationEvents_DocumentId",
                schema: "sales",
                table: "SalesQuotationEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationLines_ProductId",
                schema: "sales",
                table: "SalesQuotationLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationLines_ProductUnitId",
                schema: "sales",
                table: "SalesQuotationLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationLines_QuotationId_LineNumber",
                schema: "sales",
                table: "SalesQuotationLines",
                columns: new[] { "QuotationId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotationLines_UnitId",
                schema: "sales",
                table: "SalesQuotationLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_BranchId_FiscalYear_SequenceNumber",
                schema: "sales",
                table: "SalesQuotations",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_ConvertedOrderId",
                schema: "sales",
                table: "SalesQuotations",
                column: "ConvertedOrderId",
                unique: true,
                filter: "[ConvertedOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_CreatedAtUtc",
                schema: "sales",
                table: "SalesQuotations",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_CustomerId",
                schema: "sales",
                table: "SalesQuotations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_DraftNumber",
                schema: "sales",
                table: "SalesQuotations",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_Number",
                schema: "sales",
                table: "SalesQuotations",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotations_WarehouseId_Status",
                schema: "sales",
                table: "SalesQuotations",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnEvents_DocumentId",
                schema: "sales",
                table: "SalesReturnEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_DeliveryLineId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "DeliveryLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_LocationId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_OrderLineId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_ProductId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_ProductUnitId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_ReturnId_LineNumber",
                schema: "sales",
                table: "SalesReturnLines",
                columns: new[] { "ReturnId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_StockLineId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "StockLineId",
                unique: true,
                filter: "[StockLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_UnitId",
                schema: "sales",
                table: "SalesReturnLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_BranchId_FiscalYear_SequenceNumber",
                schema: "sales",
                table: "SalesReturns",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CreatedAtUtc",
                schema: "sales",
                table: "SalesReturns",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CustomerId",
                schema: "sales",
                table: "SalesReturns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_DeliveryId",
                schema: "sales",
                table: "SalesReturns",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_DraftNumber",
                schema: "sales",
                table: "SalesReturns",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_Number",
                schema: "sales",
                table: "SalesReturns",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_OrderId",
                schema: "sales",
                table: "SalesReturns",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_WarehouseId_Status",
                schema: "sales",
                table: "SalesReturns",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_SalesReturns_StockDocument",
                schema: "sales",
                table: "SalesReturns",
                column: "StockDocumentId",
                unique: true,
                filter: "[StockDocumentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesDeliveries_SalesOrders_OrderId",
                schema: "sales",
                table: "SalesDeliveries",
                column: "OrderId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesDeliveryLines_SalesOrderLines_OrderLineId",
                schema: "sales",
                table: "SalesDeliveryLines",
                column: "OrderLineId",
                principalSchema: "sales",
                principalTable: "SalesOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceEvents_SalesInvoices_DocumentId",
                schema: "sales",
                table: "SalesInvoiceEvents",
                column: "DocumentId",
                principalSchema: "sales",
                principalTable: "SalesInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceLines_SalesInvoices_InvoiceId",
                schema: "sales",
                table: "SalesInvoiceLines",
                column: "InvoiceId",
                principalSchema: "sales",
                principalTable: "SalesInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceLines_SalesOrderLines_OrderLineId",
                schema: "sales",
                table: "SalesInvoiceLines",
                column: "OrderLineId",
                principalSchema: "sales",
                principalTable: "SalesOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_SalesOrders_OrderId",
                schema: "sales",
                table: "SalesInvoices",
                column: "OrderId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderEvents_SalesOrders_DocumentId",
                schema: "sales",
                table: "SalesOrderEvents",
                column: "DocumentId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderLines_SalesOrders_OrderId",
                schema: "sales",
                table: "SalesOrderLines",
                column: "OrderId",
                principalSchema: "sales",
                principalTable: "SalesOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_SalesQuotations_SourceQuotationId",
                schema: "sales",
                table: "SalesOrders",
                column: "SourceQuotationId",
                principalSchema: "sales",
                principalTable: "SalesQuotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Customers_CustomerId",
                schema: "sales",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotations_Customers_CustomerId",
                schema: "sales",
                table: "SalesQuotations");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesQuotations_SalesOrders_ConvertedOrderId",
                schema: "sales",
                table: "SalesQuotations");

            migrationBuilder.DropTable(
                name: "CustomerAddresses",
                schema: "sales")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CustomerAddressesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "CustomerContacts",
                schema: "sales")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CustomerContactsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "DocumentNumberSequences",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesDeliveryEvents",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesInvoiceEvents",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesInvoiceLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesOrderEvents",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesPrices",
                schema: "sales")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SalesPricesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "SalesQuotationEvents",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesQuotationLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesReturnEvents",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesReturnLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesSettings",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesInvoices",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesDeliveryLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesReturns",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesOrderLines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesDeliveries",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "sales")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CustomersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "SalesOrders",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "SalesQuotations",
                schema: "sales");
        }
    }
}
