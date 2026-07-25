using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DriverEntity = Driver.Api.Entities.Driver;

namespace Driver.Api.Persistence.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<DriverEntity>
{
  public void Configure(EntityTypeBuilder<DriverEntity> builder)
  {
    builder.ToTable("Drivers");

    builder.HasKey(d => d.Id);
    builder.Property(d => d.Id).ValueGeneratedNever();
    builder.Property(d => d.LastLocation).HasColumnType("geography");

    builder.HasIndex(d => new { d.Status, d.LastUpdateDate })
      .HasDatabaseName("IX_Drivers_Status_LastUpdateDate")
      .IsDescending(false, true);
  }
}
