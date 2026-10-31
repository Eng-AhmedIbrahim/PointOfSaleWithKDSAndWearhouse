using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Core.Entities.Item;

namespace POS.Repository.Data.Configurations.ItemConfigurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.HasOne(i => i.Unit)
            .WithMany()
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.MenuSalesItem)
            .WithMany()
            .HasForeignKey(i => i.MenuSalesItemId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Property(i => i.CurrentQuantity)
            .HasColumnType("decimal(18,4)");
            
        builder.Property(i => i.MinimumQuantity)
            .HasColumnType("decimal(18,4)");
    }
}
