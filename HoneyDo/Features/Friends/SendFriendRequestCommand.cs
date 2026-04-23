using FluentValidation;
using HoneyDo.Common.Services;
using HoneyDo.Data;
using HoneyDo.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HoneyDo.Features.Friends;

public record SendFriendRequestCommand(Guid RequesterId, string Email) : IRequest<SendRequestResult>;

/// <summary>
/// Tells the controller — and the frontend — what actually happened.
/// InvitationSent=true means the email wasn't a registered user and an invite was dispatched.
/// </summary>
public record SendRequestResult(bool InvitationSent);

public class SendFriendRequestCommandValidator : AbstractValidator<SendFriendRequestCommand>
{
    public SendFriendRequestCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class SendFriendRequestCommandHandler(
    AppDbContext db,
    IEmailService email,
    IConfiguration config) : IRequestHandler<SendFriendRequestCommand, SendRequestResult>
{
    public async Task<SendRequestResult> Handle(SendFriendRequestCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLower();
        var addressee = await db.Profiles.FirstOrDefaultAsync(p => p.Email == normalizedEmail, ct);

        // ── Unknown email: send an invitation instead of returning an error ──
        if (addressee is null)
        {
            await SendInvitationAsync(request.RequesterId, normalizedEmail, ct);
            return new SendRequestResult(InvitationSent: true);
        }

        // Cannot send a request to yourself
        if (addressee.Id == request.RequesterId)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                "Email", "You cannot send a friend request to yourself.")]);

        // Duplicate check (either direction)
        var existing = await db.Friends.FirstOrDefaultAsync(
            f => (f.RequesterId == request.RequesterId && f.AddresseeId == addressee.Id) ||
                 (f.RequesterId == addressee.Id && f.AddresseeId == request.RequesterId), ct);

        if (existing is not null)
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                "Email", "A friend request already exists with this user.")]);

        db.Friends.Add(new Friend
        {
            RequesterId = request.RequesterId,
            AddresseeId = addressee.Id,
            Status = FriendStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return new SendRequestResult(InvitationSent: false);
    }

    private async Task SendInvitationAsync(Guid inviterId, string toEmail, CancellationToken ct)
    {
        // If this inviter already has a pending (unused) invitation to this address, don't spam.
        var alreadyInvited = await db.Invitations.AnyAsync(
            i => i.InviterId == inviterId && i.Email == toEmail && i.AcceptedAt == null, ct);

        if (alreadyInvited) return;

        var inviter = await db.Profiles.FirstAsync(p => p.Id == inviterId, ct);

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "-").Replace("/", "_").TrimEnd('='); // 22-char URL-safe token

        db.Invitations.Add(new Invitation
        {
            Id = Guid.NewGuid(),
            InviterId = inviterId,
            Email = toEmail,
            Token = token,
            SentAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        var appUrl = config["AppUrl"] ?? "http://localhost:5173";
        var inviteUrl = $"{appUrl}/register?invite={token}&email={Uri.EscapeDataString(toEmail)}";
        var body = BuildEmailBody(inviter.DisplayName, inviteUrl);

        await email.SendAsync(toEmail, $"{inviter.DisplayName} invited you to HoneyDo", body, ct);
    }

    private static string BuildEmailBody(string inviterName, string inviteUrl) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family: sans-serif; max-width: 480px; margin: 40px auto; color: #222;">
          <h2 style="margin-bottom: 8px;">You've been invited to HoneyDo 🍯</h2>
          <p><strong>{inviterName}</strong> wants to connect with you on HoneyDo — a shared to-do list app.</p>
          <p style="margin: 24px 0;">
            <a href="{inviteUrl}"
               style="background:#0a84ff; color:#fff; padding:12px 24px; border-radius:6px;
                      text-decoration:none; font-weight:600; display:inline-block;">
              Accept invitation &amp; create account
            </a>
          </p>
          <p style="font-size:13px; color:#666;">
            Or copy this link into your browser:<br>
            <a href="{inviteUrl}" style="color:#0a84ff;">{inviteUrl}</a>
          </p>
          <hr style="border:none; border-top:1px solid #eee; margin: 32px 0;">
          <p style="font-size:12px; color:#aaa;">
            If you weren't expecting this, you can safely ignore this email.
          </p>
        </body>
        </html>
        """;
}
