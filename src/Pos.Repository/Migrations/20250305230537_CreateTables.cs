using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CreateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    NormalizedEnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KitchenTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    KitchenName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderStatment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Service = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tips = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    JobID = table.Column<int>(type: "int", nullable: true),
                    CustomerReceiptCount = table.Column<int>(type: "int", nullable: true),
                    FullKitchenReceiptCount = table.Column<int>(type: "int", nullable: true),
                    SeparateReceiptCount = table.Column<int>(type: "int", nullable: true),
                    ClosingReceiptCount = table.Column<int>(type: "int", nullable: true),
                    AddServiceToItemPrice = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TakeawayCustomers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TakeawayCustomers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LogoWidth = table.Column<int>(type: "int", nullable: false, defaultValue: 200),
                    LogoHeight = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone1 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Phone2 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Suspend = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PrintingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Copy1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Copy2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Copy3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Copy4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Copy5 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComputerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KitchenId = table.Column<int>(type: "int", nullable: true),
                    kitchenName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintingSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintingSettings_KitchenTypes_KitchenId",
                        column: x => x.KitchenId,
                        principalTable: "KitchenTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    OrderID = table.Column<int>(type: "int", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShiftID = table.Column<int>(type: "int", nullable: true),
                    CashierID = table.Column<int>(type: "int", nullable: true),
                    CashierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "TakeAway"),
                    OrderState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Pending"),
                    DeliveryCompany = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TakerID = table.Column<int>(type: "int", nullable: true),
                    TakerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerID = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone1 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Phone2 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    HomeNum = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    StreetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FloorNum = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    ApartmentNum = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    AddressNotice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ZoneID = table.Column<int>(type: "int", nullable: true),
                    ZoneName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ZoneBonus = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DispatchID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriverID = table.Column<int>(type: "int", nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BackTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TableID = table.Column<int>(type: "int", nullable: true),
                    TableName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TableState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaiterID = table.Column<int>(type: "int", nullable: true),
                    WaiterName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Service = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeliveryFees = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountedItems = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FreeItems = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "Cash"),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Paid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Remain = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReservationPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReservationRemain = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ScheduleDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerCount = table.Column<int>(type: "int", nullable: true),
                    DiscountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountBy = table.Column<int>(type: "int", nullable: true),
                    DiscountByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiscountTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    WithoutService = table.Column<bool>(type: "bit", nullable: true),
                    WithoutTax = table.Column<bool>(type: "bit", nullable: true),
                    WithoutDeliveryFees = table.Column<bool>(type: "bit", nullable: true),
                    OrderNotice = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VoidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoidBy = table.Column<int>(type: "int", nullable: true),
                    VoidByName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    VoidTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VoidCount = table.Column<int>(type: "int", nullable: true),
                    TotalVoid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PrintCount = table.Column<int>(type: "int", nullable: true),
                    KitchenOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PackingOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosingTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TakeawayCustomerId = table.Column<int>(type: "int", nullable: true),
                    TakeawayCustomerName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TakeawayCustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_TakeawayCustomers_TakeawayCustomerId",
                        column: x => x.TakeawayCustomerId,
                        principalTable: "TakeawayCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    NormalizedEnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    ItemsFont = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    Invisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BranchId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuSalesItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArabicName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    NormalizedEnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SecondPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThirdPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FourthPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FifthPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AttributePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MainCategoryId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    TextSize = table.Column<int>(type: "int", nullable: true),
                    Invisible = table.Column<bool>(type: "bit", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    HasAttribute = table.Column<bool>(type: "bit", nullable: false),
                    AttributeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuSalesItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuSalesItems_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MenuSalesItems_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MenuSalesItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttributeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppearanceIndex = table.Column<int>(type: "int", nullable: false),
                    AttributeId = table.Column<int>(type: "int", nullable: false),
                    RelatedMenuItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeItems_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttributeItems_MenuSalesItems_RelatedMenuItemId",
                        column: x => x.RelatedMenuItemId,
                        principalTable: "MenuSalesItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdersDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MenuSalesItemId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true, defaultValue: 1),
                    Discount = table.Column<bool>(type: "bit", nullable: true),
                    TotalDiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalDiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalAfterDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsVoided = table.Column<bool>(type: "bit", nullable: true),
                    VoidAmount = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MenuSalesItemsId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdersDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdersDetails_MenuSalesItems_MenuSalesItemId",
                        column: x => x.MenuSalesItemId,
                        principalTable: "MenuSalesItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdersDetails_MenuSalesItems_MenuSalesItemsId",
                        column: x => x.MenuSalesItemsId,
                        principalTable: "MenuSalesItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrdersDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemAttributes",
                columns: table => new
                {
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    AttributeItemId = table.Column<int>(type: "int", nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemAttributes", x => new { x.OrderItemId, x.AttributeItemId });
                    table.ForeignKey(
                        name: "FK_OrderItemAttributes_AttributeItems_AttributeItemId",
                        column: x => x.AttributeItemId,
                        principalTable: "AttributeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemAttributes_OrdersDetails_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrdersDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeItems_AttributeId",
                table: "AttributeItems",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeItems_RelatedMenuItemId",
                table: "AttributeItems",
                column: "RelatedMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId",
                table: "Branches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_BranchId",
                table: "Categories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenTypes_BranchId",
                table: "KitchenTypes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_AttributeId",
                table: "MenuSalesItems",
                column: "AttributeId",
                unique: true,
                filter: "[AttributeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_BranchId",
                table: "MenuSalesItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_CategoryId",
                table: "MenuSalesItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemAttributes_AttributeItemId",
                table: "OrderItemAttributes",
                column: "AttributeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TakeawayCustomerId",
                table: "Orders",
                column: "TakeawayCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdersDetails_MenuSalesItemId",
                table: "OrdersDetails",
                column: "MenuSalesItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdersDetails_MenuSalesItemsId",
                table: "OrdersDetails",
                column: "MenuSalesItemsId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdersDetails_OrderId",
                table: "OrdersDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintingSettings_BranchID",
                table: "PrintingSettings",
                column: "BranchID");

            migrationBuilder.CreateIndex(
                name: "IX_PrintingSettings_KitchenId",
                table: "PrintingSettings",
                column: "KitchenId");

            migrationBuilder.CreateIndex(
                name: "IX_TakeawayCustomers_CustomerPhone",
                table: "TakeawayCustomers",
                column: "CustomerPhone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemAttributes");

            migrationBuilder.DropTable(
                name: "OrderSettings");

            migrationBuilder.DropTable(
                name: "PrintingSettings");

            migrationBuilder.DropTable(
                name: "AttributeItems");

            migrationBuilder.DropTable(
                name: "OrdersDetails");

            migrationBuilder.DropTable(
                name: "KitchenTypes");

            migrationBuilder.DropTable(
                name: "MenuSalesItems");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Attributes");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "TakeawayCustomers");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
