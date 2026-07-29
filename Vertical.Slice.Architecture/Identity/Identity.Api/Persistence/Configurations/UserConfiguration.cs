using Identity.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Api.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> builder)
  {
    builder.ToTable("Users");

    builder.HasKey(u => u.Id);
    builder.Property(u => u.Id).ValueGeneratedNever();

    builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
    builder.HasIndex(u => u.Email).IsUnique();

    builder.Property(u => u.FirstName).HasMaxLength(64);
    builder.Property(u => u.LastName).HasMaxLength(64);
    builder.Property(u => u.PhoneNumber).HasMaxLength(64);
    builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
  }
}
