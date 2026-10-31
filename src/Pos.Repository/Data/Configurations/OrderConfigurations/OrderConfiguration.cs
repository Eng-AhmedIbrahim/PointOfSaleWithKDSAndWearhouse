namespace POS.Repository.Data.Configurations.OrderConfigurations;

public class OrderConfiguration : IEntityTypeConfiguration<Orders>
{
    public void Configure(EntityTypeBuilder<Orders> builder)
    {
        builder.HasKey(o => o.Id); // Set Id as the primary key

        builder.Property(o => o.Id)
            .ValueGeneratedOnAdd(); // Auto-incremented primary key

        builder.Property(o => o.OrderID)
            .ValueGeneratedNever();

        // Performance: Add indices for frequently searched fields
        builder.HasIndex(o => o.OrderID);
        builder.HasIndex(o => o.Phone1);
        builder.HasIndex(o => o.Phone2);
        builder.HasIndex(o => o.TakeawayCustomerPhone);
        builder.HasIndex(o => o.OrderDate);

        builder.Property(o => o.BranchName).HasMaxLength(100);
        builder.Property(o => o.CashierName).HasMaxLength(100);
        builder.Property(o => o.CashierID)
            .HasColumnType("nvarchar")
            .HasMaxLength(200);

        builder.Property(o => o.OrderType).HasMaxLength(50);
        builder.Property(o => o.OrderState).HasMaxLength(50);
        builder.Property(o => o.CustomerName).HasMaxLength(150);
        builder.Property(o => o.Phone1).HasMaxLength(15);
        builder.Property(o => o.Phone2).HasMaxLength(15);
        builder.Property(o => o.AddressNotice).HasMaxLength(500);
        builder.Property(o => o.ZoneName).HasMaxLength(100);
        builder.Property(o => o.DriverName).HasMaxLength(100);
        builder.Property(o => o.TableName).HasMaxLength(50);
        builder.Property(o => o.WaiterName).HasMaxLength(100);
        builder.Property(o => o.OrderNotice).HasMaxLength(500);
        builder.Property(o => o.VoidReason).HasMaxLength(500);
        builder.Property(o => o.PaymentMethod).HasMaxLength(50);
        builder.Property(o => o.OrderType).HasMaxLength(50);
        builder.Property(o => o.TakerID).HasColumnType("nvarchar").HasMaxLength(200);
        builder.Property(o => o.TakerName).HasMaxLength(200);
        builder.Property(o => o.ShiftID).IsRequired(false);

        builder.Property(o => o.DeliveryCompany).HasMaxLength(100);
        builder.Property(o => o.TakeawayCustomerName).HasMaxLength(50);
        builder.Property(o => o.TakeawayCustomerPhone).HasMaxLength(20);
        builder.Property(o => o.StreetName).HasMaxLength(100);
        builder.Property(o => o.FloorNum).HasMaxLength(5);
        builder.Property(o => o.ApartmentNum).HasMaxLength(5);
        builder.Property(o => o.HomeNum).HasMaxLength(5);
        builder.Property(o => o.DiscountType).HasMaxLength(20);
        builder.Property(o => o.DiscountReason).HasMaxLength(250);
        builder.Property(o => o.VoidByName).HasMaxLength(70);
        builder.Property(o => o.VoidBy).HasColumnType("nvarchar").HasMaxLength(100);


        builder.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Tax).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Service).HasColumnType("decimal(18,2)");
        builder.Property(o => o.DeliveryFees).HasColumnType("decimal(18,2)");
        builder.Property(o => o.GrandTotal).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Paid).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Remain).HasColumnType("decimal(18,2)");
        builder.Property(o => o.VoidAmount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.DiscountBy).HasColumnType("nvarchar").HasMaxLength(100);
        builder.Property(o => o.MachineName).HasMaxLength(100);
        builder.Property(o => o.CaptainTipsDeduction).HasColumnType("decimal(18,2)");
        builder.Property(o => o.CallCenterOrderId).HasColumnName("CentralOrderID");

        builder.HasMany(o => o.OrderDetails)
               .WithOne(od => od.Order)
               .HasForeignKey(od => od.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.PaymentMethod)
            .HasConversion<string>()
            .HasDefaultValue(PaymentMethod.Cash);

        builder.Property(o => o.OrderType)
            .HasConversion<string>()
            .HasDefaultValue(OrderTypes.TakeAway);


        builder.Property(o => o.OrderState)
        .HasConversion<string>()
           .HasDefaultValue(OrderStates.Pending);

        builder.HasOne(o => o.Shift)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.ShiftID)
            .OnDelete(DeleteBehavior.NoAction);

    }
}