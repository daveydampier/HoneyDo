using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Tags;

public record DeleteTagCommand(Guid TagId, Guid ProfileId) : IRequest;

public class DeleteTagCommandHandler(AppDbContext db) : IRequestHandler<DeleteTagCommand>
{
    public async Task Handle(DeleteTagCommand request, CancellationToken ct)
    {
        var tag = await db.Tags
            .FirstOrDefaultAsync(t => t.Id == request.TagId && t.ProfileId == request.ProfileId, ct)
            ?? throw new NotFoundException();

        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
    }
}
