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
        // AvatarUrl can hold either a short https:// URL or a base64 data URI
        // (uploaded images up to 2 MB become ~2.7 MB base64 strings).
        // SQLite's TEXT type has no practical length limit, so we don't constrain
        // it here. If the project ever moves to SQL Server / PostgreSQL this
        // column should instead point to blob-storage URLs, which ARE short.
        builder.Property(p => p.AvatarUrl);
    }
}
