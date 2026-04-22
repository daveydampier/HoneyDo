using HoneyDo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyDo.Data.Configurations;

public class FriendConfiguration : IEntityTypeConfiguration<Friend>
{
    public void Configure(EntityTypeBuilder<Friend> builder)
    {
        builder.HasKey(f => new { f.RequesterId, f.AddresseeId });

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(f => f.Requester)
            .WithMany(p => p.SentRequests)
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Addressee)
            .WithMany(p => p.ReceivedRequests)
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
