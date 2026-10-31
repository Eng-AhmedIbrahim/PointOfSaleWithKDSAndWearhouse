using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Core.Entities.Item;

namespace POS.Repository.Data.Configurations.ItemConfigurations;

public class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.Property(u => u.ArabicName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.EnglishName)
            .HasMaxLength(100);

        builder.Property(u => u.Code)
            .HasMaxLength(20);
    }
}
