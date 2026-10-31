namespace POS.Repository.Data.Configurations.DineInConfigurations;

public class DineInOrderConfiguration : IEntityTypeConfiguration<DineInOrder>
{
    public void Configure(EntityTypeBuilder<DineInOrder> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd();

        builder.Property(d => d.OrderId)
            .IsRequired();

        builder.Property(d => d.BranchName)
            .HasMaxLength(100);

        builder.Property(d => d.CashierId)
            .HasMaxLength(200);

        builder.Property(d => d.CashierName)
            .HasMaxLength(150);

        builder.Property(d => d.CaptainId)
            .HasMaxLength(200);

        builder.Property(d => d.CaptainName)
            .HasMaxLength(150);

        builder.Property(d => d.TableName)
            .HasMaxLength(50);

        builder.Property(d => d.OrderState)
            .HasMaxLength(50)
            .HasDefaultValue("Open");

        builder.Property(d => d.DiscountType)
            .HasMaxLength(20);

        builder.Property(d => d.DiscountReason)
            .HasMaxLength(250);

        builder.Property(d => d.OrderNotice)
            .HasMaxLength(500);

        builder.Property(d => d.CustomerName)
            .HasMaxLength(150);

        builder.Property(d => d.CustomerPhone)
            .HasMaxLength(20);

        builder.Property(d => d.Subtotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.Tax)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.Service)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.DiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.DiscountPercentage)
            .HasColumnType("decimal(5,2)");

        builder.Property(d => d.TotalDiscount)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.GrandTotal)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.Paid)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.Remain)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.PaymentMethod)
            .HasConversion<string>()
            .HasDefaultValue(PaymentMethod.Cash);

        builder.HasMany(d => d.OrderDetails)
            .WithOne(od => od.DineInOrder)
            .HasForeignKey(od => od.DineInOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.OrderId);
        builder.HasIndex(d => d.TableId);
        builder.HasIndex(d => new { d.TableId, d.OrderState });
        builder.HasIndex(d => d.OrderDateTime);
    }
}
