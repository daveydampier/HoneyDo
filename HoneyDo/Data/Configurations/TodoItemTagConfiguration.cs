using HoneyDo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyDo.Data.Configurations;

public class TodoItemTagConfiguration : IEntityTypeConfiguration<TodoItemTag>
{
    public void Configure(EntityTypeBuilder<TodoItemTag> builder)
    {
        builder.HasKey(t => new { t.ItemId, t.TagId });

        builder.HasOne(t => t.Item)
            .WithMany(i => i.ItemTags)
            .HasForeignKey(t => t.ItemId);

        builder.HasOne(t => t.Tag)
            .WithMany(tag => tag.ItemTags)
            .HasForeignKey(t => t.TagId);
    }
}
