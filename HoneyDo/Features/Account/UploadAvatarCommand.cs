using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Account;

public record UploadAvatarCommand(Guid ProfileId, byte[] Data, string ContentType) : IRequest<ProfileResponse>;

public class UploadAvatarCommandHandler(AppDbContext db) : IRequestHandler<UploadAvatarCommand, ProfileResponse>
{
    private static readonly HashSet<string> AllowedTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const int MaxBytes = 2 * 1024 * 1024; // 2 MB

    public async Task<ProfileResponse> Handle(UploadAvatarCommand request, CancellationToken ct)
    {
        if (!AllowedTypes.Contains(request.ContentType))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                "File", "Only JPEG, PNG, WebP, and GIF images are allowed.")]);

        if (request.Data.Length > MaxBytes)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                "File", "Image must be 2 MB or smaller.")]);

        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == request.ProfileId, ct)
            ?? throw new NotFoundException();

        profile.AvatarUrl = $"data:{request.ContentType};base64,{Convert.ToBase64String(request.Data)}";

        await db.SaveChangesAsync(ct);

        return new ProfileResponse(
            profile.Id, profile.Email, profile.DisplayName,
            profile.PhoneNumber, profile.AvatarUrl, profile.CreatedAt);
    }
}
