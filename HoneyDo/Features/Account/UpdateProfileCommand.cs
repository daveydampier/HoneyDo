using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Account;

public record UpdateProfileCommand(
    Guid ProfileId,
    string DisplayName,
    string? PhoneNumber,
    string? AvatarUrl) : IRequest<ProfileResponse>;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
        // No length limit on AvatarUrl — it may hold a base64 data URL from an image upload.
    }
}

public class UpdateProfileCommandHandler(AppDbContext db) : IRequestHandler<UpdateProfileCommand, ProfileResponse>
{
    public async Task<ProfileResponse> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == request.ProfileId, ct)
            ?? throw new NotFoundException();

        profile.DisplayName = request.DisplayName;
        profile.PhoneNumber = request.PhoneNumber;
        profile.AvatarUrl = request.AvatarUrl;

        await db.SaveChangesAsync(ct);

        return new ProfileResponse(profile.Id, profile.Email, profile.DisplayName, profile.PhoneNumber, profile.AvatarUrl, profile.CreatedAt);
    }
}
