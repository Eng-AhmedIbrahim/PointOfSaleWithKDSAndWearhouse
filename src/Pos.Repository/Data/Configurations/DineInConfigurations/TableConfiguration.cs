using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Core.Entities.DineIn;

namespace Pos.Repository.Data.Configurations.DineInConfigurations;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("Tables");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TableState)
            .IsRequired()
            .HasDefaultValue(TableState.Available)
            .HasConversion<string>();

        builder.Property(t => t.TableName)
            .HasMaxLength(100);

        builder.Property(t => t.TableShape)
            .HasMaxLength(50);

        builder.Property(t => t.TimeStamp)
            .HasMaxLength(50);

        builder.Property(t => t.ImageUrl)
            .HasMaxLength(500);

        builder.HasOne(t => t.TableGroup)
            .WithMany(tg => tg.Tables)
            .HasForeignKey(t => t.GroupID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
