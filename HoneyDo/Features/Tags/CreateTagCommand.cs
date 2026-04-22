using FluentValidation;
using HoneyDo.Data;
using HoneyDo.Domain;
using HoneyDo.Features.Items;
using MediatR;

namespace HoneyDo.Features.Tags;

public record CreateTagCommand(Guid ProfileId, string Name, string Color) : IRequest<TagDto>;

public class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex code (e.g. #FF5733).");
    }
}

public class CreateTagCommandHandler(AppDbContext db) : IRequestHandler<CreateTagCommand, TagDto>
{
    public async Task<TagDto> Handle(CreateTagCommand request, CancellationToken ct)
    {
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            ProfileId = request.ProfileId,
            Name = request.Name,
            Color = request.Color,
            CreatedAt = DateTime.UtcNow
        };

        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);

        return new TagDto(tag.Id, tag.Name, tag.Color);
    }
}
