using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Core.Entities.OrderEntity;

namespace POS.Repository.Data.Configurations.OrderConfigurations;

public class OrderVoidConfiguration : IEntityTypeConfiguration<OrderVoid>
{
    public void Configure(EntityTypeBuilder<OrderVoid> builder)
    {
        builder.HasKey(v => v.Id);
        builder.ToTable("OrderVoids");

        builder.Property(v => v.Reason).HasMaxLength(500);
        builder.Property(v => v.VoidedBy).IsRequired().HasMaxLength(100);
        builder.Property(v => v.VoidedByName).HasMaxLength(100);
        
        // Decimals configurations
        builder.Property(v => v.SubtotalBefore).HasColumnType("decimal(18,2)");
        builder.Property(v => v.TaxBefore).HasColumnType("decimal(18,2)");
        builder.Property(v => v.ServiceBefore).HasColumnType("decimal(18,2)");
        builder.Property(v => v.DeliveryFeesBefore).HasColumnType("decimal(18,2)");
        builder.Property(v => v.DiscountBefore).HasColumnType("decimal(18,2)");
        builder.Property(v => v.GrandTotalBefore).HasColumnType("decimal(18,2)");

        builder.Property(v => v.SubtotalAfter).HasColumnType("decimal(18,2)");
        builder.Property(v => v.TaxAfter).HasColumnType("decimal(18,2)");
        builder.Property(v => v.ServiceAfter).HasColumnType("decimal(18,2)");
        builder.Property(v => v.DeliveryFeesAfter).HasColumnType("decimal(18,2)");
        builder.Property(v => v.DiscountAfter).HasColumnType("decimal(18,2)");
        builder.Property(v => v.GrandTotalAfter).HasColumnType("decimal(18,2)");

        builder.Property(v => v.TotalVoidedAmount).HasColumnType("decimal(18,2)");
        
        builder.Property(v => v.OrderType)
            .HasConversion<string>()
            .HasMaxLength(50);
            
        builder.Property(v => v.OrderStateAtVoid)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(v => v.Order)
            .WithMany()
            .HasForeignKey(v => v.OrderId)
            .OnDelete(DeleteBehavior.NoAction);
            
        builder.HasMany(v => v.VoidItems)
            .WithOne(vi => vi.OrderVoid)
            .HasForeignKey(vi => vi.OrderVoidId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderVoidItemConfiguration : IEntityTypeConfiguration<OrderVoidItem>
{
    public void Configure(EntityTypeBuilder<OrderVoidItem> builder)
    {
        builder.HasKey(vi => vi.Id);
        builder.ToTable("OrderVoidItems");

        builder.Property(vi => vi.AmountBefore).HasColumnType("decimal(18,2)");
        builder.Property(vi => vi.AmountVoided).HasColumnType("decimal(18,2)");
        builder.Property(vi => vi.AmountAfter).HasColumnType("decimal(18,2)");
        builder.Property(vi => vi.Reason).HasMaxLength(500);

        builder.HasOne(vi => vi.OrderVoid)
            .WithMany(v => v.VoidItems)
            .HasForeignKey(vi => vi.OrderVoidId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vi => vi.OrderItem)
            .WithMany()
            .HasForeignKey(vi => vi.OrderDetailId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
