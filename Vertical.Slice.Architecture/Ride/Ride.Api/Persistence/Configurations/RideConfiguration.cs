using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RideEntity = Ride.Api.Entities.Ride;

namespace Ride.Api.Persistence.Configurations;

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

    builder.HasIndex(r => new { r.UserId, r.Status, r.RequestedAt })
      .HasDatabaseName("IX_Rides_UserId_Status_RequestedAt")
      .IsDescending(false, false, true);

    builder.HasIndex(r => new { r.DriverId, r.Status, r.RequestedAt })
      .HasDatabaseName("IX_Rides_DriverId_Status_RequestedAt")
      .IsDescending(false, false, true);

    // Mirrors Identity's Users.Email unique index: only one active (Requested/DriverAssigned/
    // InProgress) ride per user, enforced at the DB layer to close the race the in-memory
    // duplicate check in RequestRideHandler can't fully prevent under concurrent requests.
    builder.HasIndex(r => r.UserId)
      .HasDatabaseName("IX_Rides_UserId_ActiveRide")
      .IsUnique()
      .HasFilter("[Status] IN (0, 1, 2)");
  }
}
