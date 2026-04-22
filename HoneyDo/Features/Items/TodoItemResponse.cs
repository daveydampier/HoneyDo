using HoneyDo.Domain;
using System.Linq.Expressions;

namespace HoneyDo.Features.Items;

public record TodoItemResponse(
    Guid Id,
    Guid ListId,
    string Content,
    StatusDto Status,
    string? Notes,
    string? DueDate,
    AssigneeDto? AssignedTo,
    IEnumerable<TagDto> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static Expression<Func<TodoItem, TodoItemResponse>> Projection =>
        i => new TodoItemResponse(
            i.Id,
            i.ListId,
            i.Content,
            new StatusDto(i.Status.Id, i.Status.Name),
            i.Notes,
            i.DueDate,
            i.AssignedTo == null ? null : new AssigneeDto(i.AssignedTo.Id, i.AssignedTo.DisplayName),
            i.ItemTags.Select(t => new TagDto(t.Tag.Id, t.Tag.Name, t.Tag.Color)),
            i.CreatedAt,
            i.UpdatedAt);
}

public record StatusDto(int Id, string Name);
public record AssigneeDto(Guid Id, string DisplayName);
public record TagDto(Guid Id, string Name, string Color);
