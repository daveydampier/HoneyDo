using FluentValidation;
using HoneyDo.Common.Behaviors;
using HoneyDo.Common.Services;
using HoneyDo.Data;
using HoneyDo.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace HoneyDo.Common.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("DefaultConnection")));
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtSection = config.GetSection("Jwt");

        var keyString = jwtSection["Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key is not configured. Set it via environment variable (Jwt__Key=<secret>) " +
                "or .NET User Secrets. See DOCUMENTATION.md §7.1.");

        // HMAC-SHA256 requires a 256-bit (32-byte) key at minimum.
        if (Encoding.UTF8.GetByteCount(keyString) < 32)
            throw new InvalidOperationException(
                "Jwt:Key must be at least 32 bytes (256 bits) for HMAC-SHA256 signing.");

        var key = Encoding.UTF8.GetBytes(keyString);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }

    public static IServiceCollection AddMediatRAndValidation(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddScoped<JwtService>();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        return services;
    }

    public static IServiceCollection AddEmailService(this IServiceCollection services)
    {
        // Singleton is safe: SmtpEmailService has no mutable fields.
        // SmtpClient is created per-call inside SendAsync, not held as a field.
        services.AddSingleton<IEmailService, SmtpEmailService>();
        return services;
    }

    public static IServiceCollection AddOpenApiDocs(this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }
}
