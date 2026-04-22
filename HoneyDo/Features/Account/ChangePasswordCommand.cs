using FluentValidation;
using HoneyDo.Common.Exceptions;
using HoneyDo.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Account;

public record ChangePasswordCommand(
    Guid ProfileId,
    string CurrentPassword,
    string NewPassword) : IRequest;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class ChangePasswordCommandHandler(AppDbContext db) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == request.ProfileId, ct)
            ?? throw new NotFoundException();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, profile.PasswordHash))
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                "CurrentPassword", "Current password is incorrect.")]);

        profile.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync(ct);
    }
}
