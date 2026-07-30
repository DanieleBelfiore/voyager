using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RideEntity = Ride.Module.Entities.Ride;

namespace Ride.Module.Persistence.Configurations;

internal class RideConfiguration : IEntityTypeConfiguration<RideEntity>
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

    // Mirrors Identity's Users.Email unique index: DB-enforced backstop for the in-memory
    // "no in-flight ride" check in RequestRideHandler, closing the check-then-act race.
    // RideStatus: Requested = 0, DriverAssigned = 1, InProgress = 2.
    builder.HasIndex(r => r.UserId)
      .IsUnique()
      .HasFilter("[Status] IN (0, 1, 2)")
      .HasDatabaseName("IX_Rides_UserId_Active_Unique");
  }
}
