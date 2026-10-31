namespace Pos.Repository.Data.Configurations.DineInConfigurations
{
    internal class TableGroupConfiguration : IEntityTypeConfiguration<TableGroup>
    {
        public void Configure(EntityTypeBuilder<TableGroup> builder)
        {
            builder.ToTable("TableGroups");

            builder.HasKey(tg => tg.Id);

            builder.Property(tg => tg.Id)
                .ValueGeneratedNever();

            builder.Property(tg => tg.GroupName)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(tg => tg.CreationDate)
                .HasColumnType("datetime")
                .IsRequired(false);

            builder.HasMany(t=>t.Tables)
                .WithOne(tg=>tg.TableGroup)
                .HasForeignKey(t=>t.GroupID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
