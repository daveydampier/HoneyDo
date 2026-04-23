using HoneyDo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyDo.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email).HasMaxLength(256).IsRequired();
        builder.Property(i => i.Token).HasMaxLength(50).IsRequired();

        // Token must be globally unique — it is the redemption credential.
        builder.HasIndex(i => i.Token).IsUnique();

        // Efficiently find whether an inviter has already invited a given email.
        builder.HasIndex(i => new { i.InviterId, i.Email });

        builder.HasOne(i => i.Inviter)
               .WithMany()
               .HasForeignKey(i => i.InviterId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
