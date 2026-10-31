namespace POS.Repository.Data.Configurations.OrderConfigurations
{
    public class OrderItemsDetailsConfiguration : IEntityTypeConfiguration<OrderItemsDetails>
    {
        public void Configure(EntityTypeBuilder<OrderItemsDetails> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrderType)
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(o => o.Quantity).HasDefaultValue(1m);

            builder.Property(o => o.TotalDiscountPrice).HasColumnType("decimal(18,2)");
            builder.Property(o => o.TotalDiscountPercentage).HasColumnType("decimal(18,2)");
            builder.Property(o => o.TotalDiscountAmount).HasColumnType("decimal(18,2)");
            builder.Property(o => o.TotalAfterDiscount).HasColumnType("decimal(18,2)");
            builder.Property(o => o.DiscountReason).HasMaxLength(250);
            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(o => o.MenuSalesItemId).HasColumnType("int");
            builder.Property(o => o.VoidAmount).HasColumnType("decimal(18,2)");
            
            builder.Property(o => o.ItemName).HasMaxLength(200);
            builder.Property(o => o.ItemNameAr).HasMaxLength(200);
            builder.Property(o => o.CategoryName).HasMaxLength(150);
            builder.Property(o => o.UnitPrice).HasColumnType("decimal(18,2)");

            builder.HasOne(o => o.Order)
               .WithMany(o => o.OrderDetails)
               .HasForeignKey(o => o.OrderId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(o => o.MenuSalesItem)
                   .WithMany(o=>o.OrderDetails)
                   .HasForeignKey(o => o.MenuSalesItemId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.OrderItemAttributes)
                   .WithOne(a => a.OrderItem)
                   .HasForeignKey(a => a.OrderItemId)
                   .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(o => o.OrderItemComments)
                   .WithOne(c => c.OrderItemDetail)
                   .HasForeignKey(c => c.OrderItemDetailId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

