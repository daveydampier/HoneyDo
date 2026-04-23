using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Common.Models;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Items;

public enum ItemSortBy { CreatedAt, DueDate, Content }

public record GetItemsQuery(
    Guid ListId,
    Guid ProfileId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    int[]? StatusIds = null,
    ItemSortBy SortBy = ItemSortBy.CreatedAt,
    bool Ascending = true) : IRequest<PagedResult<TodoItemResponse>>;

public class GetItemsQueryValidator : AbstractValidator<GetItemsQuery>
{
    public GetItemsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetItemsQueryHandler(AppDbContext db) : IRequestHandler<GetItemsQuery, PagedResult<TodoItemResponse>>
{
    public async Task<PagedResult<TodoItemResponse>> Handle(GetItemsQuery request, CancellationToken ct)
    {
        var isMember = await db.ListMembers
            .AnyAsync(m => m.ListId == request.ListId && m.ProfileId == request.ProfileId, ct);

        if (!isMember) throw new NotFoundException();

        var query = db.TodoItems
            .Where(i => i.ListId == request.ListId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(i => i.Content.Contains(request.Search) || (i.Notes != null && i.Notes.Contains(request.Search)));

        if (request.StatusIds is { Length: > 0 })
            query = query.Where(i => request.StatusIds.Contains(i.StatusId));

        query = (request.SortBy, request.Ascending) switch
        {
            // Nulls last for ascending due-date so undated tasks float to the bottom.
            (ItemSortBy.DueDate, true) => query.OrderBy(i => i.DueDate == null).ThenBy(i => i.DueDate),
            (ItemSortBy.DueDate, false) => query.OrderBy(i => i.DueDate == null).ThenByDescending(i => i.DueDate),
            (ItemSortBy.Content, true) => query.OrderBy(i => i.Content),
            (ItemSortBy.Content, false) => query.OrderByDescending(i => i.Content),
            (_, true) => query.OrderBy(i => i.CreatedAt),
            (_, false) => query.OrderByDescending(i => i.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(TodoItemResponse.Projection)
            .ToListAsync(ct);

        return new PagedResult<TodoItemResponse>(items, total, request.Page, request.PageSize);
    }
}
