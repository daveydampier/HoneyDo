using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(AppDbContext db, JwtService jwt) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var profile = await db.Profiles
            .FirstOrDefaultAsync(p => p.Email == request.Email.ToLower(), ct)
            ?? throw new NotFoundException();

        if (!BCrypt.Net.BCrypt.Verify(request.Password, profile.PasswordHash))
            throw new NotFoundException();

        return new AuthResponse(jwt.GenerateToken(profile), profile.Id, profile.DisplayName);
    }
}
