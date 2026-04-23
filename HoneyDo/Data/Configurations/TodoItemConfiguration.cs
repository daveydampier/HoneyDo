using HoneyDo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyDo.Data.Configurations;

public class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Content).HasMaxLength(512).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(256);
        builder.Property(i => i.DueDate).HasMaxLength(10).HasColumnType("TEXT");

        builder.HasOne(i => i.List)
            .WithMany(l => l.Items)
            .HasForeignKey(i => i.ListId);

        builder.HasOne(i => i.Status)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.StatusId);

        builder.HasOne(i => i.AssignedTo)
            .WithMany()
            .HasForeignKey(i => i.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
