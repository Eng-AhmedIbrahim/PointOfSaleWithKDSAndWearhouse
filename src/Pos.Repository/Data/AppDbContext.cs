namespace Pos.Repository.Data;

using POS.Core.Entities.Settings;
using POS.Core.Entities.Warehouse;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure decimal properties to avoid truncation warnings
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

            foreach (var property in properties)
            {
                property.SetColumnType("decimal(18,2)");
            }
        }

        modelBuilder.Entity<License>()
            .HasKey(l => new { l.CustomerID, l.BranchID, l.LicenseKey });

        // Warehouse Module Configurations - Restrict delete behaviors to avoid SQL Server cycles
        modelBuilder.Entity<StockBalance>(b =>
        {
            b.Ignore(sb => sb.QuantityAvailable);
            b.HasIndex(sb => new { sb.WarehouseId, sb.ProductId }).IsUnique();
            b.HasOne(sb => sb.Warehouse).WithMany(w => w.StockBalances).HasForeignKey(sb => sb.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(sb => sb.Product).WithMany(p => p.Balances).HasForeignKey(sb => sb.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Warehouse>(w =>
        {
            w.HasIndex(x => x.Code).IsUnique();
            w.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            w.HasOne(x => x.DefaultSupplyingWarehouse).WithMany().HasForeignKey(x => x.DefaultSupplyingWarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarehouseLocation>(l =>
        {
            l.HasOne(x => x.Warehouse).WithMany(w => w.Locations).HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WarehouseProduct>(p =>
        {
            p.HasIndex(x => x.SKU).IsUnique();
            p.HasOne(x => x.BaseUnit).WithMany().HasForeignKey(x => x.BaseUnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarehouseProductUnit>(pu =>
        {
            pu.HasOne(x => x.Product).WithMany(p => p.ProductUnits).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            pu.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BranchRequisition>(r =>
        {
            r.HasOne(x => x.RequestingBranch).WithMany().HasForeignKey(x => x.RequestingBranchId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.TargetWarehouse).WithMany().HasForeignKey(x => x.TargetWarehouseId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.ReroutedToWarehouse).WithMany().HasForeignKey(x => x.ReroutedToWarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BranchRequisitionLine>(rl =>
        {
            rl.HasOne(x => x.Requisition).WithMany(r => r.Lines).HasForeignKey(x => x.RequisitionId).OnDelete(DeleteBehavior.Cascade);
            rl.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            rl.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockLedger>(sl =>
        {
            sl.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            sl.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            sl.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrder>(po =>
        {
            po.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            po.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderLine>(pol =>
        {
            pol.HasOne(x => x.PurchaseOrder).WithMany(p => p.Lines).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            pol.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            pol.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceiptNote>(grn =>
        {
            grn.HasOne(x => x.PurchaseOrder).WithMany(po => po.GoodsReceiptNotes).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            grn.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            grn.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceiptNoteLine>(grnl =>
        {
            grnl.HasOne(x => x.GoodsReceiptNote).WithMany(g => g.Lines).HasForeignKey(x => x.GoodsReceiptNoteId).OnDelete(DeleteBehavior.Cascade);
            grnl.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            grnl.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierPriceContract>(spc =>
        {
            spc.HasOne(x => x.Supplier).WithMany(s => s.PriceContracts).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Cascade);
            spc.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            spc.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierInvoice>(si =>
        {
            si.HasOne(x => x.Supplier).WithMany(s => s.Invoices).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            si.HasOne(x => x.GoodsReceiptNote).WithMany().HasForeignKey(x => x.GoodsReceiptNoteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplierPayment>(sp =>
        {
            sp.HasOne(x => x.Supplier).WithMany(s => s.Payments).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public DbSet<AttributeItem> AttributeItems { get; set; }
    public DbSet<AttributeGroup> AttributeGroups { get; set; }
    public DbSet<Attributes> Attributes { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<KitchenType> KitchenTypes { get; set; }
    public DbSet<KitchenPrinters> KitchenPrinters { get; set; }
    public DbSet<MenuSalesItems> MenuSalesItems { get; set; }
    public DbSet<ItemsClassifications> ItemsClassifications { get; set; }
    public DbSet<OrderItemAttributes> OrderItemAttributes { get; set; }
    public DbSet<Orders> Orders { get; set; }
    public DbSet<OrderItemsDetails> OrdersDetails { get; set; }
    public DbSet<OrderItemComment> OrderItemComments { get; set; }
    public DbSet<OrderSetting> OrderSettings { get; set; }
    public DbSet<PrintingSettings> PrintingSettings { get; set; }
    public DbSet<TakeawayCustomer> TakeawayCustomers { get; set; }
    public DbSet<TableGroup> TableGroups { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<DineInOrder> DineInOrders { get; set; }
    public DbSet<AppDate> AppDate { get; set; }
    public DbSet<ShiftHandover> ShiftHandovers { get; set; }
    public DbSet<DeliveryCompanyInfo> DeliveryCompanyInfo { get; set; }
    public DbSet<DeliveryCustomerTitle> DeliveryCustomerTitle { get; set; }
    public DbSet<DeliveryZone> DeliveryZones { get; set; }
    public DbSet<CustomerAddress> CustomerAddress { get; set; }
    public DbSet<OrderTrack> OrderTracks { get; set; }
    public DbSet<POS.Core.Entities.ReservationEntity.Reservation> Reservations { get; set; }
    public DbSet<PosFeatureSetting> PosFeatureSettings { get; set; }
    public DbSet<POS.Core.Entities.ComplaintEntity.Complaint> Complaints { get; set; }
    public DbSet<OrderVoid> OrderVoids { get; set; }
    public DbSet<OrderVoidItem> OrderVoidItems { get; set; }
    public DbSet<POS.Core.Entities.Payment.PaymentMethodEntity> PaymentMethods { get; set; }
    public DbSet<POS.Core.Entities.Payment.Expense> Expenses { get; set; }
    public DbSet<POS.Core.Entities.Settings.DispatcherSetting> DispatcherSettings { get; set; }
    public DbSet<HqSetting> HqSettings { get; set; }
    public DbSet<LicensedDevice> LicensedDevices { get; set; }
    public DbSet<License> Licenses { get; set; }
    public DbSet<InventoryItem> InventoryItems { get; set; }

    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<InventoryTransactionImage> InventoryTransactionImages { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<ItemType> ItemTypes { get; set; }

    // Staff Meals
    public DbSet<POS.Core.Entities.StaffMealEntity.StaffMealConfig> StaffMealConfigs { get; set; }
    public DbSet<POS.Core.Entities.StaffMealEntity.StaffMealGroup> StaffMealGroups { get; set; }
    public DbSet<POS.Core.Entities.StaffMealEntity.StaffMealGroupItem> StaffMealGroupItems { get; set; }
    public DbSet<POS.Core.Entities.StaffMealEntity.StaffMealUsage> StaffMealUsages { get; set; }

    // KDS & Pickup Screens
    public DbSet<POS.Core.Entities.Kitchen.KdsScreen> KdsScreens { get; set; }

    // Warehouse Management System (WMS)
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<WarehouseLocation> WarehouseLocations { get; set; }
    public DbSet<WarehouseUnit> WarehouseUnits { get; set; }
    public DbSet<WarehouseProduct> WarehouseProducts { get; set; }
    public DbSet<WarehouseProductUnit> WarehouseProductUnits { get; set; }
    public DbSet<Supplier> WH_Suppliers { get; set; }
    public DbSet<SupplierPriceContract> SupplierPriceContracts { get; set; }
    public DbSet<SupplierInvoice> SupplierInvoices { get; set; }
    public DbSet<SupplierPayment> SupplierPayments { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes { get; set; }
    public DbSet<GoodsReceiptNoteLine> GoodsReceiptNoteLines { get; set; }
    public DbSet<BranchRequisition> BranchRequisitions { get; set; }
    public DbSet<BranchRequisitionLine> BranchRequisitionLines { get; set; }
    public DbSet<StockLedger> StockLedger { get; set; }
    public DbSet<StockBalance> StockBalances { get; set; }
}
