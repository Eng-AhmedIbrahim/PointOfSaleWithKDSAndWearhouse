namespace POS.Repository.Data.Configurations.CompanyConfigurations;

public class BranchConfigurations : IEntityTypeConfiguration<Branch>
{
    private readonly int MaxLength = 255;
    private readonly int MinLength = 70;
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.Property(c => c.Name)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.Property(c => c.NormalizedName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();


        builder.Property(c => c.ImagePath)
        .HasColumnType("nvarchar")
        .HasMaxLength(MaxLength)
        .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnType("nvarchar")
            .HasMaxLength(MaxLength);

        builder.Property(c => c.Address)
           .HasColumnType("nvarchar")
           .HasMaxLength(MaxLength);

        builder.Property(c => c.Phone1)
           .HasColumnType("nvarchar")
           .HasMaxLength(15);

        builder.Property(c => c.Phone2)
          .HasColumnType("nvarchar")
          .HasMaxLength(15);

        builder.Property(c => c.Suspend)
        .HasColumnType("bit")
        .HasDefaultValue(false);

        builder.Property(c => c.Active)
       .HasColumnType("bit")
       .HasDefaultValue(true);

        builder.Property(c => c.UpdateDate)
          .HasColumnType("datetime");


        builder.Property(c => c.CreationDate)
         .HasColumnType("datetime");


        builder.Property(c => c.LogoWidth)
            .HasColumnType("int")
            .HasDefaultValue(200);

        builder.Property(c => c.LogoHeight)
         .HasColumnType("int")
         .HasDefaultValue(100);

        builder.HasOne(b => b.Company)
            .WithMany(b => b.Branches)
            .HasForeignKey(b => b.CompanyId);

        builder.Property(b=>b.Id)
            .ValueGeneratedNever();

        builder.HasMany(b=>b.DeliveryZones)
            .WithOne(z=>z.Branch)
            .HasForeignKey(z=>z.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(b => b.ApiUrl)
            .HasColumnType("nvarchar")
            .HasMaxLength(255);

        builder.HasMany(b=>b.DeliveryZones)
            .WithOne(b=>b.Branch)
            .HasForeignKey(b=>b.BranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}