namespace Pos.Repository.Data.Configurations.KitchenConfigurations;

public class KitchenPrintersConfiguration : IEntityTypeConfiguration<KitchenPrinters>
{
    public void Configure(EntityTypeBuilder<KitchenPrinters> builder)
    {
        builder.ToTable("KitchenPrinters");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.DeviceName)
               .HasMaxLength(100);

        builder.Property(p => p.Copy1).HasMaxLength(100);
        builder.Property(p => p.Copy2).HasMaxLength(100);
        builder.Property(p => p.Copy3).HasMaxLength(100);
        builder.Property(p => p.Copy4).HasMaxLength(100);
        builder.Property(p => p.Copy5).HasMaxLength(100);
        builder.Property(p => p.Copy6).HasMaxLength(100);
        builder.Property(p => p.Copy7).HasMaxLength(100);
        builder.Property(p => p.Copy8).HasMaxLength(100);
        builder.Property(p => p.Copy9).HasMaxLength(100);
        builder.Property(p => p.Copy10).HasMaxLength(100);
        builder.Property(p => p.KitchenTypeId)
            .HasColumnType("int")
            .IsRequired();

        builder.HasOne(p => p.KitchenType)  
              .WithMany(k => k.KitchenPrinters)  
              .HasForeignKey(p => p.KitchenTypeId)
              .OnDelete(DeleteBehavior.Restrict);
    }
}