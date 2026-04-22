using FluentValidation;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Auth;

public record RegisterCommand(string Email, string Password, string DisplayName) : IRequest<AuthResponse>;

public record AuthResponse(string Token, Guid ProfileId, string DisplayName);

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(256);
    }
}

public class RegisterCommandHandler(AppDbContext db, JwtService jwt) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var exists = await db.Profiles.AnyAsync(p => p.Email == request.Email.ToLower(), ct);
        if (exists)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure("Email", "Email is already registered.")]);

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        db.Profiles.Add(profile);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(jwt.GenerateToken(profile), profile.Id, profile.DisplayName);
    }
}
