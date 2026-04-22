using HoneyDo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyDo.Data.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(p => p.Email).IsUnique();
        builder.Property(p => p.PasswordHash).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.AvatarUrl).HasMaxLength(512);
    }
}
