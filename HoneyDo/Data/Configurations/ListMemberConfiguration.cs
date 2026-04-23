using HoneyDo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyDo.Data.Configurations;

public class ListMemberConfiguration : IEntityTypeConfiguration<ListMember>
{
    public void Configure(EntityTypeBuilder<ListMember> builder)
    {
        builder.HasKey(m => new { m.ListId, m.ProfileId });

        builder.Property(m => m.Role)
            .HasConversion<string>();

        builder.HasOne(m => m.List)
            .WithMany(l => l.Members)
            .HasForeignKey(m => m.ListId);

        builder.HasOne(m => m.Profile)
            .WithMany(p => p.ListMemberships)
            .HasForeignKey(m => m.ProfileId);
    }
}
