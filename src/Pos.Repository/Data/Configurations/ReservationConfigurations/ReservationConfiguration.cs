using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Core.Entities.ReservationEntity;

namespace POS.Repository.Data.Configurations.ReservationConfigurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.CustomerName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.ReservationStatus)
            .HasMaxLength(50);

        builder.Property(r => r.Notes)
            .HasMaxLength(500);

        builder.Property(r => r.ReservationPaid)
            .HasColumnType("decimal(18,2)");

        // Relationships
        builder.HasOne(r => r.Order)
            .WithOne(o => o.Reservation)
            .HasForeignKey<Reservation>(r => r.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Table)
            .WithMany()
            .HasForeignKey(r => r.TableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
