using HoneyDo.Common.Extensions;
using HoneyDo.Common.Middleware;
using HoneyDo.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddMediatRAndValidation();
builder.Services.AddEmailService();
builder.Services.AddOpenApiDocs();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration["AllowedOrigins"]!.Split(','))
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Apply pending EF migrations automatically on startup when MigrateOnStartup is true.
// Enabled via appsettings.Development.json for local development. Production deployments
// should run migrations as a dedicated pre-deploy step to avoid race conditions when
// starting multiple replicas against a shared database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational() && app.Configuration.GetValue<bool>("MigrateOnStartup"))
        db.Database.Migrate();
}

app.UseMiddleware<SecurityHeadersMiddleware>(); // Must be first — headers must appear on all responses, including error payloads.
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposes Program as a partial class so HoneyDo.Tests can reference it via
// WebApplicationFactory<Program>. This is intentional — not dead code.
public partial class Program { }
