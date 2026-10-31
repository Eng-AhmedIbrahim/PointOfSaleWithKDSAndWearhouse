using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Core.Entities.Item;

namespace POS.Repository.Data.Configurations.ItemConfigurations;

public class ItemsClassificationsConfigurations : IEntityTypeConfiguration<ItemsClassifications>
{
    public void Configure(EntityTypeBuilder<ItemsClassifications> builder)
    {
        builder.HasKey(x => x.Id);
        
        // Disable Identity as requested
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ArabicName)
            .HasMaxLength(100);
    }
}
