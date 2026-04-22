using HoneyDo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyDo.Data.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ActionType).HasMaxLength(50).IsRequired();

        builder.HasOne(a => a.List)
            .WithMany(l => l.ActivityLogs)
            .HasForeignKey(a => a.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Actor)
            .WithMany(p => p.ActivityLogs)
            .HasForeignKey(a => a.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
