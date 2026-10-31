namespace POS.Repository.Data.Configurations.DineInConfigurations;

internal class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("Tables");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TableName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(t => t.TableState)
            .HasConversion<string>()
            .IsRequired()
            .HasDefaultValue(TableState.Available);

        builder.Property(t => t.SeatsCount)
            .IsRequired(false);

        builder.Property(t => t.LocationX)
            .IsRequired(false);

        builder.Property(t => t.LocationY)
            .IsRequired(false);

        builder.Property(t => t.SmokingSection)
            .IsRequired(false);

        builder.Property(t => t.NearWindows)
            .IsRequired(false);

        builder.Property(t => t.BoothSeating)
            .IsRequired(false);

        builder.Property(t => t.PrivateSeating)
            .IsRequired(false);

        builder.Property(t => t.LoadIndex)
            .IsRequired(false);

        builder.Property(t => t.Usable)
            .IsRequired(false)
            .HasDefaultValue(true);

        builder.Property(t => t.TableShape)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(t => t.TimeStamp)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(t => t.ImageUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasOne(t => t.TableGroup)
            .WithMany(tg => tg.Tables)
            .HasForeignKey(t => t.GroupID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
