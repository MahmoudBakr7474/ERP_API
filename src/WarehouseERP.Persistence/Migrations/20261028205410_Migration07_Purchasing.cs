using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WarehouseERP.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Migration07_Purchasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.EnsureSchema(
                name: "purchasing");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceDocumentId",
                schema: "inventory",
                table: "StockDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentNumberSequences",
                schema: "purchasing",
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
                        name: "FK_PurchasingNumberSequences_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentPolicies",
                schema: "purchasing",
                columns: table => new
                {
                    DocumentType = table.Column<byte>(type: "tinyint", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPolicies", x => x.DocumentType);
                    table.CheckConstraint("CK_DocumentPolicies_Type", "[DocumentType] IN (1, 2)");
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                schema: "purchasing",
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
                name: "PurchaseRequests",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    NeededByDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequests", x => x.Id);
                    table.CheckConstraint("CK_PurchaseRequests_Number", "([Status] IN (3, 4, 5) AND [Number] IS NOT NULL AND [SequenceNumber] IS NOT NULL) OR ([Status] IN (1, 2) AND [Number] IS NULL) OR [Status] = 6");
                    table.CheckConstraint("CK_PurchaseRequests_Status", "[Status] BETWEEN 1 AND 6");
                    table.ForeignKey(
                        name: "FK_PurchaseRequests_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequests_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NameArKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEnKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    TaxRegistrationNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    CommercialRegistrationNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    CommercialRegistry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CommercialRegistryKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                    table.CheckConstraint("CK_Suppliers_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SuppliersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "PurchaseRequestEvents",
                schema: "purchasing",
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
                    table.PrimaryKey("PK_PurchaseRequestEvents", x => x.Id);
                    table.CheckConstraint("CK_PurchaseRequestEvents_Action", "[Action] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_PurchaseRequestEvents_PurchaseRequests_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequestLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequestLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseRequestLines_Ordered", "[OrderedQuantity] >= 0 AND [OrderedQuantity] <= [Quantity]");
                    table.CheckConstraint("CK_PurchaseRequestLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLines_PurchaseRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SupplierReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ClosedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrders_Number", "([Status] IN (3, 4, 5, 6) AND [Number] IS NOT NULL AND [SequenceNumber] IS NOT NULL) OR ([Status] IN (1, 2) AND [Number] IS NULL) OR [Status] = 7");
                    table.CheckConstraint("CK_PurchaseOrders_PaymentTerms", "[PaymentTermDays] BETWEEN 0 AND 365");
                    table.CheckConstraint("CK_PurchaseOrders_Status", "[Status] BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierAddresses",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressType = table.Column<byte>(type: "tinyint", nullable: false),
                    Line1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Line2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierAddresses", x => x.Id);
                    table.CheckConstraint("CK_SupplierAddresses_Type", "[AddressType] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_SupplierAddresses_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SupplierAddressesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "SupplierContacts",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_SupplierContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierContacts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SupplierContactsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.CreateTable(
                name: "GoodsReceipts",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierDeliveryNote = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockDocumentNumber = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_StockDocuments_StockDocumentId",
                        column: x => x.StockDocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipts_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderEvents",
                schema: "purchasing",
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
                    table.PrimaryKey("PK_PurchaseOrderEvents", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderEvents_Action", "[Action] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderEvents_PurchaseOrders_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxTreatment = table.Column<byte>(type: "tinyint", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    PurchaseRequestLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ReturnedInvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoicedNetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    InvoicedTaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderLines_Invoiced", "[InvoicedQuantity] >= 0 AND [InvoicedQuantity] + [ReturnedQuantity] - [ReturnedInvoicedQuantity] <= [ReceivedQuantity]");
                    table.CheckConstraint("CK_PurchaseOrderLines_Price", "[UnitPrice] >= 0 AND [DiscountPercent] BETWEEN 0 AND 100 AND [TaxRate] BETWEEN 0 AND 100 AND [TaxTreatment] = 1");
                    table.CheckConstraint("CK_PurchaseOrderLines_Quantity", "[Quantity] > 0 AND [ConversionFactor] > 0");
                    table.CheckConstraint("CK_PurchaseOrderLines_Received", "[ReceivedQuantity] >= 0 AND [ReceivedQuantity] <= [Quantity]");
                    table.CheckConstraint("CK_PurchaseOrderLines_Returned", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [ReceivedQuantity] AND [ReturnedInvoicedQuantity] >= 0 AND [ReturnedInvoicedQuantity] <= [ReturnedQuantity]");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseRequestLines_PurchaseRequestLineId",
                        column: x => x.PurchaseRequestLineId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseRequestLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoices",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    SupplierInvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierInvoiceNumberKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PriceVarianceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_SupplierInvoices", x => x.Id);
                    table.CheckConstraint("CK_SupplierInvoices_Dates", "[DueDate] >= [InvoiceDate]");
                    table.CheckConstraint("CK_SupplierInvoices_Posted", "([Status] = 3 AND [Number] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [Number] IS NULL)");
                    table.CheckConstraint("CK_SupplierInvoices_Status", "[Status] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_SupplierInvoices_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierInvoices_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierInvoices_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierInvoices_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturns",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    FiscalYear = table.Column<short>(type: "smallint", nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: true),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StockDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockDocumentNumber = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: true),
                    CreditClaimNetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditClaimTaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditClaimTotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturns", x => x.Id);
                    table.CheckConstraint("CK_SupplierReturns_Claim", "[CreditClaimNetAmount] >= 0 AND [CreditClaimTaxAmount] >= 0 AND [CreditClaimTotalAmount] = [CreditClaimNetAmount] + [CreditClaimTaxAmount]");
                    table.CheckConstraint("CK_SupplierReturns_Posted", "([Status] = 3 AND [Number] IS NOT NULL AND [StockDocumentId] IS NOT NULL AND [PostedAtUtc] IS NOT NULL) OR ([Status] <> 3 AND [Number] IS NULL AND [StockDocumentId] IS NULL)");
                    table.CheckConstraint("CK_SupplierReturns_Status", "[Status] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "organization",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_StockDocuments_StockDocumentId",
                        column: x => x.StockDocumentId,
                        principalSchema: "inventory",
                        principalTable: "StockDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "organization",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceiptLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceiptLines", x => x.Id);
                    table.CheckConstraint("CK_GoodsReceiptLines_Quantity", "[Quantity] > 0 AND [UnitCost] >= 0");
                    table.CheckConstraint("CK_GoodsReceiptLines_Returned", "[ReturnedQuantity] >= 0 AND [ReturnedQuantity] <= [Quantity]");
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_GoodsReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceiptLines_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoiceEvents",
                schema: "purchasing",
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
                    table.PrimaryKey("PK_SupplierInvoiceEvents", x => x.Id);
                    table.CheckConstraint("CK_SupplierInvoiceEvents_Action", "[Action] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceEvents_SupplierInvoices_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "purchasing",
                        principalTable: "SupplierInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierInvoiceLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxTreatment = table.Column<byte>(type: "tinyint", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    OrderNetUnitPrice = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    PriceVarianceAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierInvoiceLines", x => x.Id);
                    table.CheckConstraint("CK_SupplierInvoiceLines_Price", "[UnitPrice] >= 0 AND [DiscountPercent] BETWEEN 0 AND 100 AND [TaxRate] BETWEEN 0 AND 100 AND [TaxTreatment] = 1");
                    table.CheckConstraint("CK_SupplierInvoiceLines_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLines_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierInvoiceLines_SupplierInvoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "purchasing",
                        principalTable: "SupplierInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnEvents",
                schema: "purchasing",
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
                    table.PrimaryKey("PK_SupplierReturnEvents", x => x.Id);
                    table.CheckConstraint("CK_SupplierReturnEvents_Action", "[Action] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_SupplierReturnEvents_SupplierReturns_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "purchasing",
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnLines",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptUnitCost = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    UninvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoicedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreditClaimNetAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    CreditClaimTaxAmount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierReturnLines", x => x.Id);
                    table.CheckConstraint("CK_SupplierReturnLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_SupplierReturnLines_Split", "[UninvoicedQuantity] >= 0 AND [InvoicedQuantity] >= 0 AND ([UninvoicedQuantity] + [InvoicedQuantity] = 0 OR [UninvoicedQuantity] + [InvoicedQuantity] = [Quantity])");
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalSchema: "purchasing",
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_ProductUnits_ProductUnitId",
                        column: x => x.ProductUnitId,
                        principalSchema: "catalog",
                        principalTable: "ProductUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_PurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_SupplierReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalSchema: "purchasing",
                        principalTable: "SupplierReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierReturnLines_WarehouseLocations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "organization",
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "purchasing",
                table: "DocumentPolicies",
                columns: new[] { "DocumentType", "CreatedAtUtc", "CreatedByUserId", "RequiresApproval", "UpdatedAtUtc", "UpdatedByUserId" },
                values: new object[,]
                {
                    { (byte)1, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null },
                    { (byte)2, new DateTime(2026, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "UX_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments",
                column: "SourceDocumentId",
                unique: true,
                filter: "[SourceDocumentId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments",
                sql: "([DocumentType] IN (9, 10) AND [SourceDocumentId] IS NOT NULL) OR ([DocumentType] NOT IN (9, 10) AND [SourceDocumentId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments",
                sql: "[DocumentType] BETWEEN 1 AND 10");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentNumberSequences_BranchId",
                schema: "purchasing",
                table: "DocumentNumberSequences",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_LocationId",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_ProductId",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_ProductUnitId",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_PurchaseOrderLineId",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptLines_ReceiptId_LineNumber",
                schema: "purchasing",
                table: "GoodsReceiptLines",
                columns: new[] { "ReceiptId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_BranchId_FiscalYear_SequenceNumber",
                schema: "purchasing",
                table: "GoodsReceipts",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_Number",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_SupplierId",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_WarehouseId_ReceivedAtUtc",
                schema: "purchasing",
                table: "GoodsReceipts",
                columns: new[] { "WarehouseId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_GoodsReceipts_StockDocument",
                schema: "purchasing",
                table: "GoodsReceipts",
                column: "StockDocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_SubjectId",
                schema: "purchasing",
                table: "IdempotencyRecords",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderEvents_DocumentId",
                schema: "purchasing",
                table: "PurchaseOrderEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_OrderId_LineNumber",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                columns: new[] { "OrderId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_ProductId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_ProductUnitId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseRequestLineId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                column: "PurchaseRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_UnitId",
                schema: "purchasing",
                table: "PurchaseOrderLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BranchId_FiscalYear_SequenceNumber",
                schema: "purchasing",
                table: "PurchaseOrders",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedAtUtc",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_DraftNumber",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Number",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "purchasing",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_WarehouseId_Status",
                schema: "purchasing",
                table: "PurchaseOrders",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestEvents_DocumentId",
                schema: "purchasing",
                table: "PurchaseRequestEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLines_ProductId",
                schema: "purchasing",
                table: "PurchaseRequestLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLines_ProductUnitId",
                schema: "purchasing",
                table: "PurchaseRequestLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLines_RequestId_LineNumber",
                schema: "purchasing",
                table: "PurchaseRequestLines",
                columns: new[] { "RequestId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestLines_UnitId",
                schema: "purchasing",
                table: "PurchaseRequestLines",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_BranchId_FiscalYear_SequenceNumber",
                schema: "purchasing",
                table: "PurchaseRequests",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_CreatedAtUtc",
                schema: "purchasing",
                table: "PurchaseRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_DraftNumber",
                schema: "purchasing",
                table: "PurchaseRequests",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_Number",
                schema: "purchasing",
                table: "PurchaseRequests",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequests_WarehouseId_Status",
                schema: "purchasing",
                table: "PurchaseRequests",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierAddresses_SupplierId",
                schema: "purchasing",
                table: "SupplierAddresses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierContacts_SupplierId",
                schema: "purchasing",
                table: "SupplierContacts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceEvents_DocumentId",
                schema: "purchasing",
                table: "SupplierInvoiceEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_InvoiceId_LineNumber",
                schema: "purchasing",
                table: "SupplierInvoiceLines",
                columns: new[] { "InvoiceId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_ProductId",
                schema: "purchasing",
                table: "SupplierInvoiceLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_ProductUnitId",
                schema: "purchasing",
                table: "SupplierInvoiceLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_PurchaseOrderLineId",
                schema: "purchasing",
                table: "SupplierInvoiceLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_BranchId_FiscalYear_SequenceNumber",
                schema: "purchasing",
                table: "SupplierInvoices",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_CreatedAtUtc",
                schema: "purchasing",
                table: "SupplierInvoices",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_DraftNumber",
                schema: "purchasing",
                table: "SupplierInvoices",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_Number",
                schema: "purchasing",
                table: "SupplierInvoices",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_PurchaseOrderId",
                schema: "purchasing",
                table: "SupplierInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_Status_DueDate",
                schema: "purchasing",
                table: "SupplierInvoices",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_WarehouseId",
                schema: "purchasing",
                table: "SupplierInvoices",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UX_SupplierInvoices_SupplierNumber",
                schema: "purchasing",
                table: "SupplierInvoices",
                columns: new[] { "SupplierId", "SupplierInvoiceNumberKey" },
                unique: true,
                filter: "[Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnEvents_DocumentId",
                schema: "purchasing",
                table: "SupplierReturnEvents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_GoodsReceiptLineId",
                schema: "purchasing",
                table: "SupplierReturnLines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_LocationId",
                schema: "purchasing",
                table: "SupplierReturnLines",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_ProductId",
                schema: "purchasing",
                table: "SupplierReturnLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_ProductUnitId",
                schema: "purchasing",
                table: "SupplierReturnLines",
                column: "ProductUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_PurchaseOrderLineId",
                schema: "purchasing",
                table: "SupplierReturnLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnLines_ReturnId_LineNumber",
                schema: "purchasing",
                table: "SupplierReturnLines",
                columns: new[] { "ReturnId", "LineNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_BranchId_FiscalYear_SequenceNumber",
                schema: "purchasing",
                table: "SupplierReturns",
                columns: new[] { "BranchId", "FiscalYear", "SequenceNumber" },
                unique: true,
                filter: "[SequenceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_CreatedAtUtc",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_DraftNumber",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "DraftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_GoodsReceiptId",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_Number",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "Number",
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_PurchaseOrderId",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_SupplierId",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturns_WarehouseId",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "UX_SupplierReturns_StockDocument",
                schema: "purchasing",
                table: "SupplierReturns",
                column: "StockDocumentId",
                unique: true,
                filter: "[StockDocumentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_NameArKey",
                schema: "purchasing",
                table: "Suppliers",
                column: "NameArKey");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_NameEnKey",
                schema: "purchasing",
                table: "Suppliers",
                column: "NameEnKey");

            migrationBuilder.CreateIndex(
                name: "UX_Suppliers_Code",
                schema: "purchasing",
                table: "Suppliers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Suppliers_CommercialRegistration",
                schema: "purchasing",
                table: "Suppliers",
                columns: new[] { "CountryCode", "CommercialRegistryKey", "CommercialRegistrationNumber" },
                unique: true,
                filter: "[CommercialRegistrationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Suppliers_TaxNumber",
                schema: "purchasing",
                table: "Suppliers",
                columns: new[] { "CountryCode", "TaxRegistrationNumber" },
                unique: true,
                filter: "[TaxRegistrationNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentNumberSequences",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "DocumentPolicies",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "IdempotencyRecords",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseOrderEvents",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseRequestEvents",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierAddresses",
                schema: "purchasing")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SupplierAddressesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "SupplierContacts",
                schema: "purchasing")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SupplierContactsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropTable(
                name: "SupplierInvoiceEvents",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierInvoiceLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierReturnEvents",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierReturnLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierInvoices",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "GoodsReceiptLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "SupplierReturns",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "GoodsReceipts",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseRequestLines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "PurchaseRequests",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "purchasing")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "SuppliersHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "history")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "ValidTo")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "ValidFrom");

            migrationBuilder.DropIndex(
                name: "UX_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Source",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.DropColumn(
                name: "SourceDocumentId",
                schema: "inventory",
                table: "StockDocuments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockDocuments_Type",
                schema: "inventory",
                table: "StockDocuments",
                sql: "[DocumentType] BETWEEN 1 AND 8");
        }
    }
}
