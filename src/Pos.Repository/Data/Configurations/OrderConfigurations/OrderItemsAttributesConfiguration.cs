namespace POS.Repository.Data.Configurations.OrderConfigurations;

public class OrderItemsAttributesConfiguration : IEntityTypeConfiguration<OrderItemAttributes>
{
    public void Configure(EntityTypeBuilder<OrderItemAttributes> builder)
    {
        builder.HasKey(oia => new { oia.OrderItemId, oia.AttributeItemId });

        builder.HasOne(oia => oia.OrderItem)
               .WithMany(o => o.OrderItemAttributes)
               .HasForeignKey(oia => oia.OrderItemId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oia => oia.AttributeItem)
               .WithMany()
               .HasForeignKey(oia => oia.AttributeItemId)
               .IsRequired(false) // Make the relationship optional at EF level
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(oia => oia.AttributeName).HasColumnType("nvarchar(80)");
    }
}
