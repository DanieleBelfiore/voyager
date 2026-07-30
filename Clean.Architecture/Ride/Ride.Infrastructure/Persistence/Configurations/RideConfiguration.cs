using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RideEntity = Ride.Domain.Entities.Ride;

namespace Ride.Infrastructure.Persistence.Configurations;

public class RideConfiguration : IEntityTypeConfiguration<RideEntity>
{
  public void Configure(EntityTypeBuilder<RideEntity> builder)
  {
    builder.ToTable("Rides");

    builder.HasKey(r => r.Id);
    builder.Property(r => r.Id).ValueGeneratedNever();

    builder.Property(r => r.CancellationReason).HasMaxLength(128);
    builder.Property(r => r.PickupLocation).HasColumnType("geography");
    builder.Property(r => r.DropoffLocation).HasColumnType("geography");
    builder.Property(r => r.LastLocation).HasColumnType("geography");
    builder.Property(r => r.RowVersion).IsRowVersion();

    builder.HasIndex(r => new { r.Status, r.UserId, r.DriverId })
      .HasDatabaseName("IX_Rides_Status_UserId_DriverId");

    // Enforces "one active ride per user" at the DB level, mirroring the in-memory check in
    // RequestRideHandler (HasInFlightRideAsync) so a race between two concurrent requests can't
    // both pass the in-memory check and insert two in-flight rides for the same user.
    builder.HasIndex(r => r.UserId)
      .HasDatabaseName("IX_Rides_UserId_ActiveOnly")
      .IsUnique()
      .HasFilter("[Status] IN (0, 1, 2)");

    builder.HasIndex(r => new { r.UserId, r.Status, r.RequestedAt })
      .HasDatabaseName("IX_Rides_UserId_Status_RequestedAt")
      .IsDescending(false, false, true);

    builder.HasIndex(r => new { r.DriverId, r.Status, r.RequestedAt })
      .HasDatabaseName("IX_Rides_DriverId_Status_RequestedAt")
      .IsDescending(false, false, true);
  }
}
