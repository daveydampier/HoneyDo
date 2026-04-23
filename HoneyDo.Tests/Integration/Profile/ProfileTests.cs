using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Profile;

public class ProfileTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetProfile_Authenticated_Returns200WithProfileData()
    {
        var (client, _, _, email) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        body!.Email.Should().Be(email);
        body.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetProfile_Unauthenticated_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_ValidData_Returns200WithUpdatedName()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PatchAsJsonAsync("/api/profile", new
        {
            displayName = "Updated Name",
            phoneNumber = (string?)null,
            avatarUrl = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        body!.DisplayName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProfile_EmptyDisplayName_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PatchAsJsonAsync("/api/profile", new
        {
            displayName = "",
            phoneNumber = (string?)null,
            avatarUrl = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_ValidCurrentPassword_Returns204()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PatchAsJsonAsync("/api/profile/password", new
        {
            currentPassword = "Password1!",
            newPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PatchAsJsonAsync("/api/profile/password", new
        {
            currentPassword = "WrongPassword!",
            newPassword = "NewPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordTooShort_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var response = await client.PatchAsJsonAsync("/api/profile/password", new
        {
            currentPassword = "Password1!",
            newPassword = "abc"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record ProfileResponse(Guid Id, string Email, string DisplayName, string? PhoneNumber, string? AvatarUrl);
}
