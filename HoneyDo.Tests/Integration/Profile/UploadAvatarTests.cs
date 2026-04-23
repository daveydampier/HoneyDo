using FluentAssertions;
using HoneyDo.Tests.Fixtures;
using HoneyDo.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace HoneyDo.Tests.Integration.Profile;

public class UploadAvatarTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // Minimal 1×1 PNG (67 bytes) — a real, parseable image so the content-type is accepted.
    private static readonly byte[] MinimalPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR length + type
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // width=1, height=1
        0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, // bit depth=8, color=RGB
        0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT length + type
        0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00, // compressed pixel
        0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, // CRC
        0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND length + type
        0x44, 0xAE, 0x42, 0x60, 0x82                    // IEND CRC
    ];

    private static MultipartFormDataContent BuildImageContent(byte[] data, string contentType, string filename)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(data);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", filename);
        return content;
    }

    [Fact]
    public async Task UploadAvatar_ValidPng_Returns200AndSetsAvatarUrl()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var content = BuildImageContent(MinimalPng, "image/png", "avatar.png");
        var response = await client.PostAsync("/api/profile/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        body!.AvatarUrl.Should().StartWith("data:image/png;base64,");
    }

    [Fact]
    public async Task UploadAvatar_AvatarUrlPersists_VisibleOnGetProfile()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var content = BuildImageContent(MinimalPng, "image/png", "avatar.png");
        await client.PostAsync("/api/profile/avatar", content);

        var profile = await client.GetFromJsonAsync<ProfileResponse>("/api/profile");
        profile!.AvatarUrl.Should().StartWith("data:image/png;base64,");
    }

    [Fact]
    public async Task UploadAvatar_JpegContentType_Returns200()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        // Use the PNG bytes but declare it as JPEG — the handler only validates ContentType, not magic bytes
        var content = BuildImageContent(MinimalPng, "image/jpeg", "avatar.jpg");
        var response = await client.PostAsync("/api/profile/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>();
        body!.AvatarUrl.Should().StartWith("data:image/jpeg;base64,");
    }

    [Fact]
    public async Task UploadAvatar_DisallowedContentType_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var content = BuildImageContent(MinimalPng, "text/plain", "avatar.txt");
        var response = await client.PostAsync("/api/profile/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAvatar_PdfContentType_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        var content = BuildImageContent(MinimalPng, "application/pdf", "document.pdf");
        var response = await client.PostAsync("/api/profile/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAvatar_FileTooLarge_Returns400()
    {
        var (client, _, _, _) = await TestApi.RegisterAsync(factory.CreateClient());

        // 2 MB + 1 byte exceeds the 2 MB limit
        var oversizedData = new byte[2 * 1024 * 1024 + 1];
        var content = BuildImageContent(oversizedData, "image/png", "big.png");
        var response = await client.PostAsync("/api/profile/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadAvatar_Unauthenticated_Returns401()
    {
        var content = BuildImageContent(MinimalPng, "image/png", "avatar.png");
        var response = await factory.CreateClient().PostAsync("/api/profile/avatar", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record ProfileResponse(Guid Id, string Email, string DisplayName, string? PhoneNumber, string? AvatarUrl);
}
